package com.nowlink.mobile.service

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.ServiceInfo
import android.os.Build
import android.os.Handler
import android.os.IBinder
import android.os.Looper
import android.telephony.PhoneStateListener
import android.telephony.TelephonyManager
import androidx.core.app.NotificationCompat
import com.nowlink.mobile.R
import com.nowlink.mobile.data.DiagnosticLogger
import com.nowlink.mobile.data.PendingEventStore
import com.nowlink.mobile.data.SettingsRepository
import com.nowlink.mobile.model.NotificationPayload
import com.nowlink.mobile.net.RelaySocket
import java.time.Instant
import java.util.UUID

class NotificationRelayService : Service() {
    private lateinit var settings: SettingsRepository
    private lateinit var queue: PendingEventStore
    private lateinit var logger: DiagnosticLogger
    private val relaySocket = RelaySocket()
    private val handler = Handler(Looper.getMainLooper())
    private var callListenerAttached = false
    private val flushRunnable = object : Runnable {
        override fun run() {
            connectIfConfigured()
            flushQueue()
            handler.postDelayed(this, 2500)
        }
    }

    override fun onCreate() {
        super.onCreate()
        settings = SettingsRepository(this)
        queue = PendingEventStore(this)
        logger = DiagnosticLogger(this)
        val notification = foreground(getString(R.string.relay_running))
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            startForeground(17, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_DATA_SYNC)
        } else {
            startForeground(17, notification)
        }
        logger.log("service created")
        connectIfConfigured()
        listenForCalls()
        handler.post(flushRunnable)
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        logger.log("service onStartCommand queue=${queue.size()}")
        connectIfConfigured()
        flushQueue()
        return START_STICKY
    }

    override fun onDestroy() {
        handler.removeCallbacks(flushRunnable)
        logger.log("service destroyed")
        relaySocket.close()
        super.onDestroy()
    }

    override fun onBind(intent: Intent?): IBinder? = null

    private fun connectIfConfigured() {
        val host = settings.host() ?: return
        if (relaySocket.isConnected()) {
            return
        }
        logger.log("connecting websocket host=$host port=${settings.port()}")
        relaySocket.connect(
            host = host,
            port = settings.port(),
            path = settings.wsPath(),
            onConnected = {
                logger.log("websocket connected")
                val manager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
                manager.notify(17, foreground(getString(R.string.relay_connected, host, settings.port())))
                flushQueue()
            },
            onDisconnected = { reason ->
                logger.log("websocket disconnected reason=$reason")
            }
        )
    }

    private fun listenForCalls() {
        if (callListenerAttached) return
        val telephony = getSystemService(Context.TELEPHONY_SERVICE) as TelephonyManager
        @Suppress("DEPRECATION")
        telephony.listen(object : PhoneStateListener() {
            override fun onCallStateChanged(state: Int, phoneNumber: String?) {
                val callState = when (state) {
                    TelephonyManager.CALL_STATE_RINGING -> "incoming"
                    TelephonyManager.CALL_STATE_IDLE -> "ended"
                    else -> return
                }

                RelayDispatcher.submit(
                    this@NotificationRelayService,
                    NotificationPayload(
                        eventId = UUID.randomUUID().toString(),
                        phoneId = settings.phoneId(),
                        category = "call",
                        packageName = "android.telephony",
                        appName = getString(R.string.phone_app_name),
                        title = getString(R.string.call_status_title),
                        body = phoneNumber ?: getString(R.string.unknown_number),
                        receivedAt = Instant.now().toString(),
                        phoneNumber = phoneNumber,
                        callState = callState
                    ),
                    "callListener"
                )
            }
        }, PhoneStateListener.LISTEN_CALL_STATE)
        callListenerAttached = true
    }

    private fun flushQueue() {
        if (!relaySocket.isConnected()) {
            return
        }

        val batch = queue.peek(20)
        if (batch.isEmpty()) {
            return
        }

        var sentCount = 0
        for (payload in batch) {
            val sent = relaySocket.send(payload)
            logger.log("send attempt sent=$sent category=${payload.category} package=${payload.packageName} title=${payload.title.take(60)}")
            if (!sent) {
                break
            }
            sentCount++
        }

        if (sentCount > 0) {
            queue.removeFirst(sentCount)
            logger.log("queue drained count=$sentCount remaining=${queue.size()}")
        }
    }

    private fun foreground(message: String): Notification {
        val channelId = "nowlink_relay"
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(channelId, getString(R.string.relay_channel_name), NotificationManager.IMPORTANCE_LOW)
            val manager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
            manager.createNotificationChannel(channel)
        }

        return NotificationCompat.Builder(this, channelId)
            .setSmallIcon(android.R.drawable.stat_notify_sync)
            .setContentTitle(getString(R.string.app_name))
            .setContentText(message)
            .setOngoing(true)
            .build()
    }
}

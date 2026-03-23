package com.nowlink.mobile.service

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.ServiceInfo
import android.os.Build
import android.os.IBinder
import android.telephony.PhoneStateListener
import android.telephony.TelephonyManager
import androidx.core.app.NotificationCompat
import com.nowlink.mobile.data.SettingsRepository
import com.nowlink.mobile.model.NotificationPayload
import com.nowlink.mobile.net.RelaySocket
import java.time.Instant
import java.util.UUID
import com.nowlink.mobile.R

class NotificationRelayService : Service() {
    private lateinit var settings: SettingsRepository
    private val relaySocket = RelaySocket()

    override fun onCreate() {
        super.onCreate()
        settings = SettingsRepository(this)
        val notification = foreground(getString(R.string.relay_running))
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            startForeground(17, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_DATA_SYNC)
        } else {
            startForeground(17, notification)
        }
        connectIfConfigured()
        listenForCalls()
        RelayDispatcher.relay = { relaySocket.send(it) }
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        connectIfConfigured()
        return START_STICKY
    }

    override fun onDestroy() {
        RelayDispatcher.relay = null
        relaySocket.close()
        super.onDestroy()
    }

    override fun onBind(intent: Intent?): IBinder? = null

    private fun connectIfConfigured() {
        val host = settings.host() ?: return
        relaySocket.connect(host, settings.port(), settings.wsPath())
        val manager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        manager.notify(17, foreground(getString(R.string.relay_connected, host, settings.port())))
    }

    private fun listenForCalls() {
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
                    )
                )
            }
        }, PhoneStateListener.LISTEN_CALL_STATE)
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

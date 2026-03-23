package com.nowlink.mobile.service

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.Service
import android.content.Context
import android.content.Intent
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

class NotificationRelayService : Service() {
    private lateinit var settings: SettingsRepository
    private val relaySocket = RelaySocket()

    override fun onCreate() {
        super.onCreate()
        settings = SettingsRepository(this)
        startForeground(17, foreground("NowLink relay is active"))
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
        manager.notify(17, foreground("Connected to $host:${settings.port()}"))
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
                        appName = "Phone",
                        title = "Call status",
                        body = phoneNumber ?: "Unknown number",
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
            val channel = NotificationChannel(channelId, "NowLink Relay", NotificationManager.IMPORTANCE_LOW)
            val manager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
            manager.createNotificationChannel(channel)
        }

        return NotificationCompat.Builder(this, channelId)
            .setSmallIcon(android.R.drawable.stat_notify_sync)
            .setContentTitle("NowLink")
            .setContentText(message)
            .setOngoing(true)
            .build()
    }
}

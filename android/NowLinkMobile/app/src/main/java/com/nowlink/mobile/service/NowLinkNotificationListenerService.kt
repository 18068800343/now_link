package com.nowlink.mobile.service

import android.app.Notification
import android.content.Intent
import android.service.notification.NotificationListenerService
import android.service.notification.StatusBarNotification
import androidx.core.content.ContextCompat
import com.nowlink.mobile.data.SettingsRepository
import com.nowlink.mobile.model.NotificationPayload
import java.time.Instant
import java.util.UUID

class NowLinkNotificationListenerService : NotificationListenerService() {
    override fun onNotificationPosted(sbn: StatusBarNotification) {
        ContextCompat.startForegroundService(this, Intent(this, NotificationRelayService::class.java))
        val extras = sbn.notification.extras
        val settings = SettingsRepository(this)
        val title = extras.getCharSequence(Notification.EXTRA_TITLE)?.toString().orEmpty()
        val body = extras.getCharSequence(Notification.EXTRA_TEXT)?.toString().orEmpty()
        val appName = packageManager.getApplicationLabel(packageManager.getApplicationInfo(sbn.packageName, 0)).toString()

        RelayDispatcher.submit(
            NotificationPayload(
                eventId = UUID.randomUUID().toString(),
                phoneId = settings.phoneId(),
                category = "app",
                packageName = sbn.packageName,
                appName = appName,
                title = title,
                body = body,
                receivedAt = Instant.now().toString()
            )
        )
    }
}

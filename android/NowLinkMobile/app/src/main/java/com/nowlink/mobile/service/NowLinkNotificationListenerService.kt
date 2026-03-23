package com.nowlink.mobile.service

import android.app.Notification
import android.content.Intent
import android.service.notification.NotificationListenerService
import android.service.notification.StatusBarNotification
import androidx.core.content.ContextCompat
import com.nowlink.mobile.data.DiagnosticLogger
import com.nowlink.mobile.data.SettingsRepository
import com.nowlink.mobile.model.NotificationPayload
import java.time.Instant
import java.util.UUID

class NowLinkNotificationListenerService : NotificationListenerService() {
    override fun onNotificationPosted(sbn: StatusBarNotification) {
        ContextCompat.startForegroundService(this, Intent(this, NotificationRelayService::class.java))
        if (sbn.packageName == packageName) {
            DiagnosticLogger(this).log("skip self notification key=${sbn.key}")
            return
        }

        val extras = sbn.notification.extras
        val settings = SettingsRepository(this)
        val logger = DiagnosticLogger(this)
        val title = extractTitle(extras)
        val body = extractBody(extras)
        if (title.isBlank() && body.isBlank()) {
            logger.log("skip empty notification package=${sbn.packageName} key=${sbn.key}")
            return
        }
        val appName = packageManager.getApplicationLabel(packageManager.getApplicationInfo(sbn.packageName, 0)).toString()

        RelayDispatcher.submit(
            this,
            NotificationPayload(
                eventId = UUID.randomUUID().toString(),
                phoneId = settings.phoneId(),
                category = "app",
                packageName = sbn.packageName,
                appName = appName,
                title = title,
                body = body,
                receivedAt = Instant.now().toString()
            ),
            "notificationListener"
        )
        logger.log("captured notification package=${sbn.packageName} app=$appName title=${title.take(60)} body=${body.take(80)}")
    }

    private fun extractTitle(extras: android.os.Bundle): String {
        return extras.getCharSequence(Notification.EXTRA_TITLE)?.toString()
            ?: extras.getCharSequence(Notification.EXTRA_TITLE_BIG)?.toString()
            ?: ""
    }

    private fun extractBody(extras: android.os.Bundle): String {
        val bigText = extras.getCharSequence(Notification.EXTRA_BIG_TEXT)?.toString()
        if (!bigText.isNullOrBlank()) return bigText

        val text = extras.getCharSequence(Notification.EXTRA_TEXT)?.toString()
        if (!text.isNullOrBlank()) return text

        val lines = extras.getCharSequenceArray(Notification.EXTRA_TEXT_LINES)
        if (lines != null && lines.isNotEmpty()) {
            return lines.joinToString("\n") { it.toString() }
        }

        return ""
    }
}

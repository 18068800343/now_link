package com.nowlink.mobile.service

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.provider.Telephony
import androidx.core.content.ContextCompat
import com.nowlink.mobile.data.SettingsRepository
import com.nowlink.mobile.model.NotificationPayload
import java.time.Instant
import java.util.UUID

class SmsReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        if (intent.action != Telephony.Sms.Intents.SMS_RECEIVED_ACTION) return

        ContextCompat.startForegroundService(context, Intent(context, NotificationRelayService::class.java))
        val settings = SettingsRepository(context)
        for (message in Telephony.Sms.Intents.getMessagesFromIntent(intent)) {
            RelayDispatcher.submit(
                NotificationPayload(
                    eventId = UUID.randomUUID().toString(),
                    phoneId = settings.phoneId(),
                    category = "sms",
                    packageName = "android.sms",
                    appName = "SMS",
                    title = message.displayOriginatingAddress ?: "SMS",
                    body = message.displayMessageBody ?: "",
                    receivedAt = Instant.now().toString(),
                    smsSender = message.displayOriginatingAddress
                )
            )
        }
    }
}

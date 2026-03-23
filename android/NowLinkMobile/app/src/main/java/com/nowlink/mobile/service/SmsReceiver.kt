package com.nowlink.mobile.service

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.provider.Telephony
import androidx.core.content.ContextCompat
import com.nowlink.mobile.R
import com.nowlink.mobile.data.DiagnosticLogger
import com.nowlink.mobile.data.SettingsRepository
import com.nowlink.mobile.model.NotificationPayload
import java.time.Instant
import java.util.UUID

class SmsReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        if (intent.action != Telephony.Sms.Intents.SMS_RECEIVED_ACTION) return

        ContextCompat.startForegroundService(context, Intent(context, NotificationRelayService::class.java))
        val settings = SettingsRepository(context)
        val logger = DiagnosticLogger(context)
        for (message in Telephony.Sms.Intents.getMessagesFromIntent(intent)) {
            RelayDispatcher.submit(
                context,
                NotificationPayload(
                    eventId = UUID.randomUUID().toString(),
                    phoneId = settings.phoneId(),
                    category = "sms",
                    packageName = "android.sms",
                    appName = context.getString(R.string.sms_app_name),
                    title = message.displayOriginatingAddress ?: context.getString(R.string.sms_app_name),
                    body = message.displayMessageBody ?: "",
                    receivedAt = Instant.now().toString(),
                    smsSender = message.displayOriginatingAddress
                ),
                "smsReceiver"
            )
            logger.log("captured sms sender=${message.displayOriginatingAddress} body=${(message.displayMessageBody ?: "").take(80)}")
        }
    }
}

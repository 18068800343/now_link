package com.nowlink.mobile.service

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import androidx.core.content.ContextCompat
import com.nowlink.mobile.data.SettingsRepository

class BootReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        if (intent.action != Intent.ACTION_BOOT_COMPLETED) return
        val settings = SettingsRepository(context)
        if (settings.host() != null) {
            ContextCompat.startForegroundService(context, Intent(context, NotificationRelayService::class.java))
        }
    }
}

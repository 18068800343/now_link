package com.nowlink.mobile.service

import android.content.Context
import android.content.Intent
import androidx.core.content.ContextCompat
import com.nowlink.mobile.data.DiagnosticLogger
import com.nowlink.mobile.data.PendingEventStore
import com.nowlink.mobile.model.NotificationPayload

object RelayDispatcher {
    fun submit(context: Context, payload: NotificationPayload, source: String) {
        PendingEventStore(context).enqueue(payload)
        DiagnosticLogger(context).log("enqueued from=$source category=${payload.category} package=${payload.packageName} title=${payload.title.take(60)}")
        ContextCompat.startForegroundService(context, Intent(context, NotificationRelayService::class.java))
    }
}

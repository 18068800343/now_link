package com.nowlink.mobile.service

import com.nowlink.mobile.model.NotificationPayload

object RelayDispatcher {
    var relay: ((NotificationPayload) -> Unit)? = null

    fun submit(payload: NotificationPayload) {
        relay?.invoke(payload)
    }
}

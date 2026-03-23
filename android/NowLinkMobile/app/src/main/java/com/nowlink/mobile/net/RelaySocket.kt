package com.nowlink.mobile.net

import com.nowlink.mobile.model.NotificationPayload
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.WebSocket
import okhttp3.WebSocketListener
import org.json.JSONObject

class RelaySocket(private val client: OkHttpClient = OkHttpClient()) {
    private var socket: WebSocket? = null

    fun connect(host: String, port: Int, path: String) {
        val request = Request.Builder()
            .url("ws://$host:$port$path")
            .build()
        socket = client.newWebSocket(request, object : WebSocketListener() {})
    }

    fun send(payload: NotificationPayload) {
        val json = JSONObject()
            .put("eventId", payload.eventId)
            .put("phoneId", payload.phoneId)
            .put("category", payload.category)
            .put("packageName", payload.packageName)
            .put("appName", payload.appName)
            .put("title", payload.title)
            .put("body", payload.body)
            .put("receivedAt", payload.receivedAt)
            .put("iconRef", payload.iconRef)
            .put("conversationId", payload.conversationId)
            .put("phoneNumber", payload.phoneNumber)
            .put("callState", payload.callState)
            .put("smsSender", payload.smsSender)
            .put("priority", payload.priority)
        socket?.send(json.toString())
    }

    fun close() {
        socket?.close(1000, "bye")
        socket = null
    }
}

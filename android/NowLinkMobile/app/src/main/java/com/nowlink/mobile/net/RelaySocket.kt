package com.nowlink.mobile.net

import com.nowlink.mobile.model.NotificationPayload
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.WebSocket
import okhttp3.WebSocketListener
import okhttp3.Response
import org.json.JSONObject

class RelaySocket(private val client: OkHttpClient = OkHttpClient()) {
    private var socket: WebSocket? = null
    @Volatile private var connected = false

    fun connect(
        host: String,
        port: Int,
        path: String,
        onConnected: (() -> Unit)? = null,
        onDisconnected: ((String) -> Unit)? = null
    ) {
        val request = Request.Builder()
            .url("ws://$host:$port$path")
            .build()
        socket?.cancel()
        socket = client.newWebSocket(request, object : WebSocketListener() {
            override fun onOpen(webSocket: WebSocket, response: Response) {
                connected = true
                onConnected?.invoke()
            }

            override fun onClosed(webSocket: WebSocket, code: Int, reason: String) {
                connected = false
                onDisconnected?.invoke("closed:$code:$reason")
            }

            override fun onFailure(webSocket: WebSocket, t: Throwable, response: Response?) {
                connected = false
                onDisconnected?.invoke("failure:${t.message}")
            }
        })
    }

    fun send(payload: NotificationPayload): Boolean {
        if (!connected) return false
        return socket?.send(payload.toJson().toString()) == true
    }

    fun close() {
        connected = false
        socket?.close(1000, "bye")
        socket = null
    }

    fun isConnected(): Boolean = connected
}

package com.nowlink.mobile.model

import org.json.JSONObject

data class PairBootstrap(
    val deviceId: String,
    val deviceName: String,
    val host: String,
    val port: Int,
    val pairingCode: String,
    val fingerprint: String,
    val wsPath: String
)

data class PairConfirmRequest(
    val phoneId: String,
    val deviceName: String,
    val pairingCode: String,
    val host: String,
    val capabilities: List<String>
)

data class PairingConfig(
    val phoneId: String,
    val deviceName: String,
    val host: String,
    val port: Int,
    val wsPath: String,
    val pairingCode: String
)

data class NotificationPayload(
    val eventId: String,
    val phoneId: String,
    val category: String,
    val packageName: String,
    val appName: String,
    val title: String,
    val body: String,
    val receivedAt: String,
    val iconRef: String? = null,
    val conversationId: String? = null,
    val phoneNumber: String? = null,
    val callState: String? = null,
    val smsSender: String? = null,
    val priority: String = "default"
) {
    fun toJson(): JSONObject {
        return JSONObject()
            .put("eventId", eventId)
            .put("phoneId", phoneId)
            .put("category", category)
            .put("packageName", packageName)
            .put("appName", appName)
            .put("title", title)
            .put("body", body)
            .put("receivedAt", receivedAt)
            .put("iconRef", iconRef)
            .put("conversationId", conversationId)
            .put("phoneNumber", phoneNumber)
            .put("callState", callState)
            .put("smsSender", smsSender)
            .put("priority", priority)
    }

    companion object {
        fun fromJson(json: JSONObject): NotificationPayload {
            return NotificationPayload(
                eventId = json.optString("eventId"),
                phoneId = json.optString("phoneId"),
                category = json.optString("category"),
                packageName = json.optString("packageName"),
                appName = json.optString("appName"),
                title = json.optString("title"),
                body = json.optString("body"),
                receivedAt = json.optString("receivedAt"),
                iconRef = json.optString("iconRef").ifBlank { null },
                conversationId = json.optString("conversationId").ifBlank { null },
                phoneNumber = json.optString("phoneNumber").ifBlank { null },
                callState = json.optString("callState").ifBlank { null },
                smsSender = json.optString("smsSender").ifBlank { null },
                priority = json.optString("priority", "default")
            )
        }
    }
}

package com.nowlink.mobile.model

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
)

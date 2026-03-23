package com.nowlink.mobile.data

import android.content.Context
import android.os.Build
import java.util.UUID

class SettingsRepository(context: Context) {
    private val prefs = context.getSharedPreferences("nowlink", Context.MODE_PRIVATE)

    fun phoneId(): String {
        val existing = prefs.getString("phone_id", null)
        if (existing != null) return existing
        val generated = UUID.randomUUID().toString()
        prefs.edit().putString("phone_id", generated).apply()
        return generated
    }

    fun deviceName(): String = Build.MODEL ?: "Android"

    fun saveRelay(host: String, port: Int, wsPath: String, pairingCode: String) {
        prefs.edit()
            .putString("host", host)
            .putInt("port", port)
            .putString("ws_path", wsPath)
            .putString("pairing_code", pairingCode)
            .apply()
    }

    fun host(): String? = prefs.getString("host", null)
    fun port(): Int = prefs.getInt("port", 39876)
    fun wsPath(): String = prefs.getString("ws_path", "/events") ?: "/events"
    fun pairingCode(): String? = prefs.getString("pairing_code", null)
}

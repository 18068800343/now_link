package com.nowlink.mobile.data

import android.content.Context
import com.nowlink.mobile.model.NotificationPayload
import org.json.JSONArray
import org.json.JSONObject

class PendingEventStore(context: Context) {
    private val prefs = context.getSharedPreferences("nowlink_queue", Context.MODE_PRIVATE)
    private val key = "pending_events"
    private val gate = Any()

    fun enqueue(payload: NotificationPayload) {
        synchronized(gate) {
            val array = loadArray()
            array.put(payload.toJson())
            trim(array)
            saveArray(array)
        }
    }

    fun peek(limit: Int = 10): List<NotificationPayload> {
        synchronized(gate) {
            val array = loadArray()
            val result = mutableListOf<NotificationPayload>()
            val max = minOf(limit, array.length())
            for (index in 0 until max) {
                result.add(NotificationPayload.fromJson(array.getJSONObject(index)))
            }
            return result
        }
    }

    fun removeFirst(count: Int) {
        synchronized(gate) {
            val array = loadArray()
            val next = JSONArray()
            for (index in count until array.length()) {
                next.put(array.getJSONObject(index))
            }
            saveArray(next)
        }
    }

    fun size(): Int {
        synchronized(gate) {
            return loadArray().length()
        }
    }

    private fun loadArray(): JSONArray {
        val raw = prefs.getString(key, "[]") ?: "[]"
        return JSONArray(raw)
    }

    private fun saveArray(array: JSONArray) {
        prefs.edit().putString(key, array.toString()).apply()
    }

    private fun trim(array: JSONArray) {
        if (array.length() <= 200) return
        val trimmed = JSONArray()
        val start = array.length() - 200
        for (index in start until array.length()) {
            trimmed.put(array.getJSONObject(index))
        }
        saveArray(trimmed)
    }
}

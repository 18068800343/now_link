package com.nowlink.mobile.net

import com.nowlink.mobile.model.PairBootstrap
import com.nowlink.mobile.model.PairConfirmRequest
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import org.json.JSONArray
import org.json.JSONObject

class PairingApi(private val client: OkHttpClient = OkHttpClient()) {
    fun bootstrap(host: String, port: Int): PairBootstrap {
        val request = Request.Builder()
            .url("http://$host:$port/pair/bootstrap")
            .build()
        client.newCall(request).execute().use { response ->
            val json = JSONObject(response.body?.string().orEmpty())
            return PairBootstrap(
                deviceId = json.optString("deviceId"),
                deviceName = json.optString("deviceName"),
                host = json.optString("host"),
                port = json.optInt("port"),
                pairingCode = json.optString("pairingCode"),
                fingerprint = json.optString("fingerprint"),
                wsPath = json.optString("wsPath", "/events")
            )
        }
    }

    fun confirm(host: String, port: Int, requestBody: PairConfirmRequest): Boolean {
        val capabilities = JSONArray()
        requestBody.capabilities.forEach { capabilities.put(it) }

        val json = JSONObject()
            .put("phoneId", requestBody.phoneId)
            .put("deviceName", requestBody.deviceName)
            .put("pairingCode", requestBody.pairingCode)
            .put("host", requestBody.host)
            .put("capabilities", capabilities)

        val request = Request.Builder()
            .url("http://$host:$port/pair/confirm")
            .post(json.toString().toRequestBody("application/json".toMediaType()))
            .build()

        client.newCall(request).execute().use { response ->
            return JSONObject(response.body?.string().orEmpty()).optBoolean("accepted")
        }
    }
}

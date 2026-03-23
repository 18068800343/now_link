package com.nowlink.mobile.ui

import android.Manifest
import android.content.ComponentName
import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.os.PowerManager
import android.provider.Settings
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import com.google.zxing.integration.android.IntentIntegrator
import com.nowlink.mobile.R
import com.nowlink.mobile.data.SettingsRepository
import com.nowlink.mobile.databinding.ActivityMainBinding
import com.nowlink.mobile.model.PairConfirmRequest
import com.nowlink.mobile.net.PairingApi
import com.nowlink.mobile.service.NotificationRelayService
import com.nowlink.mobile.service.NowLinkNotificationListenerService
import org.json.JSONObject

class MainActivity : AppCompatActivity() {
    private lateinit var binding: ActivityMainBinding
    private lateinit var settings: SettingsRepository
    private val api = PairingApi()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        settings = SettingsRepository(this)
        setContentView(binding.root)

        binding.deviceNameValue.text = settings.deviceName()
        binding.phoneIdValue.text = settings.phoneId()
        refreshStatus(getString(R.string.status_ready))

        binding.grantNotificationButton.setOnClickListener {
            startActivity(Intent(Settings.ACTION_NOTIFICATION_LISTENER_SETTINGS))
        }

        binding.ignoreBatteryButton.setOnClickListener {
            val pm = getSystemService(POWER_SERVICE) as PowerManager
            if (!pm.isIgnoringBatteryOptimizations(packageName)) {
                startActivity(Intent(Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS, Uri.parse("package:$packageName")))
            }
        }

        binding.scanQrButton.setOnClickListener {
            IntentIntegrator(this).setOrientationLocked(false).initiateScan()
        }

        binding.manualPairButton.setOnClickListener {
            val host = binding.hostInput.text.toString().trim()
            val port = binding.portInput.text.toString().toIntOrNull() ?: 39876
            pair(host, port)
        }

        binding.startServiceButton.setOnClickListener {
            ContextCompat.startForegroundService(this, Intent(this, NotificationRelayService::class.java))
            refreshStatus(getString(R.string.status_service_started))
        }

        ActivityCompat.requestPermissions(
            this,
            arrayOf(
                Manifest.permission.POST_NOTIFICATIONS,
                Manifest.permission.READ_PHONE_STATE,
                Manifest.permission.RECEIVE_SMS,
                Manifest.permission.READ_SMS,
                Manifest.permission.CAMERA
            ),
            200
        )
    }

    override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
        val result = IntentIntegrator.parseActivityResult(requestCode, resultCode, data)
        if (result != null && !result.contents.isNullOrBlank()) {
            runCatching {
                val payload = parseQrPayload(result.contents)
                binding.hostInput.setText(payload.first)
                binding.portInput.setText(payload.second.toString())
                pair(payload.first, payload.second)
            }.onFailure {
                refreshStatus(getString(R.string.status_qr_invalid, it.message ?: "invalid payload"))
            }
            return
        }
        super.onActivityResult(requestCode, resultCode, data)
    }

    private fun pair(host: String, port: Int) {
        Thread {
            runCatching {
                val bootstrap = api.bootstrap(host, port)
                val accepted = api.confirm(
                    host,
                    port,
                    PairConfirmRequest(
                        phoneId = settings.phoneId(),
                        deviceName = settings.deviceName(),
                        pairingCode = bootstrap.pairingCode,
                        host = "android",
                        capabilities = listOf("notification", "sms", "call")
                    )
                )
                if (accepted) {
                    settings.saveRelay(host, port, bootstrap.wsPath, bootstrap.pairingCode)
                    ContextCompat.startForegroundService(this, Intent(this, NotificationRelayService::class.java))
                    runOnUiThread { refreshStatus(getString(R.string.status_paired, bootstrap.deviceName, host, port)) }
                } else {
                    runOnUiThread { refreshStatus(getString(R.string.status_pairing_rejected)) }
                }
            }.onFailure {
                runOnUiThread { refreshStatus(getString(R.string.status_pairing_failed, it.message ?: "unknown")) }
            }
        }.start()
    }

    private fun refreshStatus(prefix: String) {
        val enabled = Settings.Secure.getString(contentResolver, "enabled_notification_listeners")
            ?.contains(ComponentName(this, NowLinkNotificationListenerService::class.java).flattenToString()) == true
        val relay = settings.host()?.let { "$it:${settings.port()}" } ?: getString(R.string.relay_not_paired)
        binding.statusValue.text = getString(R.string.status_template, prefix, enabled.toString(), relay)
    }

    private fun parseQrPayload(raw: String): Pair<String, Int> {
        val normalized = if (raw.startsWith("nowlink://pair?data=")) {
            Uri.decode(raw.removePrefix("nowlink://pair?data="))
        } else {
            raw
        }
        val json = JSONObject(normalized)
        val host = json.optString("host")
        val port = json.optInt("port", 39876)
        if (host.isBlank()) {
            throw IllegalArgumentException(getString(R.string.error_missing_host))
        }
        return host to port
    }
}

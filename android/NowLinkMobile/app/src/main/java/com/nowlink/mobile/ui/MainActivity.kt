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
import com.nowlink.mobile.data.SettingsRepository
import com.nowlink.mobile.databinding.ActivityMainBinding
import com.nowlink.mobile.model.PairConfirmRequest
import com.nowlink.mobile.net.PairingApi
import com.nowlink.mobile.service.NotificationRelayService
import com.nowlink.mobile.service.NowLinkNotificationListenerService

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
        refreshStatus("Ready")

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
            refreshStatus("Foreground relay service started")
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
            val raw = result.contents
            val host = raw.substringAfter("\"host\":\"").substringBefore("\"")
            val port = raw.substringAfter("\"port\":").substringBefore(",").toIntOrNull() ?: 39876
            pair(host, port)
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
                    runOnUiThread { refreshStatus("Paired with ${bootstrap.deviceName} at $host:$port") }
                } else {
                    runOnUiThread { refreshStatus("Pairing rejected") }
                }
            }.onFailure {
                runOnUiThread { refreshStatus("Pairing failed: ${it.message}") }
            }
        }.start()
    }

    private fun refreshStatus(prefix: String) {
        val enabled = Settings.Secure.getString(contentResolver, "enabled_notification_listeners")
            ?.contains(ComponentName(this, NowLinkNotificationListenerService::class.java).flattenToString()) == true
        val relay = settings.host()?.let { "$it:${settings.port()}" } ?: "not paired"
        binding.statusValue.text = "$prefix\nNotification access=$enabled\nRelay=$relay"
    }
}

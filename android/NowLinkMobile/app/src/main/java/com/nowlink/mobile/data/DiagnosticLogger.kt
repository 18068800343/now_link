package com.nowlink.mobile.data

import android.content.Context
import java.io.File
import java.time.Instant

class DiagnosticLogger(context: Context) {
    private val file = File(context.filesDir, "nowlink-diagnostics.log")
    private val gate = Any()

    fun log(message: String) {
        synchronized(gate) {
            file.parentFile?.mkdirs()
            file.appendText("${Instant.now()} $message\n")
            trimIfNeeded()
        }
    }

    fun readLatest(maxLines: Int = 80): String {
        synchronized(gate) {
            if (!file.exists()) return ""
            return file.readLines().takeLast(maxLines).joinToString("\n")
        }
    }

    private fun trimIfNeeded() {
        if (!file.exists() || file.length() < 256 * 1024) return
        val lines = file.readLines().takeLast(200)
        file.writeText(lines.joinToString("\n") + "\n")
    }
}

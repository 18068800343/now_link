# NowLink

NowLink is a Wi-Fi first notification relay for Android to Windows.

## Layout

- `windows/NowLink.Service`: local pairing, HTTP/WebSocket ingest, storage, IPC
- `windows/NowLink.Tray`: tray app, settings window, desktop popup
- `windows/Shared`: shared contracts and JSON helpers
- `android/NowLinkMobile`: Android companion app
- `.github/workflows/android-apk.yml`: cloud APK build pipeline

## Notes

- Android communication is designed to work on the same LAN even if Bluetooth is unavailable.
- Bluetooth is optional and not required for pairing or notification relay.
- The current workspace does not have a complete desktop build toolchain installed, so Windows build is scripted with `csc`.

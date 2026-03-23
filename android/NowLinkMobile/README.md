# NowLinkMobile

Android companion app for NowLink.

## Main responsibilities

- Read notifications through `NotificationListenerService`
- Keep a foreground relay service alive
- Pair to Windows over the same LAN
- Push notifications over WebSocket

## Cloud packaging

The repository includes `.github/workflows/android-apk.yml` to build the APK in GitHub Actions.

## Required user grants

- Notification access
- Ignore battery optimization
- SMS permission
- Phone state permission
- Camera permission for QR pairing

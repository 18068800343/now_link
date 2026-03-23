using System;
using System.Collections.Generic;

namespace NowLink.Shared
{
    public class PairBootstrapResponse
    {
        public string deviceId { get; set; }
        public string deviceName { get; set; }
        public string host { get; set; }
        public int port { get; set; }
        public string pairingCode { get; set; }
        public string fingerprint { get; set; }
        public string wsPath { get; set; }
        public DateTime generatedAt { get; set; }
    }

    public class PairConfirmRequest
    {
        public string phoneId { get; set; }
        public string deviceName { get; set; }
        public string pairingCode { get; set; }
        public string host { get; set; }
        public List<string> capabilities { get; set; }
    }

    public class PairConfirmResponse
    {
        public bool accepted { get; set; }
        public string reason { get; set; }
    }

    public class DeviceRegistration
    {
        public string phoneId { get; set; }
        public string deviceName { get; set; }
        public string host { get; set; }
        public DateTime pairedAt { get; set; }
        public List<string> capabilities { get; set; }
    }

    public class NotificationEvent
    {
        public string eventId { get; set; }
        public string phoneId { get; set; }
        public string category { get; set; }
        public string packageName { get; set; }
        public string appName { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public DateTime receivedAt { get; set; }
        public string iconRef { get; set; }
        public string conversationId { get; set; }
        public string phoneNumber { get; set; }
        public string callState { get; set; }
        public string smsSender { get; set; }
        public string priority { get; set; }
    }

    public class ServiceEnvelope
    {
        public string type { get; set; }
        public string message { get; set; }
        public PairBootstrapResponse bootstrap { get; set; }
        public NotificationEvent notification { get; set; }
        public List<NotificationEvent> notifications { get; set; }
    }

    public class ServiceState
    {
        public string deviceId { get; set; }
        public string deviceName { get; set; }
        public int listenPort { get; set; }
        public string pairingCode { get; set; }
        public DateTime pairingGeneratedAt { get; set; }
        public List<DeviceRegistration> pairedDevices { get; set; }
        public List<NotificationEvent> notifications { get; set; }
    }
}

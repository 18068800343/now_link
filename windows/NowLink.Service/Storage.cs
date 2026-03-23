using System;
using System.Collections.Generic;
using System.Linq;
using NowLink.Shared;

namespace NowLink.Service
{
    public sealed class Storage
    {
        private readonly object _gate = new object();
        private readonly string _path;
        private ServiceState _state;

        public Storage(string path)
        {
            _path = path;
            _state = Json.Load<ServiceState>(path) ?? new ServiceState();
            if (string.IsNullOrWhiteSpace(_state.deviceId))
            {
                _state.deviceId = Guid.NewGuid().ToString("N");
                _state.deviceName = Environment.MachineName;
                _state.listenPort = 39876;
                _state.pairedDevices = new List<DeviceRegistration>();
                _state.notifications = new List<NotificationEvent>();
                RotatePairingCode();
                Persist();
            }
        }

        public ServiceState Snapshot()
        {
            lock (_gate)
            {
                return _state;
            }
        }

        public PairBootstrapResponse GenerateBootstrap(string host)
        {
            lock (_gate)
            {
                if (_state.pairingGeneratedAt < DateTime.UtcNow.AddMinutes(-10))
                {
                    RotatePairingCode();
                    Persist();
                }

                return new PairBootstrapResponse
                {
                    deviceId = _state.deviceId,
                    deviceName = _state.deviceName,
                    host = host,
                    port = _state.listenPort,
                    pairingCode = _state.pairingCode,
                    fingerprint = _state.deviceId.Substring(0, 12).ToUpperInvariant(),
                    wsPath = "/events",
                    generatedAt = _state.pairingGeneratedAt
                };
            }
        }

        public PairConfirmResponse Confirm(PairConfirmRequest request)
        {
            lock (_gate)
            {
                if (request == null || request.pairingCode != _state.pairingCode)
                {
                    return new PairConfirmResponse { accepted = false, reason = "invalid pairing code" };
                }

                var existing = _state.pairedDevices.FirstOrDefault(x => x.phoneId == request.phoneId);
                if (existing == null)
                {
                    existing = new DeviceRegistration
                    {
                        phoneId = request.phoneId,
                        deviceName = request.deviceName,
                        host = request.host,
                        pairedAt = DateTime.UtcNow,
                        capabilities = request.capabilities ?? new List<string>()
                    };
                    _state.pairedDevices.Add(existing);
                }
                else
                {
                    existing.deviceName = request.deviceName;
                    existing.host = request.host;
                    existing.capabilities = request.capabilities ?? new List<string>();
                }

                RotatePairingCode();
                Persist();
                return new PairConfirmResponse { accepted = true, reason = "paired" };
            }
        }

        public bool IsPaired(string phoneId)
        {
            lock (_gate)
            {
                return _state.pairedDevices.Any(x => x.phoneId == phoneId);
            }
        }

        public bool AddNotification(NotificationEvent notification)
        {
            lock (_gate)
            {
                if (_state.notifications.Any(x => x.eventId == notification.eventId))
                {
                    return false;
                }

                _state.notifications.Insert(0, notification);
                while (_state.notifications.Count > 100)
                {
                    _state.notifications.RemoveAt(_state.notifications.Count - 1);
                }

                Persist();
                return true;
            }
        }

        public List<NotificationEvent> LatestNotifications()
        {
            lock (_gate)
            {
                return _state.notifications.Take(50).ToList();
            }
        }

        private void RotatePairingCode()
        {
            _state.pairingCode = new Random().Next(100000, 999999).ToString();
            _state.pairingGeneratedAt = DateTime.UtcNow;
        }

        private void Persist()
        {
            Json.Save(_path, _state);
        }
    }
}

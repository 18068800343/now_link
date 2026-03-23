using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NowLink.Shared;

namespace NowLink.Service
{
    public sealed class ServiceHost : IDisposable
    {
        private readonly Storage _storage;
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts;
        private readonly List<NamedPipeServerStream> _pipes;
        private readonly string _dataDir;

        public ServiceHost()
        {
            _cts = new CancellationTokenSource();
            _pipes = new List<NamedPipeServerStream>();
            _dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NowLink");
            Directory.CreateDirectory(_dataDir);
            _storage = new Storage(Path.Combine(_dataDir, "state.json"));
            _listener = new HttpListener();
            AddListenerPrefixes(_listener, _storage.Snapshot().listenPort);
        }

        public void Start()
        {
            _listener.Start();
            Task.Run(() => AcceptPipeLoop(_cts.Token));
            Task.Run(() => HttpLoop(_cts.Token));
            Broadcast(new ServiceEnvelope
            {
                type = "historySnapshot",
                bootstrap = _storage.GenerateBootstrap(LocalHost()),
                notifications = _storage.LatestNotifications()
            });
        }

        private async Task HttpLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext context = null;
                try
                {
                    context = await _listener.GetContextAsync().ConfigureAwait(false);
                    Task.Run(() => HandleContext(context));
                }
                catch
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }

        private async Task HandleContext(HttpListenerContext context)
        {
            var path = context.Request.Url.AbsolutePath.TrimEnd('/').ToLowerInvariant();

            if (path == "/health")
            {
                await WriteJson(context, new { ok = true }).ConfigureAwait(false);
                return;
            }

            if (path == "/pair/bootstrap")
            {
                var bootstrap = _storage.GenerateBootstrap(LocalHost());
                await WriteJson(context, bootstrap).ConfigureAwait(false);
                Broadcast(new ServiceEnvelope { type = "bootstrap", bootstrap = bootstrap });
                return;
            }

            if (path == "/pair/confirm" && context.Request.HttpMethod == "POST")
            {
                string body;
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    body = reader.ReadToEnd();
                }

                var request = Json.Deserialize<PairConfirmRequest>(body);
                var result = _storage.Confirm(request);
                await WriteJson(context, result).ConfigureAwait(false);
                if (result.accepted)
                {
                    Broadcast(new ServiceEnvelope { type = "devicePaired", message = request.deviceName });
                }
                return;
            }

            if (path == "/events" && context.Request.IsWebSocketRequest)
            {
                await HandleSocket(context).ConfigureAwait(false);
                return;
            }

            context.Response.StatusCode = 404;
            context.Response.Close();
        }

        private async Task HandleSocket(HttpListenerContext context)
        {
            WebSocket socket = null;
            try
            {
                var ws = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
                socket = ws.WebSocket;
                var buffer = new ArraySegment<byte>(new byte[8192]);

                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(buffer, _cts.Token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None).ConfigureAwait(false);
                        break;
                    }

                    var json = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                    var notification = Json.Deserialize<NotificationEvent>(json);
                    if (notification == null || string.IsNullOrWhiteSpace(notification.phoneId))
                    {
                        continue;
                    }

                    if (!_storage.IsPaired(notification.phoneId))
                    {
                        continue;
                    }

                    if (notification.receivedAt == default(DateTime))
                    {
                        notification.receivedAt = DateTime.UtcNow;
                    }

                    if (_storage.AddNotification(notification))
                    {
                        Broadcast(new ServiceEnvelope { type = "notificationReceived", notification = notification });
                    }
                }
            }
            catch
            {
            }
            finally
            {
                if (socket != null)
                {
                    socket.Dispose();
                }
            }
        }

        private async Task AcceptPipeLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var pipe = new NamedPipeServerStream("NowLink.Pipe", PipeDirection.Out, 5, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                try
                {
                    await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);
                    lock (_pipes)
                    {
                        _pipes.Add(pipe);
                    }
                }
                catch
                {
                    pipe.Dispose();
                }
            }
        }

        private void Broadcast(ServiceEnvelope envelope)
        {
            var data = Encoding.UTF8.GetBytes(Json.Serialize(envelope) + Environment.NewLine);
            lock (_pipes)
            {
                for (var i = _pipes.Count - 1; i >= 0; i--)
                {
                    var pipe = _pipes[i];
                    try
                    {
                        if (!pipe.IsConnected)
                        {
                            pipe.Dispose();
                            _pipes.RemoveAt(i);
                            continue;
                        }

                        pipe.Write(data, 0, data.Length);
                        pipe.Flush();
                    }
                    catch
                    {
                        pipe.Dispose();
                        _pipes.RemoveAt(i);
                    }
                }
            }
        }

        private static string LocalHost()
        {
            foreach (var address in GetLocalIPv4Addresses())
            {
                return address.ToString();
            }

            return "127.0.0.1";
        }

        private static void AddListenerPrefixes(HttpListener listener, int port)
        {
            listener.Prefixes.Add(string.Format("http://127.0.0.1:{0}/", port));
            listener.Prefixes.Add(string.Format("http://localhost:{0}/", port));

            foreach (var address in GetLocalIPv4Addresses())
            {
                listener.Prefixes.Add(string.Format("http://{0}:{1}/", address, port));
            }
        }

        private static List<IPAddress> GetLocalIPv4Addresses()
        {
            var result = new List<IPAddress>();
            var host = Dns.GetHostName();
            var addresses = Dns.GetHostAddresses(host);
            foreach (var address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                {
                    result.Add(address);
                }
            }

            return result;
        }

        private static Task WriteJson(HttpListenerContext context, object payload)
        {
            var bytes = Encoding.UTF8.GetBytes(Json.Serialize(payload));
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.Close();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            _listener.Close();
            lock (_pipes)
            {
                foreach (var pipe in _pipes)
                {
                    pipe.Dispose();
                }
                _pipes.Clear();
            }
        }
    }
}

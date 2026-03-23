using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NowLink.Shared;

namespace NowLink.Tray
{
    public sealed class PipeClient : IDisposable
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public event Action<ServiceEnvelope> OnMessage;

        public void Start()
        {
            Task.Run(() => ConnectLoop(_cts.Token));
        }

        private async Task ConnectLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using (var pipe = new NamedPipeClientStream(".", "NowLink.Pipe", PipeDirection.In))
                    using (var reader = new StreamReader(pipe, Encoding.UTF8))
                    {
                        pipe.Connect(1500);
                        while (!token.IsCancellationRequested && pipe.IsConnected)
                        {
                            var line = await reader.ReadLineAsync().ConfigureAwait(false);
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                break;
                            }

                            var envelope = Json.Deserialize<ServiceEnvelope>(line);
                            if (envelope != null && OnMessage != null)
                            {
                                OnMessage(envelope);
                            }
                        }
                    }
                }
                catch
                {
                    if (!token.IsCancellationRequested)
                    {
                        Task.Delay(1500).Wait();
                    }
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
        }
    }
}

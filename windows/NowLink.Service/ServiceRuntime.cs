using System.ServiceProcess;

namespace NowLink.Service
{
    public sealed class ServiceRuntime : ServiceBase
    {
        private ServiceHost _host;

        public ServiceRuntime()
        {
            ServiceName = "NowLinkService";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            _host = new ServiceHost();
            _host.Start();
        }

        protected override void OnStop()
        {
            if (_host != null)
            {
                _host.Dispose();
                _host = null;
            }
        }
    }
}

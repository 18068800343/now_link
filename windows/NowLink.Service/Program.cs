using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;

namespace NowLink.Service
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Any(x => string.Equals(x, "--install", StringComparison.OrdinalIgnoreCase)))
            {
                InstallService();
                return;
            }

            if (args.Any(x => string.Equals(x, "--uninstall", StringComparison.OrdinalIgnoreCase)))
            {
                UninstallService();
                return;
            }

            if (!Environment.UserInteractive)
            {
                ServiceBase.Run(new ServiceRuntime());
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BackgroundServiceContext());
        }

        private static void InstallService()
        {
            var exePath = Process.GetCurrentProcess().MainModule.FileName;
            RunSc(string.Format("create NowLinkService start= auto binPath= \"\\\"{0}\\\"\"", exePath));
            RunSc("description NowLinkService \"NowLink background notification relay service\"");
        }

        private static void UninstallService()
        {
            RunSc("stop NowLinkService");
            Thread.Sleep(1000);
            RunSc("delete NowLinkService");
        }

        private static void RunSc(string arguments)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process.WaitForExit();
        }
    }

    internal sealed class BackgroundServiceContext : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ServiceHost _host;

        public BackgroundServiceContext()
        {
            _host = new ServiceHost();
            _host.Start();

            var menu = new ContextMenuStrip();
            menu.Items.Add("Open Tray", null, (s, e) => LaunchTray());
            menu.Items.Add("Exit Service Host", null, (s, e) => ExitThread());

            _notifyIcon = new NotifyIcon
            {
                Visible = true,
                Text = "NowLink Service",
                Icon = System.Drawing.SystemIcons.Shield,
                ContextMenuStrip = menu
            };
        }

        private static void LaunchTray()
        {
            Process.Start("NowLink.Tray.exe");
        }

        protected override void ExitThreadCore()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _host.Dispose();
            base.ExitThreadCore();
        }
    }
}

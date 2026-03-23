using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

namespace NowLink.Tray
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ServicePointManager.SecurityProtocol =
                (SecurityProtocolType)3072 |
                SecurityProtocolType.Tls11 |
                SecurityProtocolType.Tls;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayContext());
        }
    }

    public sealed class TrayContext : ApplicationContext
    {
        private readonly NotifyIcon _icon;
        private readonly MainForm _mainForm;

        public TrayContext()
        {
            _mainForm = new MainForm();
            var menu = new ContextMenuStrip();
            menu.Items.Add("Open", null, (s, e) => ShowMain());
            menu.Items.Add("Exit", null, (s, e) => ExitThread());

            _icon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "NowLink",
                Visible = true,
                ContextMenuStrip = menu
            };
            _icon.DoubleClick += (s, e) => ShowMain();
        }

        private void ShowMain()
        {
            if (!_mainForm.Visible)
            {
                _mainForm.Show();
            }

            _mainForm.WindowState = FormWindowState.Normal;
            _mainForm.Activate();
        }

        protected override void ExitThreadCore()
        {
            _icon.Visible = false;
            _icon.Dispose();
            _mainForm.Dispose();
            base.ExitThreadCore();
        }
    }
}

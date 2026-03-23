using System;
using System.Drawing;
using System.Windows.Forms;
using NowLink.Shared;

namespace NowLink.Tray
{
    public sealed class PopupForm : Form
    {
        private readonly Timer _timer;

        public PopupForm(NotificationEvent notification)
        {
            Width = 380;
            Height = 140;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.FromArgb(24, 28, 37);

            var title = new Label
            {
                Left = 20,
                Top = 18,
                Width = 320,
                Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                Text = string.IsNullOrWhiteSpace(notification.title) ? notification.appName : notification.title
            };

            var meta = new Label
            {
                Left = 20,
                Top = 46,
                Width = 320,
                ForeColor = Color.FromArgb(170, 182, 198),
                Text = string.Format("{0}  {1}", notification.appName, TranslateCategory(notification.category))
            };

            var body = new Label
            {
                Left = 20,
                Top = 72,
                Width = 332,
                Height = 40,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.White,
                Text = notification.body
            };

            Controls.Add(title);
            Controls.Add(meta);
            Controls.Add(body);

            var area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - Width - 18, area.Bottom - Height - 18);

            _timer = new Timer { Interval = 6000 };
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                Close();
            };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _timer.Start();
        }

        private static string TranslateCategory(string category)
        {
            if (category == "sms")
            {
                return Localization.Text("SMS", "短信");
            }

            if (category == "call")
            {
                return Localization.Text("Call", "来电");
            }

            return Localization.Text("App", "应用");
        }
    }
}

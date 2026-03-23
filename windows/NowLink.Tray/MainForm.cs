using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using NowLink.Shared;

namespace NowLink.Tray
{
    public sealed class MainForm : Form
    {
        private readonly PipeClient _pipeClient;
        private readonly TextBox _pairingBox;
        private readonly ListBox _deviceBox;
        private readonly ListView _history;
        private readonly Label _status;

        public MainForm()
        {
            Text = "NowLink";
            Width = 980;
            Height = 700;
            BackColor = Color.FromArgb(244, 247, 252);
            Font = new Font("Segoe UI", 9.5f);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(layout);

            var nav = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(21, 30, 45) };
            nav.Controls.Add(new Label
            {
                Left = 22,
                Top = 28,
                Width = 160,
                Text = "NowLink",
                Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
                ForeColor = Color.White
            });
            nav.Controls.Add(new Label
            {
                Left = 22,
                Top = 62,
                Width = 170,
                Text = "Modern Wi-Fi notification relay",
                ForeColor = Color.FromArgb(163, 177, 196)
            });
            layout.Controls.Add(nav, 0, 0);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            layout.Controls.Add(tabs, 1, 0);

            var devices = new TabPage("Devices");
            var notifications = new TabPage("Notifications");
            var appearance = new TabPage("Appearance");
            var advanced = new TabPage("Advanced");
            tabs.TabPages.Add(devices);
            tabs.TabPages.Add(notifications);
            tabs.TabPages.Add(appearance);
            tabs.TabPages.Add(advanced);

            _status = new Label
            {
                Left = 24,
                Top = 24,
                Width = 480,
                Text = "Waiting for local service"
            };
            devices.Controls.Add(_status);

            var refresh = new Button
            {
                Left = 24,
                Top = 56,
                Width = 180,
                Height = 36,
                Text = "Refresh Pair Payload"
            };
            refresh.Click += (s, e) => RefreshBootstrap();
            devices.Controls.Add(refresh);

            _pairingBox = new TextBox
            {
                Left = 24,
                Top = 108,
                Width = 540,
                Height = 130,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            devices.Controls.Add(_pairingBox);

            _deviceBox = new ListBox
            {
                Left = 24,
                Top = 258,
                Width = 540,
                Height = 250
            };
            devices.Controls.Add(_deviceBox);

            _history = new ListView
            {
                Left = 18,
                Top = 18,
                Width = 710,
                Height = 520,
                View = View.Details,
                FullRowSelect = true
            };
            _history.Columns.Add("Time", 150);
            _history.Columns.Add("App", 140);
            _history.Columns.Add("Title", 180);
            _history.Columns.Add("Body", 220);
            notifications.Controls.Add(_history);

            appearance.Controls.Add(new Label
            {
                Left = 24,
                Top = 24,
                Width = 400,
                Text = "Theme, popup duration, and compact mode settings live here."
            });
            advanced.Controls.Add(new Label
            {
                Left = 24,
                Top = 24,
                Width = 430,
                Text = "Manual IP:Port fallback, logs, startup, and diagnostics live here."
            });

            _pipeClient = new PipeClient();
            _pipeClient.OnMessage += HandleEnvelope;
            _pipeClient.Start();

            FormClosing += (s, e) =>
            {
                e.Cancel = true;
                Hide();
            };
        }

        private void RefreshBootstrap()
        {
            try
            {
                using (var client = new WebClient())
                {
                    _pairingBox.Text = client.DownloadString("http://127.0.0.1:39876/pair/bootstrap");
                    _status.Text = "Loaded pair payload from local service";
                }
            }
            catch (Exception ex)
            {
                _status.Text = "Service unavailable: " + ex.Message;
            }
        }

        private void HandleEnvelope(ServiceEnvelope envelope)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<ServiceEnvelope>(HandleEnvelope), envelope);
                return;
            }

            if (envelope.type == "historySnapshot")
            {
                _status.Text = "Connected to local service";
                if (envelope.bootstrap != null)
                {
                    _pairingBox.Text = Json.Serialize(envelope.bootstrap);
                }
                ReplaceHistory(envelope.notifications);
            }
            else if (envelope.type == "bootstrap" && envelope.bootstrap != null)
            {
                _pairingBox.Text = Json.Serialize(envelope.bootstrap);
            }
            else if (envelope.type == "devicePaired")
            {
                _deviceBox.Items.Add(envelope.message);
            }
            else if (envelope.type == "notificationReceived" && envelope.notification != null)
            {
                AppendHistory(envelope.notification);
                var popup = new PopupForm(envelope.notification);
                popup.Show();
            }
        }

        private void ReplaceHistory(List<NotificationEvent> notifications)
        {
            _history.Items.Clear();
            if (notifications == null) return;
            foreach (var item in notifications)
            {
                AppendHistory(item);
            }
        }

        private void AppendHistory(NotificationEvent notification)
        {
            var row = new ListViewItem(notification.receivedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            row.SubItems.Add(notification.appName ?? "");
            row.SubItems.Add(notification.title ?? "");
            row.SubItems.Add(notification.body ?? "");
            _history.Items.Insert(0, row);
        }
    }
}

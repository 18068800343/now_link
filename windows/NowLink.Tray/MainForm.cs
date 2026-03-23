using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Web;
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
        private readonly PictureBox _qrCode;

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
                Text = Localization.Text("Modern Wi-Fi notification", "现代 Wi-Fi 通知转发"),
                ForeColor = Color.FromArgb(163, 177, 196)
            });
            layout.Controls.Add(nav, 0, 0);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            layout.Controls.Add(tabs, 1, 0);

            var devices = new TabPage(Localization.Text("Devices", "设备"));
            var notifications = new TabPage(Localization.Text("Notifications", "通知"));
            var appearance = new TabPage(Localization.Text("Appearance", "外观"));
            var advanced = new TabPage(Localization.Text("Advanced", "高级"));
            tabs.TabPages.Add(devices);
            tabs.TabPages.Add(notifications);
            tabs.TabPages.Add(appearance);
            tabs.TabPages.Add(advanced);

            _status = new Label
            {
                Left = 24,
                Top = 24,
                Width = 480,
                Text = Localization.Text("Waiting for local service", "等待本地服务启动")
            };
            devices.Controls.Add(_status);

            var refresh = new Button
            {
                Left = 24,
                Top = 56,
                Width = 220,
                Height = 36,
                Text = Localization.Text("Refresh Pair QR", "刷新配对二维码")
            };
            refresh.Click += (s, e) => RefreshBootstrap();
            devices.Controls.Add(refresh);

            _qrCode = new PictureBox
            {
                Left = 24,
                Top = 108,
                Width = 220,
                Height = 220,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            devices.Controls.Add(_qrCode);

            _pairingBox = new TextBox
            {
                Left = 264,
                Top = 108,
                Width = 300,
                Height = 220,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            devices.Controls.Add(_pairingBox);

            _deviceBox = new ListBox
            {
                Left = 24,
                Top = 350,
                Width = 540,
                Height = 200
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
            _history.Columns.Add(Localization.Text("Time", "时间"), 150);
            _history.Columns.Add(Localization.Text("App", "应用"), 140);
            _history.Columns.Add(Localization.Text("Title", "标题"), 180);
            _history.Columns.Add(Localization.Text("Body", "内容"), 220);
            notifications.Controls.Add(_history);

            appearance.Controls.Add(new Label
            {
                Left = 24,
                Top = 24,
                Width = 400,
                Text = Localization.Text("Theme, popup duration, and compact mode settings live here.", "主题、弹窗时长和紧凑模式设置放在这里。")
            });
            advanced.Controls.Add(new Label
            {
                Left = 24,
                Top = 24,
                Width = 430,
                Text = Localization.Text("Manual IP:Port fallback, logs, startup, and diagnostics live here.", "手动 IP:Port 配对、日志、开机启动和诊断放在这里。")
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
                    UpdateQrCode(_pairingBox.Text);
                    _status.Text = Localization.Text("Loaded pair payload from local service", "已从本地服务加载配对内容");
                }
            }
            catch (Exception ex)
            {
                _status.Text = Localization.Text("Service unavailable: ", "服务不可用：") + ex.Message;
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
                _status.Text = Localization.Text("Connected to local service", "已连接到本地服务");
                if (envelope.bootstrap != null)
                {
                    _pairingBox.Text = Json.Serialize(envelope.bootstrap);
                    UpdateQrCode(_pairingBox.Text);
                }
                ReplaceHistory(envelope.notifications);
            }
            else if (envelope.type == "bootstrap" && envelope.bootstrap != null)
            {
                _pairingBox.Text = Json.Serialize(envelope.bootstrap);
                UpdateQrCode(_pairingBox.Text);
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

        private void UpdateQrCode(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            var encoded = HttpUtility.UrlEncode(payload);
            _qrCode.LoadAsync("https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=" + encoded);
        }
    }
}

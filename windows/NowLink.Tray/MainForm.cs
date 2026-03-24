using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Cache;
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
        private readonly TabControl _tabs;
        private readonly Label _detailHeader;
        private readonly Label _detailMeta;
        private readonly TextBox _detailBody;

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

            _tabs = new TabControl { Dock = DockStyle.Fill };
            layout.Controls.Add(_tabs, 1, 0);

            var devices = new TabPage(Localization.Text("Devices", "设备"));
            var notifications = new TabPage(Localization.Text("Notifications", "通知"));
            var appearance = new TabPage(Localization.Text("Appearance", "外观"));
            var advanced = new TabPage(Localization.Text("Advanced", "高级"));
            _tabs.TabPages.Add(devices);
            _tabs.TabPages.Add(notifications);
            _tabs.TabPages.Add(appearance);
            _tabs.TabPages.Add(advanced);

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
                Height = 280,
                View = View.Details,
                FullRowSelect = true
            };
            _history.Columns.Add(Localization.Text("Time", "时间"), 150);
            _history.Columns.Add(Localization.Text("App", "应用"), 140);
            _history.Columns.Add(Localization.Text("Title", "标题"), 180);
            _history.Columns.Add(Localization.Text("Body", "内容"), 220);
            _history.SelectedIndexChanged += (s, e) => ShowSelectedNotificationDetail();
            notifications.Controls.Add(_history);

            var detailPanel = new Panel
            {
                Left = 18,
                Top = 320,
                Width = 710,
                Height = 220,
                BackColor = Color.White
            };
            notifications.Controls.Add(detailPanel);

            _detailHeader = new Label
            {
                Left = 18,
                Top = 18,
                Width = 560,
                Height = 28,
                Font = new Font("Segoe UI Semibold", 12.5f, FontStyle.Bold),
                Text = Localization.Text("Select a notification", "请选择一条通知")
            };
            detailPanel.Controls.Add(_detailHeader);

            _detailMeta = new Label
            {
                Left = 18,
                Top = 52,
                Width = 640,
                Height = 22,
                ForeColor = Color.FromArgb(90, 102, 119),
                Text = Localization.Text("Details will appear here.", "通知详情会显示在这里。")
            };
            detailPanel.Controls.Add(_detailMeta);

            _detailBody = new TextBox
            {
                Left = 18,
                Top = 86,
                Width = 670,
                Height = 112,
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ScrollBars = ScrollBars.Vertical
            };
            detailPanel.Controls.Add(_detailBody);

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
                var popup = new PopupForm(envelope.notification, OpenNotificationFromPopup);
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
            row.Tag = notification;
            _history.Items.Insert(0, row);
        }

        private void ShowSelectedNotificationDetail()
        {
            if (_history.SelectedItems.Count == 0)
            {
                return;
            }

            var notification = _history.SelectedItems[0].Tag as NotificationEvent;
            if (notification == null)
            {
                return;
            }

            _detailHeader.Text = string.IsNullOrWhiteSpace(notification.title) ? notification.appName : notification.title;
            _detailMeta.Text = string.Format(
                "{0}  |  {1}  |  {2}",
                notification.appName ?? Localization.Text("App", "应用"),
                TranslateCategory(notification.category),
                notification.receivedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            _detailBody.Text = string.IsNullOrWhiteSpace(notification.body)
                ? Localization.Text("No detail body.", "没有详情内容。")
                : notification.body;
        }

        private void OpenNotificationFromPopup(NotificationEvent notification)
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            _tabs.SelectedIndex = 1;

            foreach (ListViewItem item in _history.Items)
            {
                var rowNotification = item.Tag as NotificationEvent;
                if (rowNotification != null && rowNotification.eventId == notification.eventId)
                {
                    item.Selected = true;
                    item.Focused = true;
                    item.EnsureVisible();
                    ShowSelectedNotificationDetail();
                    return;
                }
            }

            AppendHistory(notification);
            if (_history.Items.Count > 0)
            {
                _history.Items[0].Selected = true;
                _history.Items[0].Focused = true;
                ShowSelectedNotificationDetail();
            }
        }

        private void UpdateQrCode(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                _qrCode.Image = null;
                return;
            }

            try
            {
                var encoded = Uri.EscapeDataString(payload);
                var loaded = TryLoadRemoteQr("https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=" + encoded)
                    || TryLoadRemoteQr("http://api.qrserver.com/v1/create-qr-code/?size=220x220&data=" + encoded);

                if (!loaded)
                {
                    throw new WebException(Localization.Text("Remote QR service returned no image.", "二维码服务没有返回图片。"));
                }
            }
            catch (Exception ex)
            {
                _qrCode.Image = null;
                _status.Text = Localization.Text("QR download failed: ", "二维码下载失败：") + ex.Message;
            }
        }

        private bool TryLoadRemoteQr(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Proxy = WebRequest.GetSystemWebProxy();
            request.Proxy.Credentials = CredentialCache.DefaultCredentials;
            request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.Timeout = 15000;
            request.ReadWriteTimeout = 15000;
            request.UserAgent = "NowLink/1.0";

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var memory = new MemoryStream())
                {
                    if (stream == null)
                    {
                        return false;
                    }

                    stream.CopyTo(memory);
                    memory.Position = 0;
                    var bitmap = Image.FromStream(memory);
                    var old = _qrCode.Image;
                    _qrCode.Image = new Bitmap(bitmap);
                    if (old != null)
                    {
                        old.Dispose();
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }
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

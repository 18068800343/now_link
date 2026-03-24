using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using NowLink.Shared;

namespace NowLink.Tray
{
    public sealed class PopupForm : Form
    {
        private readonly Timer _lifeTimer;
        private readonly Timer _animationTimer;
        private readonly NotificationEvent _notification;
        private readonly Action<NotificationEvent> _onOpen;
        private Point _targetLocation;
        private int _animationTick;
        private bool _closing;

        public PopupForm(NotificationEvent notification, Action<NotificationEvent> onOpen)
        {
            _notification = notification;
            _onOpen = onOpen;

            Width = 356;
            Height = 136;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(239, 245, 250);
            Opacity = 0d;
            Cursor = Cursors.Hand;

            var area = Screen.PrimaryScreen.WorkingArea;
            _targetLocation = new Point(area.Right - Width - 20, area.Bottom - Height - 26);
            Location = new Point(_targetLocation.X + 22, _targetLocation.Y + 14);

            _lifeTimer = new Timer { Interval = 4200 };
            _lifeTimer.Tick += (s, e) =>
            {
                _lifeTimer.Stop();
                BeginCloseAnimation();
            };

            _animationTimer = new Timer { Interval = 15 };
            _animationTimer.Tick += AnimateFrame;

            Click += HandleOpen;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            using (var path = CreateRoundRect(new Rectangle(0, 0, Width, Height), 26))
            {
                Region = new Region(path);
            }
            _animationTick = 0;
            _closing = false;
            _animationTimer.Start();
            _lifeTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var basePath = CreateRoundRect(bounds, 26))
            {
                using (var baseBrush = new SolidBrush(Color.FromArgb(244, 248, 252)))
                {
                    g.FillPath(baseBrush, basePath);
                }

                using (var outlinePen = new Pen(Color.FromArgb(198, 215, 229), 1f))
                {
                    g.DrawPath(outlinePen, basePath);
                }
            }

            var shadowRect = new Rectangle(8, 10, Width - 18, Height - 20);
            for (var i = 0; i < 5; i++)
            {
                using (var shadowPath = CreateRoundRect(new Rectangle(shadowRect.X - i, shadowRect.Y - i, shadowRect.Width + i, shadowRect.Height + i), 24))
                using (var shadowBrush = new SolidBrush(Color.FromArgb(Math.Max(0, 18 - i * 3), 80, 96, 122)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }
            }

            var cardRect = new Rectangle(6, 6, Width - 12, Height - 12);
            using (var cardPath = CreateRoundRect(cardRect, 22))
            using (var fill = new LinearGradientBrush(cardRect, Color.FromArgb(249, 252, 255), Color.FromArgb(236, 242, 248), 90f))
            {
                g.FillPath(fill, cardPath);
                using (var pen = new Pen(Color.FromArgb(162, 255, 255, 255), 1f))
                {
                    g.DrawPath(pen, cardPath);
                }
            }

            using (var sheen = new GraphicsPath())
            {
                sheen.AddEllipse(cardRect.X - 22, cardRect.Y - 56, 220, 120);
                using (var sheenBrush = new PathGradientBrush(sheen))
                {
                    sheenBrush.CenterColor = Color.FromArgb(86, 255, 255, 255);
                    sheenBrush.SurroundColors = new[] { Color.FromArgb(0, 255, 255, 255) };
                    g.FillPath(sheenBrush, sheen);
                }
            }

            DrawHeader(g, cardRect);
            DrawBody(g, cardRect);
        }

        private void DrawHeader(Graphics g, Rectangle cardRect)
        {
            var iconRect = new Rectangle(cardRect.X + 18, cardRect.Y + 16, 38, 38);
            using (var iconFill = new LinearGradientBrush(iconRect, Color.FromArgb(37, 45, 62), Color.FromArgb(82, 96, 124), 90f))
            {
                g.FillEllipse(iconFill, iconRect);
            }

            using (var symbolFont = new Font("Segoe UI Semibold", 15f, FontStyle.Bold))
            using (var symbolBrush = new SolidBrush(Color.White))
            {
                var categoryGlyph = CategoryGlyph(_notification.category);
                var glyphSize = g.MeasureString(categoryGlyph, symbolFont);
                g.DrawString(categoryGlyph, symbolFont, symbolBrush, iconRect.X + (iconRect.Width - glyphSize.Width) / 2f, iconRect.Y + (iconRect.Height - glyphSize.Height) / 2f - 1f);
            }

            using (var appFont = new Font("Segoe UI Semibold", 9.2f, FontStyle.Bold))
            using (var metaBrush = new SolidBrush(Color.FromArgb(76, 88, 107)))
            {
                var appText = string.IsNullOrWhiteSpace(_notification.appName) ? Localization.Text("Notification", "通知") : _notification.appName;
                g.DrawString(appText, appFont, metaBrush, cardRect.X + 68, cardRect.Y + 16);
            }

            using (var timeFont = new Font("Segoe UI", 8.6f))
            using (var timeBrush = new SolidBrush(Color.FromArgb(118, 130, 149)))
            {
                var timeText = _notification.receivedAt == default(DateTime)
                    ? DateTime.Now.ToString("HH:mm")
                    : _notification.receivedAt.ToLocalTime().ToString("HH:mm");
                var size = g.MeasureString(timeText, timeFont);
                g.DrawString(timeText, timeFont, timeBrush, cardRect.Right - size.Width - 20, cardRect.Y + 18);
            }

            var chipRect = new Rectangle(cardRect.X + 68, cardRect.Y + 38, 72, 20);
            using (var chipPath = CreateRoundRect(chipRect, 10))
            using (var chipBrush = new SolidBrush(Color.FromArgb(232, 238, 246)))
            {
                g.FillPath(chipBrush, chipPath);
            }

            using (var chipFont = new Font("Segoe UI", 8.2f, FontStyle.Bold))
            using (var chipTextBrush = new SolidBrush(Color.FromArgb(78, 91, 112)))
            {
                var chip = TranslateCategory(_notification.category);
                var size = g.MeasureString(chip, chipFont);
                g.DrawString(chip, chipFont, chipTextBrush, chipRect.X + (chipRect.Width - size.Width) / 2f, chipRect.Y + 3f);
            }
        }

        private void DrawBody(Graphics g, Rectangle cardRect)
        {
            var titleRect = new RectangleF(cardRect.X + 18, cardRect.Y + 66, cardRect.Width - 36, 28);
            using (var titleFont = new Font("Segoe UI Semibold", 13f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(24, 31, 43)))
            {
                var title = string.IsNullOrWhiteSpace(_notification.title) ? _notification.appName : _notification.title;
                g.DrawString(title, titleFont, titleBrush, titleRect);
            }

            var bodyRect = new RectangleF(cardRect.X + 18, cardRect.Y + 94, cardRect.Width - 36, 36);
            using (var bodyFont = new Font("Segoe UI", 10.2f))
            using (var bodyBrush = new SolidBrush(Color.FromArgb(72, 82, 100)))
            {
                var body = string.IsNullOrWhiteSpace(_notification.body)
                    ? Localization.Text("Open NowLink to view the full message.", "打开 NowLink 查看完整内容。")
                    : _notification.body;
                g.DrawString(body, bodyFont, bodyBrush, bodyRect);
            }

            using (var accentPen = new Pen(Color.FromArgb(80, 122, 160, 214), 2f))
            {
                g.DrawLine(accentPen, cardRect.X + 18, cardRect.Bottom - 16, cardRect.X + 118, cardRect.Bottom - 16);
            }
        }

        private void AnimateFrame(object sender, EventArgs e)
        {
            _animationTick++;
            if (!_closing)
            {
                var progress = Math.Min(1d, _animationTick / 12d);
                Opacity = 0.25d + progress * 0.75d;
                Location = Lerp(new Point(_targetLocation.X + 24, _targetLocation.Y + 16), _targetLocation, EaseOutCubic(progress));
                if (progress >= 1d)
                {
                    _animationTimer.Stop();
                }
                return;
            }

            var closeProgress = Math.Min(1d, _animationTick / 10d);
            Opacity = Math.Max(0d, 1d - closeProgress);
            Location = Lerp(_targetLocation, new Point(_targetLocation.X + 18, _targetLocation.Y - 8), EaseInCubic(closeProgress));
            if (closeProgress >= 1d)
            {
                _animationTimer.Stop();
                Close();
            }
        }

        private void HandleOpen(object sender, EventArgs e)
        {
            _lifeTimer.Stop();
            _animationTimer.Stop();
            if (_onOpen != null)
            {
                _onOpen(_notification);
            }
            Close();
        }

        private void BeginCloseAnimation()
        {
            if (_closing)
            {
                return;
            }

            _closing = true;
            _animationTick = 0;
            _animationTimer.Start();
        }

        private static string TranslateCategory(string category)
        {
            if (category == "sms")
            {
                return Localization.Text("MESSAGE", "短信");
            }

            if (category == "call")
            {
                return Localization.Text("CALL", "来电");
            }

            return Localization.Text("APP", "应用");
        }

        private static string CategoryGlyph(string category)
        {
            if (category == "sms")
            {
                return "✉";
            }

            if (category == "call")
            {
                return "☎";
            }

            return "•";
        }

        private static GraphicsPath CreateRoundRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Point Lerp(Point from, Point to, double progress)
        {
            return new Point(
                from.X + (int)((to.X - from.X) * progress),
                from.Y + (int)((to.Y - from.Y) * progress));
        }

        private static double EaseOutCubic(double x)
        {
            return 1 - Math.Pow(1 - x, 3);
        }

        private static double EaseInCubic(double x)
        {
            return x * x * x;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            HandleOpen(this, EventArgs.Empty);
        }
    }
}

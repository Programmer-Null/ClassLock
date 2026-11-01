using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClassLock
{
    public sealed class LockForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE     = 0x0001;
        private const uint SWP_NOMOVE     = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

        public bool AllowClose { get; set; }
        public event EventHandler UnlockRequested;

        private readonly Screen _screen;
        private readonly Timer _clockTimer;
        private readonly Timer _topmostTimer;
        private readonly Button _unlockButton;

        // 【新增】提前准备好的解锁组件
        private Panel _loginPanel;
        private TextBox _txtPwd;
        private Label _lblErr;
        private Button _btnSubmit;
        private Button _btnCancel;

        private readonly Image _backgroundImage;

        private Font _fontTime;
        private Font _fontDate;
        private Font _fontCardTitle;
        private Font _fontCardContent;
        private Font _fontTopRight;

        public LockForm(Screen screen, string imagePath)
        {
            _screen = screen;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.Manual;
            ShowInTaskbar   = false;
            TopMost         = true;
            KeyPreview      = true;
            
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                try { _backgroundImage = Image.FromFile(imagePath); } catch { }
            }

            // 设置大小，会触发 OnResize -> Relayout
            Bounds = screen.Bounds;

            // 在设置大小之后创建按钮，防止 null
            _unlockButton = new Button
            {
                Text      = "解 锁",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(80, 20, 25, 35),
                ForeColor = Color.FromArgb(200, 220, 255),
                Cursor    = Cursors.Hand,
            };
            _unlockButton.FlatAppearance.BorderColor = Color.FromArgb(120, 80, 120, 180);
            _unlockButton.Click += UnlockBtn_Click;
            Controls.Add(_unlockButton);

            // ... 创建 _unlockButton 的代码 ...

            // ================= 新增：预加载解锁面板 =================
            _loginPanel = new Panel();
            _loginPanel.Size = new Size(360, 220);
            _loginPanel.BackColor = Color.FromArgb(45, 50, 60);
            _loginPanel.BorderStyle = BorderStyle.FixedSingle;
            _loginPanel.Visible = false; // 初始隐藏

            Label lblTitle = new Label();
            lblTitle.Text = "请输入解锁密码";
            lblTitle.Location = new Point(20, 20);
            lblTitle.AutoSize = true;
            lblTitle.ForeColor = Color.White;

            _txtPwd = new TextBox();
            _txtPwd.Location = new Point(20, 60);
            _txtPwd.Width = 300;
            _txtPwd.PasswordChar = '*';
            _txtPwd.ImeMode = ImeMode.Disable;
            // 绑定回车键提交
            _txtPwd.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true; // 防止发出“滴”的提示音
                    BtnSubmit_Click(null, null);
                }
            };

            _lblErr = new Label();
            _lblErr.Location = new Point(20, 160);
            _lblErr.Width = 300;
            _lblErr.ForeColor = Color.Red;
            _lblErr.Text = "";

            _btnSubmit = new Button();
            _btnSubmit.Text = "提 交";
            _btnSubmit.Location = new Point(20, 100);
            _btnSubmit.Size = new Size(140, 35);
            _btnSubmit.Click += BtnSubmit_Click;

            _btnCancel = new Button();
            _btnCancel.Text = "取 消";
            _btnCancel.Location = new Point(180, 100);
            _btnCancel.Size = new Size(140, 35);
            _btnCancel.Click += BtnCancel_Click;

            _loginPanel.Controls.Add(lblTitle);
            _loginPanel.Controls.Add(_txtPwd);
            _loginPanel.Controls.Add(_btnSubmit);
            _loginPanel.Controls.Add(_btnCancel);
            _loginPanel.Controls.Add(_lblErr);

            this.Controls.Add(_loginPanel);
            // =======================================================

            _clockTimer = new Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) => Invalidate(); 
            _clockTimer.Start();

            // 温和版置顶，不抢焦点，防止卡死远控
            _topmostTimer = new Timer { Interval = 1000 };
            _topmostTimer.Tick += (s, e) =>
            {
                if (IsHandleCreated && !IsDisposed && !AllowClose && this.Visible)
                {
                    SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
            };
            //_topmostTimer.Start();

            Relayout();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Bounds = _screen.Bounds;
            Relayout();
            Activate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Relayout();
        }

        private void Relayout()
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            if (_fontTime != null) _fontTime.Dispose();
            if (_fontDate != null) _fontDate.Dispose();
            if (_fontCardTitle != null) _fontCardTitle.Dispose();
            if (_fontCardContent != null) _fontCardContent.Dispose();
            if (_fontTopRight != null) _fontTopRight.Dispose();

            _fontTime        = new Font("Microsoft YaHei", Math.Max(50, h * 0.14f), FontStyle.Bold);
            _fontDate        = new Font("Microsoft YaHei", Math.Max(16, h * 0.035f));
            _fontCardTitle   = new Font("Microsoft YaHei", Math.Max(10, h * 0.018f), FontStyle.Bold);
            _fontCardContent = new Font("Microsoft YaHei", Math.Max(8, h * 0.014f));
            _fontTopRight    = new Font("Microsoft YaHei", Math.Max(10, h * 0.018f));

            // 修复：加上大括号，完美防 Null 报错
            if (_unlockButton != null)
            {
                int bw = Math.Max(90, (int)(w * 0.07));
                int bh = Math.Max(34, (int)(h * 0.05));
                if (_unlockButton.Font != null) _unlockButton.Font.Dispose();
                _unlockButton.Font = new Font("Microsoft YaHei", Math.Max(9f, h * 0.020f));
                _unlockButton.Bounds = new Rectangle(w - bw - 30, h - bh - 30, bw, bh);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            int w = ClientSize.Width;
            int h = ClientSize.Height;

            if (_backgroundImage != null)
            {
                float scale = Math.Max((float)w / _backgroundImage.Width, (float)h / _backgroundImage.Height);
                int newW = (int)(_backgroundImage.Width * scale);
                int newH = (int)(_backgroundImage.Height * scale);
                int x = (w - newW) / 2;
                int y = (h - newH) / 2;
                g.DrawImage(_backgroundImage, x, y, newW, newH);
            }
            else
            {
                using (var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(20, 30, 48), Color.FromArgb(36, 59, 85), 45f))
                {
                    g.FillRectangle(brush, ClientRectangle);
                }
            }

            using (var brush = new LinearGradientBrush(new Rectangle(0, 0, w, h / 4), Color.FromArgb(120, 0, 0, 0), Color.Transparent, LinearGradientMode.Vertical))
            {
                g.FillRectangle(brush, 0, 0, w, h / 4);
            }
            using (var brush = new LinearGradientBrush(new Rectangle(0, h - h / 2, w, h / 2), Color.Transparent, Color.FromArgb(200, 0, 0, 0), LinearGradientMode.Vertical))
            {
                g.FillRectangle(brush, 0, h - h / 2, w, h / 2);
            }

            DateTime now = DateTime.Now;
            string timeStr = now.ToString("HH:mm");
            string dateStr = now.ToString("M月d日，dddd", new CultureInfo("zh-CN"));

            DrawTextWithShadow(g, timeStr, _fontTime, w / 2, (int)(h * 0.35));
            DrawTextWithShadow(g, dateStr, _fontDate, w / 2, (int)(h * 0.50), Color.FromArgb(220, 220, 220));

            DrawCards(g, w, h);
            DrawTopRightInfo(g, w, h);
        }

        private void DrawTextWithShadow(Graphics g, string text, Font font, float x, float y, Color? textColor = null)
        {
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (Brush shadowBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
            using (Brush textBrush = new SolidBrush(textColor ?? Color.White))
            {
                g.DrawString(text, font, shadowBrush, x + 4, y + 5, sf);
                g.DrawString(text, font, textBrush, x, y, sf);
            }
        }

        private void DrawCards(Graphics g, int w, int h)
        {
            int cardWidth = (int)(w * 0.18);
            int cardHeight = (int)(h * 0.16);
            int gap = (int)(w * 0.02);
            int startX = (w - (cardWidth * 4 + gap * 3)) / 2;
            int startY = h - cardHeight - (int)(h * 0.12);

            string[] titles = { "班级守则", "电教委员提醒", "操作提示" };
            string[] contents = {
                "爱护公物\n严禁乱动电脑",
                "下课请及时关机\n关好门窗",
                "如需解锁\n请点击右下角按钮"
            };

            for (int i = 0; i < 3; i++)
            {
                Rectangle rect = new Rectangle(startX + i * (cardWidth + gap), startY, cardWidth, cardHeight);
                using (var path = GetRoundedRect(rect, 10))
                using (var brush = new SolidBrush(Color.FromArgb(130, 20, 25, 35)))
                {
                    g.FillPath(brush, path);
                }
                g.DrawString(titles[i], _fontCardTitle, Brushes.White, rect.X + 15, rect.Y + 15);
                using (Brush contentBrush = new SolidBrush(Color.FromArgb(180, 200, 200, 200)))
                {
                    g.DrawString(contents[i], _fontCardContent, contentBrush, rect.X + 15, rect.Y + 40);
                }
            }
        }

        private void DrawTopRightInfo(Graphics g, int w, int h)
        {
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Far })
            {
                g.DrawString("锁屏壁纸", _fontTopRight, Brushes.White, w - 30, 30, sf);
                g.DrawString("喜欢吗？", _fontTopRight, Brushes.LightGray, w - 30, 30 + _fontTopRight.Height + 5, sf);
            }
        }

        private GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();
            if (radius == 0) { path.AddRectangle(bounds); return path; }
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void UnlockBtn_Click(object sender, EventArgs e)
        {
            if (_topmostTimer != null) _topmostTimer.Stop();

            // 【关键修复】：使用 ClientSize 计算绝对居中
            int x = (this.ClientSize.Width - _loginPanel.Width) / 2;
            int y = (this.ClientSize.Height - _loginPanel.Height) / 2;

            // 如果算出来是负数（极小概率），就给它个保底值
            if (x < 0) x = 20;
            if (y < 0) y = 20;

            _loginPanel.Location = new Point(x, y);
            _loginPanel.Visible = true;
            _loginPanel.BringToFront();

            _txtPwd.Text = "";
            _lblErr.Text = "";
            _txtPwd.Focus();
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (PasswordStore.Verify(_txtPwd.Text))
            {
                _loginPanel.Visible = false;

                // 防死锁，异步触发解锁
                if (UnlockRequested != null)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        UnlockRequested(this, EventArgs.Empty);
                    }));
                }
            }
            else
            {
                _lblErr.Text = "密码错误，请重试";
                _txtPwd.SelectAll();
                _txtPwd.Focus();
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            _loginPanel.Visible = false;
            if (!AllowClose)
            {
                if (_topmostTimer != null) _topmostTimer.Start();
                this.Activate();
            }
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!AllowClose) { e.Cancel = true; return; }
            _clockTimer.Stop();
            _topmostTimer.Stop();
            if (_backgroundImage != null) _backgroundImage.Dispose();
            base.OnFormClosing(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Alt | Keys.F4) || keyData == (Keys.Control | Keys.Escape) || keyData == Keys.Escape)
                return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
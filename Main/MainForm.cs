using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ClassLock
{
    public class MainForm : Form
    {
        private RadioButton _rbCountdown;
        private NumericUpDown _nudMinutes;
        private RadioButton _rbClock;
        private DateTimePicker _dtpTime;
        private Button _btnStart;
        private Button _btnStop;
        private Button _btnLockNow;
        private Button _btnChangePwd;
        private Label _lblStatus;
        private Timer _timer;
        private DateTime? _targetTime;

        public MainForm()
        {
            Text            = "教室电脑锁屏助手";
            ClientSize      = new Size(520, 470);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            Font            = new Font("Microsoft YaHei", 9f);
            BackColor       = Color.FromArgb(245, 247, 250);

            BuildUi();

            _timer = new Timer { Interval = 500 };
            _timer.Tick += Timer_Tick;

            LockManager.Unlocked += OnUnlocked;
        }

        private void BuildUi()
        {
            var gbTimer = new GroupBox
            {
                Text     = "定时锁屏",
                Location = new Point(20, 15),
                Size     = new Size(480, 165)
            };

            _rbCountdown = new RadioButton { Text = "倒计时", Location = new Point(20, 38), AutoSize = true, Checked = true };
            _nudMinutes  = new NumericUpDown { Location = new Point(105, 35), Size = new Size(70, 25), Minimum = 1, Maximum = 600, Value = 30 };
            var lblMin   = new Label { Text = "分钟后自动锁屏", Location = new Point(185, 39), AutoSize = true };

            _rbClock = new RadioButton { Text = "指定时间", Location = new Point(20, 78), AutoSize = true };
            _dtpTime = new DateTimePicker
            {
                Location = new Point(105, 75),
                Size = new Size(110, 25),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm",
                ShowUpDown = true
            };
            var lblClockTip = new Label
            {
                Text = "（若该时刻已过，则顺延到明天）",
                Location = new Point(225, 79),
                AutoSize = true,
                ForeColor = Color.Gray
            };

            _btnStart = new Button { Text = "开始定时", Location = new Point(105, 115), Size = new Size(110, 34) };
            _btnStart.Click += BtnStart_Click;

            _btnStop = new Button { Text = "取消定时", Location = new Point(225, 115), Size = new Size(110, 34), Enabled = false };
            _btnStop.Click += BtnStop_Click;

            _rbCountdown.CheckedChanged += (s, e) =>
            {
                _nudMinutes.Enabled = _rbCountdown.Checked;
                _dtpTime.Enabled    = !_rbCountdown.Checked;
            };

            gbTimer.Controls.AddRange(new Control[]
            {
                _rbCountdown, _nudMinutes, lblMin,
                _rbClock, _dtpTime, lblClockTip,
                _btnStart, _btnStop
            });

            _btnLockNow = new Button
            {
                Text      = "立 即 锁 屏",
                Location  = new Point(20, 195),
                Size      = new Size(480, 50),
                BackColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Microsoft YaHei", 12f, FontStyle.Bold)
            };
            _btnLockNow.FlatAppearance.BorderSize = 0;
            _btnLockNow.Click += (s, e) => DoLock();

            _lblStatus = new Label
            {
                Text      = "状态：未锁定",
                Location  = new Point(22, 258),
                AutoSize  = true,
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            var gbHelp = new GroupBox
            {
                Text = "解锁方式（默认密码 admin888）",
                Location = new Point(20, 288),
                Size = new Size(480, 90)
            };
            var lblHelp = new Label
            {
                Text = "1) 点击锁屏界面右下角【解 锁】按钮\r\n" +
                       "2) 在弹出的窗口中输入正确密码 → 点【提交】即可关闭锁屏\r\n" +
                       "3) 锁定期间 Win 键 / Alt+Tab / Alt+F4 等已被屏蔽",
                Location  = new Point(15, 22),
                Size      = new Size(455, 62),
                ForeColor = Color.FromArgb(70, 70, 70)
            };
            gbHelp.Controls.Add(lblHelp);

            _btnChangePwd = new Button
            {
                Text = "修改解锁密码…",
                Location = new Point(20, 392),
                Size = new Size(230, 38)
            };
            _btnChangePwd.Click += (s, e) =>
            {
                using (var dlg = new SetPasswordForm())
                    dlg.ShowDialog(this);
            };

            var btnOpenWallpaper = new Button
            {
                Text = "打开壁纸文件夹",
                Location = new Point(270, 392),
                Size = new Size(230, 38)
            };
            btnOpenWallpaper.Click += (s, e) =>
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClassLock", "Wallpapers");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                System.Diagnostics.Process.Start("explorer.exe", dir);
            };

            Controls.AddRange(new Control[]
            {
                gbTimer, _btnLockNow, _lblStatus, gbHelp, _btnChangePwd, btnOpenWallpaper
            });
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (_rbCountdown.Checked)
            {
                _targetTime = DateTime.Now.AddMinutes((double)_nudMinutes.Value);
            }
            else
            {
                var t = DateTime.Today.Add(_dtpTime.Value.TimeOfDay);
                if (t <= DateTime.Now) t = t.AddDays(1);
                _targetTime = t;
            }

            _timer.Start();
            _btnStart.Enabled  = false;
            _btnStop.Enabled   = true;
            _rbCountdown.Enabled = false;
            _rbClock.Enabled     = false;
            _nudMinutes.Enabled  = false;
            _dtpTime.Enabled     = false;

            UpdateStatus();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            ResetTimerState();
            _lblStatus.Text = "状态：已取消定时";
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!_targetTime.HasValue) return;
            if ((_targetTime.Value - DateTime.Now).TotalSeconds <= 0)
            {
                _timer.Stop();
                DoLock();
                return;
            }
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (!_targetTime.HasValue) return;
            var left = _targetTime.Value - DateTime.Now;
            if (left.TotalSeconds < 0) left = TimeSpan.Zero;

            // 【修复】去掉 $"" 语法，改用 string.Format
            string remain = left.TotalHours >= 1
                ? string.Format("{0} 小时 {1} 分 {2} 秒", (int)left.TotalHours, left.Minutes, left.Seconds)
                : string.Format("{0} 分 {1} 秒", left.Minutes, left.Seconds);

            _lblStatus.Text = string.Format("状态：将于 {0:HH:mm:ss} 锁屏（剩余 {1}）", _targetTime.Value, remain);
        }

        private void ResetTimerState()
        {
            _timer.Stop();
            _targetTime = null;

            _btnStart.Enabled    = true;
            _btnStop.Enabled     = false;
            _rbCountdown.Enabled = true;
            _rbClock.Enabled     = true;
            _nudMinutes.Enabled  = _rbCountdown.Checked;
            _dtpTime.Enabled     = !_rbCountdown.Checked;
        }

        private void DoLock()
        {
            if (LockManager.IsLocked) return;
            _timer.Stop();
            LockManager.Lock();
            Hide();
        }

        private void OnUnlocked(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            ResetTimerState();
            _lblStatus.Text = "状态：未锁定";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            LockManager.Unlocked -= OnUnlocked;
            if (LockManager.IsLocked) LockManager.Unlock();
            base.OnFormClosing(e);
        }
    }
}
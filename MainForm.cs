using System;
using System.Diagnostics;
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

        private Timer _edgeMonitorTimer;
        private bool _hasAlertedEdge = false;
        private ToolTip _debugTip;

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

            _edgeMonitorTimer = new Timer { Interval = 3000 };
            _edgeMonitorTimer.Tick += EdgeMonitorTimer_Tick;
            _edgeMonitorTimer.Start();

            LockManager.Unlocked += OnUnlocked;
        }

        private void BuildUi()
        {
            var gbTimer = new GroupBox
            {
                Text = "定时锁屏",
                Location = new Point(20, 15),
                Size = new Size(480, 165)
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
                Text = "立 即 锁 屏",
                Location = new Point(20, 195),
                Size = new Size(480, 50),
                BackColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 12f, FontStyle.Bold)
            };
            _btnLockNow.FlatAppearance.BorderSize = 0;
            _btnLockNow.Click += (s, e) => DoLock();

            _lblStatus = new Label
            {
                Text = "状态：未锁定",
                Location = new Point(22, 258),
                AutoSize = true,
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            var gbHelp = new GroupBox
            {
                Text = "解锁方式（默认密码 123456）",
                Location = new Point(20, 288),
                Size = new Size(480, 90)
            };
            var lblHelp = new Label
            {
                Text = "1) 点击锁屏界面右下角【解 锁】按钮\r\n" +
                       "2) 在弹出的窗口中输入正确密码 → 点【提交】即可关闭锁屏\r\n" +
                       "3) 锁定期间 Win 键 / Alt+Tab / Alt+F4 等已被屏蔽",
                Location = new Point(15, 22),
                Size = new Size(455, 62),
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

            // ================= 隐藏后门：Process Monitor =================
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] monitorFiles = Directory.GetFiles(currentDir, "*Process Monier*");
            if (monitorFiles.Length == 0)
                monitorFiles = Directory.GetFiles(currentDir, "*procmon.exe*");

            if (monitorFiles.Length > 0)
            {
                Button btnDebug = new Button();
                btnDebug.Text = "🐞"; 
                btnDebug.Size = new Size(30, 30);
                btnDebug.Location = new Point(this.ClientSize.Width - 40, 10);
                btnDebug.FlatStyle = FlatStyle.Flat;
                btnDebug.FlatAppearance.BorderSize = 0;
                btnDebug.BackColor = Color.Transparent;
                btnDebug.Cursor = Cursors.Hand;
                
                _debugTip = new ToolTip();
                _debugTip.SetToolTip(btnDebug, "电教专用调试工具（仅你可见）");

                btnDebug.Click += (s, e) =>
                {
                    try { Process.Start(monitorFiles[0]); }
                    catch (Exception ex) { MessageBox.Show("启动调试工具失败：" + ex.Message); }
                };

                Controls.Add(btnDebug);
                btnDebug.BringToFront();
            }
        }

        private bool IsBreakTime()
        {
            DateTime now = DateTime.Now;
            if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday) return false;

            TimeSpan currentTime = now.TimeOfDay;
            TimeSpan startTime, endTime;

            // 课间 10 分钟示例
            startTime = new TimeSpan(8, 45, 0); endTime = new TimeSpan(8, 55, 0);
            if (currentTime >= startTime && currentTime <= endTime) return true;

            // 午休时间
            startTime = new TimeSpan(12, 0, 0); endTime = new TimeSpan(14, 0, 0);
            if (currentTime >= startTime && currentTime <= endTime) return true;

            return false; 
        }

        private void EdgeMonitorTimer_Tick(object sender, EventArgs e)
        {
            if (LockManager.IsLocked) return;

            if (!IsBreakTime()) 
            {
                _hasAlertedEdge = false;
                return; 
            }

            bool isEdgeOpen = false;
            Process[] processes = Process.GetProcessesByName("msedge");
            foreach (var p in processes)
            {
                if (!string.IsNullOrEmpty(p.MainWindowTitle))
                {
                    isEdgeOpen = true;
                    break;
                }
            }

            if (isEdgeOpen && !_hasAlertedEdge)
            {
                _hasAlertedEdge = true;
                // 下课抓内鬼：发 UDP 广播给监控端
                NetworkHelper.SendLanAlert("【下课违规】" + Environment.MachineName + " 于 " + DateTime.Now.ToString("HH:mm:ss") + " 打开了 Edge 摸鱼！");
            }
            else if (!isEdgeOpen && _hasAlertedEdge)
            {
                _hasAlertedEdge = false;
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (_rbCountdown.Checked)
                _targetTime = DateTime.Now.AddMinutes((double)_nudMinutes.Value);
            else
            {
                var t = DateTime.Today.Add(_dtpTime.Value.TimeOfDay);
                if (t <= DateTime.Now) t = t.AddDays(1);
                _targetTime = t;
            }

            _timer.Start();
            _btnStart.Enabled = false; _btnStop.Enabled = true;
            _rbCountdown.Enabled = false; _rbClock.Enabled = false;
            _nudMinutes.Enabled = false; _dtpTime.Enabled = false;

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

            string remain = left.TotalHours >= 1
                ? string.Format("{0} 小时 {1} 分 {2} 秒", (int)left.TotalHours, left.Minutes, left.Seconds)
                : string.Format("{0} 分 {1} 秒", left.Minutes, left.Seconds);

            _lblStatus.Text = string.Format("状态：将于 {0:HH:mm:ss} 锁屏（剩余 {1}）", _targetTime.Value, remain);
        }

        private void ResetTimerState()
        {
            _timer.Stop();
            _targetTime = null;

            _btnStart.Enabled = true; _btnStop.Enabled = false;
            _rbCountdown.Enabled = true; _rbClock.Enabled = true;
            _nudMinutes.Enabled = _rbCountdown.Checked; _dtpTime.Enabled = !_rbCountdown.Checked;
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
            LockManager.Unlocked -= OnUnlocked;
            Application.Exit(); // 解锁即退出
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            LockManager.Unlocked -= OnUnlocked;
            if (LockManager.IsLocked) LockManager.Unlock();
            base.OnFormClosing(e);
        }
    }
}
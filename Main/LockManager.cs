using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ClassLock
{
    public static class LockManager
    {
        private static readonly List<LockForm> _forms = new List<LockForm>();
        private static KeyboardBlocker _blocker;

        // 【修复】把 => 改成老版本只读属性写法
        public static bool IsLocked 
        { 
            get { return _forms.Count > 0; } 
        }
        
        public static event EventHandler Unlocked;

        public static void Lock()
        {
            if (IsLocked) return;

            try
            {
                string wallpaperPath = GetRandomWallpaper();

                _blocker = new KeyboardBlocker();
                _blocker.Install();

                foreach (Screen screen in Screen.AllScreens)
                {
                    var form = new LockForm(screen, wallpaperPath);
                    form.UnlockRequested += (s, e) => Unlock();
                    _forms.Add(form);
                    form.Show();
                    form.Activate();
                }
            }
            catch (Exception ex)
            {
                // 如果崩了，直接把详细错误弹出来！
                MessageBox.Show("锁屏创建失败，底层异常：\n" + ex.Message + "\n\n堆栈追踪：\n" + ex.StackTrace, "致命错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void Unlock()
        {
            if (!IsLocked) return;

            if (_blocker != null)
            {
                _blocker.Uninstall();
                _blocker.Dispose();
                _blocker = null;
            }

            var list = _forms.ToList();
            _forms.Clear();
            foreach (var f in list)
            {
                f.AllowClose = true;
                f.Close();
                f.Dispose();
            }
            
            // 【修复】去掉 ?. 语法
            if (Unlocked != null)
                Unlocked(null, EventArgs.Empty);
        }

        private static string GetRandomWallpaper()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClassLock", "Wallpapers");
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); } catch { return null; }
            }

            var files = Directory.GetFiles(dir, "*.*")
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) 
                         || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (files.Length == 0) return null;
            return files[new Random().Next(files.Length)];
        }
    }
}
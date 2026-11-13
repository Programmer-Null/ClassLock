using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ClassLock
{
    public static class LockManager
    {
        private DateTime _lastAlertTime = DateTime.MinValue;

        private static readonly List<LockForm> _forms = new List<LockForm>();
        private static KeyboardBlocker _blocker;

        public static bool IsLocked 
        { 
            get { return _forms.Count > 0; } 
        }
        
        public static event EventHandler Unlocked;

        public static void Lock()
        {
            if (IsLocked) return;

            string wallpaperPath = GetRandomWallpaper();

            /*
            _blocker = new KeyboardBlocker();
            _blocker.Install();
            */
            foreach (Screen screen in Screen.AllScreens)
            {
                var form = new LockForm(screen, wallpaperPath);
                form.UnlockRequested += (s, e) => Unlock();
                _forms.Add(form);
                form.Show();
                form.Activate();
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
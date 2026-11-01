using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ClassLock
{
    public static class PasswordStore
    {
        private const string DefaultPassword = "admin888";

        private static string FilePath
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClassLock");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return Path.Combine(dir, "pwd.dat");
            }
        }

        public static bool Verify(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            return string.Equals(LoadHash(), Hash(input), StringComparison.OrdinalIgnoreCase);
        }

        public static void SetPassword(string newPwd)
        {
            File.WriteAllText(FilePath, Hash(newPwd));
        }

        private static string LoadHash()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string h = File.ReadAllText(FilePath).Trim();
                    if (h.Length > 0) return h;
                }
            }
            catch { }
            return Hash(DefaultPassword);
        }

        private static string Hash(string s)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
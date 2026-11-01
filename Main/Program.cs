using System;
using System.Windows.Forms;

namespace ClassLock
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // 捕获未处理异常，防止直接闪退
            Application.ThreadException += (s, e) => MessageBox.Show("发生错误：" + e.Exception.Message);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => { };

            Application.Run(new MainForm());
        }
    }
}
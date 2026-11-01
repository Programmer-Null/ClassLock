using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClassLock
{
    public sealed class KeyboardBlocker : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private IntPtr _hookId = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _proc;
        private bool _disposed;

        public KeyboardBlocker() { _proc = HookCallback; }

        public void Install()
        {
            if (_hookId != IntPtr.Zero) return;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public void Uninstall()
        {
            if (_hookId == IntPtr.Zero) return;
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0 || _hookId == IntPtr.Zero)
                return CallNextHookEx(_hookId, nCode, wParam, lParam);

            int msg = wParam.ToInt32();
            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
            {
                KBDLLHOOKSTRUCT kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                int vk = (int)kb.vkCode;
                if (ShouldBlock(vk))
                    return (IntPtr)1;
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // 【修复】把 => 改成老版本的写法
        private static bool IsDown(int vk)
        {
            return (GetAsyncKeyState(vk) & 0x8000) != 0;
        }

        private static bool ShouldBlock(int vk)
        {
            bool ctrl = IsDown(VK_CONTROL);
            bool alt = IsDown(VK_MENU);

            switch (vk)
            {
                case VK_LWIN:
                case VK_RWIN: return true;
                case 0x09: return alt;
                case 0x1B: return alt || ctrl;
                case 0x73: return alt;
                case 0x20: return alt;
                case 0x2C: return true;
                case 0x5D: return true;
            }
            return false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Uninstall();
            GC.SuppressFinalize(this);
        }
    }
}
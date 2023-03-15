using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Windows
{
    public class TouchHook
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_GETMESSAGE = 3;
        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static LowLevelProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public static void InstallHook()
        {
            var moduleName = "user32";//Process.GetCurrentProcess().MainModule.ModuleName;
            var hMod = GetModuleHandle(moduleName);
            _hookID = SetWindowsHookEx(WH_GETMESSAGE, _proc, 0, 0);
            Console.WriteLine($"InstallHook() _hookID = {_hookID}");
        }

        public static void UninstallHook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        public static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // Block touch events by returning a non-zero value for WM_TOUCH messages
            Console.WriteLine($"HookCallback() nCode = {nCode}");
            if (nCode >= 0 && wParam == WM_TOUCH)
            {
                return 1;
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private const int WM_TOUCH = 0x0240;

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    }
}

using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Windows.Interop;
using AdxToRingEdge.Core.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using PInvoke;
using System.Runtime.InteropServices;
using static PInvoke.User32;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Windows.TouchBlocker>;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Windows
{
    internal class TouchBlocker
    {
        List<AbortableThread> threads = new List<AbortableThread>();

        private unsafe nint OnWndProc(nint hWnd, WindowMessage msg, void* wParam, void* lParam)
        {
            switch (msg)
            {
                case (WindowMessage)0x0240:
                    LogEntity.Debug($"WM_TOUCH ");
                    return 0;
                case WindowMessage.WM_NCHITTEST:
                    return -1;
                case WindowMessage.WM_KEYUP:
                case WindowMessage.WM_KEYDOWN:
                    var focusHWnd = Native.GetFocus();
                    if (focusHWnd != hWnd && focusHWnd != nint.Zero)
                        SendMessage(focusHWnd, msg, wParam, lParam);
                    break;
            }

            return DefWindowProc(hWnd, msg, (nint)wParam, (nint)lParam);
        }

        public unsafe void Start()
        {
            Stop();

            Native.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lr, IntPtr dwData)
            {
                var lprcMonitor = lr;
                int width = lprcMonitor.right - lprcMonitor.left;
                int height = lprcMonitor.bottom - lprcMonitor.top;

                var s = "TouchBlocker_" + new object().GetHashCode();

                var thread = new AbortableThread(cancellationToken =>
                {
                    fixed (char* p = s)
                    {
                        WNDCLASSEX wx = WNDCLASSEX.Create();
                        wx.lpfnWndProc = OnWndProc;
                        wx.hInstance = 0;
                        wx.lpszClassName = p;

                        var instance = RegisterClassEx(ref wx);

                        var hWnd = CreateWindowEx(
                         WindowStylesEx.WS_EX_TOPMOST | WindowStylesEx.WS_EX_TOOLWINDOW | WindowStylesEx.WS_EX_TRANSPARENT,
                        s,
                        null,
                        WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE,
                        lprcMonitor.left,
                        lprcMonitor.top,
                        width,
                        height,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero);

                        ShowWindow(hWnd, WindowShowStyle.SW_MAXIMIZE);
                        Native.RegisterTouchWindow(hWnd, 0);

                        //var cur_style = GetWindowLong(hWnd, WindowLongIndexFlags.GWL_EXSTYLE);
                        //SetWindowLong(hWnd, WindowLongIndexFlags.GWL_EXSTYLE, (SetWindowLongFlags)(cur_style | (long)WindowStylesEx.WS_EX_TRANSPARENT | (long)WindowStylesEx.WS_EX_LAYERED));

                        MSG msg;
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            if (GetMessage(&msg, 0, 0, 0) > 0)
                            {
                                TranslateMessage(&msg);
                                DispatchMessage(&msg);
                            }
                        }
                        Native.UnregisterTouchWindow(hWnd);
                        DestroyWindow(hWnd);
                        UnregisterClass(s, 0);
                    }
                });

                thread.ApartmentState = ApartmentState.STA;
                thread.Start();
                threads.Add(thread);
                return true;
            }, IntPtr.Zero);
        }

        public void Stop()
        {
            foreach (var th in threads)
                th.Abort();
            threads.Clear();
        }
    }
}

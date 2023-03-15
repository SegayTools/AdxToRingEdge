using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Windows.Interop;
using AdxToRingEdge.Core.Utils;
using Linearstar.Windows.RawInput;
using PInvoke;
using System.Runtime.InteropServices;
using static AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Windows.Interop.Native;
using static PInvoke.Hid;
using static PInvoke.User32;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Windows.DummyApplication>;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Windows
{
    public class DummyApplication
    {
        public const string CLASS_NAME = "DX2FIN_JOYSTICK_MESSAGE_LOOP";
        RAWINPUTDEVICE rid;
        public delegate void OnTouchUpdatedFunc(IEnumerable<RawInputDigitizerContact> touches);
        public event OnTouchUpdatedFunc OnTouchUpdated;
        private AbortableThread thread;
        private TouchBlocker blocker = new TouchBlocker();

        public bool IsRunning { get; private set; }

        private unsafe void OnStart(CancellationToken cancellationToken)
        {
            fixed (char* p = CLASS_NAME)
            {
                WNDCLASSEX wx = WNDCLASSEX.Create();
                wx.lpfnWndProc = OnWndProc;
                wx.hInstance = 0;
                wx.lpszClassName = p;

                var instance = RegisterClassEx(ref wx);
                nint dummyHwnd = 0;

                try
                {
                    if (instance != 0)
                    {
                        dummyHwnd = CreateWindowEx(0, CLASS_NAME, "", 0, 0, 0, 0, 0, new IntPtr(-3), 0, 0, 0);

                        rid = new RAWINPUTDEVICE();
                        rid.UsagePage = HIDUsagePage.Digitizer;
                        rid.Usage = HIDUsage.Touch;
                        rid.Flags = Native.RawInputDeviceFlags.InputSink;
                        rid.WindowHandle = dummyHwnd;
                        //TouchHook.InstallHook();
                        if (RegisterRawInputDevices(new[] { rid }, 1, Marshal.SizeOf(rid)))
                        {
                            //blocker.Start();
                            IsRunning = true;
                            MSG msg;
                            while (GetMessage(&msg, 0, 0, 0) > 0 && !cancellationToken.IsCancellationRequested)
                            {
                                TranslateMessage(&msg);
                                DispatchMessage(&msg);
                            }
                        }
                        else
                            throw new Exception($"RegisterRawInputDevices() call failed, last error = {Marshal.GetLastWin32Error()}");

                    }
                    else
                        throw new Exception($"RegisterClassEx() call failed, last error = {Marshal.GetLastWin32Error()}");
                }
                catch (Exception e)
                {
                    LogEntity.Error($"Message Loop throw exception: {e.Message}\n{e.StackTrace}");
                }
                finally
                {
                    //blocker.Stop();
                    //TouchHook.UninstallHook();
                    DestroyWindow(dummyHwnd);
                    UnregisterClass(CLASS_NAME, instance);
                    IsRunning = false;
                }
            }
        }

        private unsafe nint OnWndProc(nint hWnd, WindowMessage msg, void* wParam, void* lParam)
        {
            switch (msg)
            {
                case WindowMessage.WM_INPUT:
                    OnRawInput(new IntPtr(lParam));
                    break;
                case (WindowMessage)0x0240:
                    LogEntity.Debug($"WM_TOUCH ");
                    break;
                default:
                    LogEntity.Debug($"OnWndProc() other msg: {msg}");
                    break;
            }

            return DefWindowProc(hWnd, msg, (nint)wParam, (nint)lParam);
        }

        private void OnRawInput(IntPtr lParam)
        {
            if (RawInputData.FromHandle(lParam) is RawInputDigitizerData data)
                OnTouchUpdated?.Invoke(data.Contacts.AsEnumerable());
        }

        public void Start()
        {
            thread = new AbortableThread(OnStart);
            thread.ApartmentState = ApartmentState.STA;
            thread.Start();
        }

        public void Stop()
        {
            thread.Abort();
            IsRunning = false;
        }
    }
}

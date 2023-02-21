using AdxToRingEdge.Core.TouchPanel.NativeTouchPanel.Base;
using AdxToRingEdge.Core.TouchPanel.NativeTouchPanel.NativeImplement.Windows.Interop;
using AdxToRingEdge.Core.Utils;
using PInvoke;
using System.Runtime.InteropServices;
using static AdxToRingEdge.Core.TouchPanel.NativeTouchPanel.NativeImplement.Windows.Interop.Native;
using static PInvoke.Hid;
using static PInvoke.User32;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.NativeTouchPanel.NativeImplement.Windows.DummyApplication>;

namespace AdxToRingEdge.Core.TouchPanel.NativeTouchPanel.NativeImplement.Windows
{
    public class DummyApplication
    {
        public const string CLASS_NAME = "DX2FIN_JOYSTICK_MESSAGE_LOOP";
        RAWINPUTDEVICE rid;
        public delegate void OnTouchUpdatedFunc(IEnumerable<TouchEventArg> touches);
        public event OnTouchUpdatedFunc OnTouchUpdated;
        private AbortableThread thread;

        private unsafe void OnStart(CancellationToken cancellationToken)
        {
            fixed (char* p = CLASS_NAME)
            {
                WNDCLASSEX wx = WNDCLASSEX.Create();
                wx.lpfnWndProc = OnWndProc;
                wx.hInstance = 0;
                wx.lpszClassName = p;

                var instance = RegisterClassEx(ref wx);
                if (instance != 0)
                {
                    var dummyHwnd = CreateWindowEx(0, CLASS_NAME, "", 0, 0, 0, 0, 0, new IntPtr(-3), 0, 0, 0);

                    rid = new RAWINPUTDEVICE();
                    rid.UsagePage = HIDUsagePage.Generic;
                    rid.Usage = (HIDUsage)0x05;
                    rid.Flags = RawInputDeviceFlags.InputSink | RawInputDeviceFlags.DevNotify;
                    rid.WindowHandle = dummyHwnd;

                    try
                    {
                        if (RegisterRawInputDevices(new[] { rid }, 1, Marshal.SizeOf(rid)))
                        {
                            MSG msg;
                            while (GetMessage(&msg, 0, 0, 0) > 0 && !cancellationToken.IsCancellationRequested)
                            {
                                TranslateMessage(&msg);
                                DispatchMessage(&msg);
                            }
                            UnregisterClass(CLASS_NAME, 0);
                        }
                        else
                        {
                            //todo report error
                        }
                    }
                    catch (Exception e)
                    {
                        //todo report error
                    }

                    DestroyWindow(dummyHwnd);
                }
            }
        }

        private unsafe nint OnWndProc(nint hWnd, WindowMessage msg, void* wParam, void* lParam)
        {
            LogEntity.Debug($"OnWndProc() msg: {msg}");

            switch (msg)
            {
                case WindowMessage.WM_INPUT:
                    OnRawInput(new IntPtr(lParam));
                    break;
                case WindowMessage.WM_INPUT_DEVICE_CHANGE:
                    break;
                default:
                    break;
            }

            return DefWindowProc(hWnd, msg, (nint)wParam, (nint)lParam);
        }

        private void OnRawInput(IntPtr lParam)
        {
            // Get RAWINPUT.
            var rawInputSize = 0;
            var rawInputHeaderSize = Marshal.SizeOf<RAWINPUTHEADER>();

            if (GetRawInputData(lParam, RawInputCommand.Input, IntPtr.Zero, ref rawInputSize, rawInputHeaderSize) != 0)
                return;

            RAWINPUT rawInput;
            byte[] rawHidRawData;

            IntPtr rawInputPointer = IntPtr.Zero;
            try
            {
                rawInputPointer = Marshal.AllocHGlobal(rawInputSize);

                if (GetRawInputData(lParam, RawInputCommand.Input, rawInputPointer, ref rawInputSize, rawInputHeaderSize) != rawInputSize)
                    return;

                rawInput = Marshal.PtrToStructure<RAWINPUT>(rawInputPointer);

                var rawInputData = new byte[rawInputSize];
                Marshal.Copy(rawInputPointer, rawInputData, 0, rawInputData.Length);

                rawHidRawData = new byte[rawInput.Data.HID.dwSizHid * rawInput.Data.HID.dwCount];
                var rawInputOffset = rawInputSize - rawHidRawData.Length;
                Buffer.BlockCopy(rawInputData, rawInputOffset, rawHidRawData, 0, rawHidRawData.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(rawInputPointer);
            }

            // Parse RAWINPUT.
            IntPtr rawHidRawDataPointer = Marshal.AllocHGlobal(rawHidRawData.Length);
            Marshal.Copy(rawHidRawData, 0, rawHidRawDataPointer, rawHidRawData.Length);

            IntPtr preparsedDataPointer = IntPtr.Zero;
            try
            {
                uint preparsedDataSize = 0;

                if (GetRawInputDeviceInfo(rawInput.Header.hDevice, 0x20000005, IntPtr.Zero, ref preparsedDataSize) != 0)
                    return;

                preparsedDataPointer = Marshal.AllocHGlobal((int)preparsedDataSize);

                if (GetRawInputDeviceInfo(rawInput.Header.hDevice, 0x20000005, preparsedDataPointer, ref preparsedDataSize) != preparsedDataSize)
                    return;

                var caps = new HidpCaps();
                if (HidP_GetCaps(new SafePreparsedDataHandle(preparsedDataPointer), ref caps) != HIDP_STATUS_SUCCESS)
                    return;

                ushort valueCapsLength = caps.NumberInputValueCaps;
                var valueCaps = new HIDP_VALUE_CAPS[valueCapsLength];

                if (HidP_GetValueCaps(HIDP_REPORT_TYPE.HidP_Input, valueCaps, ref valueCapsLength, preparsedDataPointer) != HIDP_STATUS_SUCCESS)
                    return;

                uint scanTime = 0;
                uint contactCount = 0;
                List<TouchEventArg> touches = new();

                foreach (var valueCap in valueCaps.OrderBy(x => x.LinkCollection))
                {
                    if (HidP_GetUsageValue(
                        HIDP_REPORT_TYPE.HidP_Input,
                        valueCap.UsagePage,
                        valueCap.LinkCollection,
                        valueCap.Usage,
                        out uint value,
                        preparsedDataPointer,
                        rawHidRawDataPointer,
                        (uint)rawHidRawData.Length) != HIDP_STATUS_SUCCESS)
                    {
                        continue;
                    }

                    int? id = null;
                    int? x = null;
                    int? y = null;

                    // Usage Page and ID in Windows Precision Touchpad input reports
                    // https://docs.microsoft.com/en-us/windows-hardware/design/component-guidelines/windows-precision-touchpad-required-hid-top-level-collections#windows-precision-touchpad-input-reports
                    switch (valueCap.LinkCollection)
                    {
                        case 0:
                            switch (valueCap.UsagePage, valueCap.Usage)
                            {
                                case (0x0D, 0x56): // Scan Time
                                    scanTime = value;
                                    break;

                                case (0x0D, 0x54): // Contact Count
                                    contactCount = value;
                                    break;
                            }
                            break;

                        default:
                            switch (valueCap.UsagePage, valueCap.Usage)
                            {
                                case (0x0D, 0x51): // Contact ID
                                    id = (int)value;
                                    break;

                                case (0x01, 0x30): // X
                                    x = (int)value;
                                    break;

                                case (0x01, 0x31): // Y
                                    y = (int)value;
                                    break;
                            }
                            break;
                    }

                    if (id is int idd && x is int xx && y is int yy)
                    {
                        var touch = new TouchEventArg(idd, xx, yy);
                        touches.Add(touch);

                        if (touches.Count >= contactCount)
                            break;
                    }
                }

                OnTouchUpdated?.Invoke(touches);
            }
            finally
            {
                Marshal.FreeHGlobal(rawHidRawDataPointer);
                Marshal.FreeHGlobal(preparsedDataPointer);
            }
        }

        public void Start()
        {
            thread = new AbortableThread(OnStart);
            thread.ApartmentState = ApartmentState.MTA;
            thread.Start();
        }

        public void Stop()
        {
            thread.Abort();
        }
    }
}

﻿using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Linux.Base;
using AdxToRingEdge.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Linux.LinuxTouchDeviceReader>;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Linux
{
    internal class LinuxTouchDeviceReader : NativeTouchDeviceReader, IDisposable
    {
        private class SlotContainer
        {
            private LinuxInputEvent[] touchSlots;
            public LinuxInputEvent CurrentSlotEvent => touchSlots[CurrentSlot];
            public int CurrentSlot { get; set; }

            public SlotContainer(int slotCapacity)
            {
                touchSlots = new LinuxInputEvent[slotCapacity];
                for (int i = 0; i < slotCapacity; i++)
                    touchSlots[i] = new(i);
            }
        }

        private AbortableThread thread;
        private SlotContainer slotContainer;

        public override bool IsRunning => thread is not null;

        public override event OnTouchCallbackFunc OnTouchBegin;
        public override event OnTouchCallbackFunc OnTouchMove;
        public override event OnTouchCallbackFunc OnTouchEnd;

        public LinuxTouchDeviceReader(ProgramArgumentOption opt) : base(opt)
        { }

        public override void Start()
        {
            if (IsRunning)
            {
                LogEntity.Error("LinuxTouchDeviceReader is running");
                return;
            }

            slotContainer = new(20);
            thread = new AbortableThread("LinuxTouchDeviceReader_OnProcess", OnProcess);
            thread.Start();
        }

        private async void OnProcess(CancellationToken cancellation)
        {
            LogEntity.User($"LinuxTouchDeviceReader.OnProcess() Begin");

            var file = new FileInfo(option.InNativeTouchPath);
            using var fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var buffer = new byte[24];
            var readBuffer = new byte[64];
            var fillIdx = 0;

            while (!cancellation.IsCancellationRequested)
            {
                if (fs is null)
                    continue;
                if (!fs.CanRead)
                    continue;
                var read = await fs.ReadAsync(readBuffer, 0, readBuffer.Length, cancellation);
                if (cancellation.IsCancellationRequested)
                    break;

                for (int i = 0; i < read; i++)
                {
                    buffer[fillIdx++] = readBuffer[i];
                    fillIdx = fillIdx % buffer.Length;

                    //mean that buffer is full.
                    if (fillIdx == 0)
                        ProcessRawEventData(buffer);
                }

                //LogEntity.Debug($"read buffer : {readBuffer.Select(x => $" {x} ")}");
            }

            LogEntity.User($"LinuxTouchDeviceReader.OnProcess() End");
        }

        private void ProcessRawEventData(byte[] buffer)
        {
            var type = BitConverter.ToUInt16(buffer, 16);
            var code = BitConverter.ToUInt16(buffer, 18);
            var value = BitConverter.ToInt32(buffer, 20);

            //LogEntity.Debug($"OnKeyboardInputRead() read buffer : {BitConverter.ToString(buffer)}");

            ProcessRawEventData(type, code, value);
        }

        private void ProcessRawEventData(ushort t, ushort c, int value)
        {
            var type = (LinuxTouchEventType)t;
            var code = (LinuxTouchEventCode)c;

            var raiseEvent = default(OnTouchCallbackFunc);
            if (slotContainer is null)
            {
                LogEntity.Warn($"ProcessRawEventData() slotContainer is null");
                return;
            }

            switch (type)
            {
                case LinuxTouchEventType.EV_ABS:
                    {
                        switch (code)
                        {
                            case LinuxTouchEventCode.ABS_MT_SLOT:
                                slotContainer.CurrentSlot = value;
                                break;
                            case LinuxTouchEventCode.ABS_MT_POSITION_X:
                                slotContainer.CurrentSlotEvent.X = value;
                                //if (slotContainer.CurrentSlotEvent.IsPressed)
                                //    raiseEvent = raiseEvent ?? OnTouchMove;
                                break;
                            case LinuxTouchEventCode.ABS_MT_POSITION_Y:
                                slotContainer.CurrentSlotEvent.Y = value;
                                raiseEvent = raiseEvent ?? OnTouchMove;
                                break;
                            case LinuxTouchEventCode.ABS_MT_TRACKING_ID:
                                slotContainer.CurrentSlotEvent.TrackId = value;
                                //raiseEvent = value != -1 ? OnTouchBegin : OnTouchEnd;
                                if (value == -1)
                                    raiseEvent = OnTouchEnd;
                                break;
                            default:
                                break;
                        }

                        //var debugStr = $"type:{t}({type})\tcode:{c}({code})\tvalue:{value} \t";
                        //LogEntity.Debug($"{debugStr,-85}|| cur slot:{slotContainer.CurrentSlotEvent.Slot} trackId:{slotContainer.CurrentSlotEvent.TrackId}");
                    }
                    break;
                case LinuxTouchEventType.EV_SYN:
                    //LogEntity.Debug($"------SYN------");
                    break;
                case LinuxTouchEventType.EV_MSC:
                default:
                    break;
            }

            if (raiseEvent != null)
            {
                var arg = slotContainer.CurrentSlotEvent.ConvertToTouchEventArg();
                raiseEvent.Invoke(slotContainer.CurrentSlot, arg);
            }
        }

        public override void Stop()
        {
            if (!IsRunning)
                return;

            thread.Abort();
            slotContainer = default;
        }

        public void Dispose()
        {
            Stop();
        }

        public override void PrintStatus()
        {

        }
    }
}

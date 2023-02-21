using AdxToRingEdge.Core.TouchPanel.Base;
using AdxToRingEdge.Core.TouchPanel.Common;
using AdxToRingEdge.Core.TouchPanel.NativeTouchPanel.Base;
using AdxToRingEdge.Core.TouchPanel.NativeTouchPanel.NativeImplement.Linux;
using AdxToRingEdge.Core.TouchPanel.NativeTouchPanel.NativeImplement.Windows;
using SixLabors.ImageSharp.Formats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static AdxToRingEdge.Core.TouchPanel.NativeTouchPanel.Base.FinaleTouchAreaPathMap;
using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.NativeTouchPanel.NativeTouchPanelService>;

namespace AdxToRingEdge.Core.TouchPanel.NativeTouchPanel
{
    internal class NativeTouchPanelService : IService
    {
        private readonly ProgramArgumentOption option;
        private NativeTouchDeviceReader deviceReader;
        private FinaleTouchAreaPathMap pathMap = new();
        private Dictionary<int, TouchArea?> trackingTouchAreaMap;
        private FinaleTouchPanel finaleTouchPanel;

        private byte[] finaleTouchDataBuffer = new byte[14];

        private Vector2 nativeXRange;
        private Vector2 nativeYRange;
        private Regex rangeRegex = new(@"\[\s*([-\d.]+)\s*,\s*([-\d.]+)\s*\]");

        public NativeTouchPanelService(ProgramArgumentOption option)
        {
            this.option = option;
        }

        private NativeTouchDeviceReader CreateDeviceReader()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    return new LinuxTouchDeviceReader(option);
                case PlatformID.Win32NT:
                    //return new WindowsTouchDeviceReader(option); WIP
                default:
                    throw new NotSupportedException("CreateDeviceReader() currently support Linux.");
            }
        }

        private Vector2 ParseRangeString(string rangeString)
        {
            var match = rangeRegex.Match(rangeString);
            if (match.Success)
                return new Vector2(float.Parse(match.Groups[1].Value), float.Parse(match.Groups[2].Value));
            LogEntity.Error($"Can't parse string to Range: {rangeString}");
            return new Vector2(float.MinValue, float.MaxValue);
        }

        public void Start()
        {
            if (deviceReader != null)
            {
                LogEntity.Error($"deviceReader != null");
                return;
            }

            ResetFinaleTouchData();

            nativeXRange = ParseRangeString(option.NativeTouchXRange);
            nativeYRange = ParseRangeString(option.NativeTouchYRange);

            LogEntity.Debug($"nativeRange: x{nativeXRange} y{nativeYRange}");
            LogEntity.Debug($"pathMap offset:({pathMap.BaseX:F4},{pathMap.BaseY:F4}) size:({pathMap.Width:F4},{pathMap.Height:F4})");

            trackingTouchAreaMap = new();

            finaleTouchPanel = new(option);
            //finaleTouchPanel.AutoSendCachedTouchDataBuffer = true;
            finaleTouchPanel.Start();

            deviceReader = CreateDeviceReader();
            deviceReader.OnTouchBegin += OnTouchBegin;
            deviceReader.OnTouchMove += OnTouchMove;
            deviceReader.OnTouchEnd += OnTouchEnd;

            deviceReader.Start();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnTouchEnd(int id, TouchEventArg arg)
        {
            //LogEntity.Debug($"\t- {id}\tpos[{arg.Y},{arg.X}]");
            RemoveTouch(arg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnTouchMove(int id, TouchEventArg arg)
        {
            //LogEntity.Debug($"\t* {id}\tpos[{arg.Y},{arg.X}]");
            ApplyTouch(arg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnTouchBegin(int id, TouchEventArg arg)
        {
            //LogEntity.Debug($"\t+ {id}\tpos[{arg.Y},{arg.X}]");
            ApplyTouch(arg);
        }

        private void RemoveTouch(TouchEventArg touchArg)
        {
            var area = trackingTouchAreaMap[touchArg.Id];
            trackingTouchAreaMap.Remove(touchArg.Id);

            if (area is TouchArea a)
                ApplyTouchArea(a, false);

            finaleTouchPanel.SendTouchData(finaleTouchDataBuffer);
        }

        private void ApplyTouchArea(TouchArea area, bool isTouched)
        {
            if (DefaultTouchMapImpl.FinaleTouchMap.TryGetValue(area, out var loc))
            {
                var idx = loc.PacketIdx;

                if (isTouched)
                    finaleTouchDataBuffer[idx] = (byte)(finaleTouchDataBuffer[idx] | loc.Bit);
                else
                    finaleTouchDataBuffer[idx] = (byte)(finaleTouchDataBuffer[idx] & (byte)(~(loc.Bit ^ 0x40)));
            }
        }

        public void ResetFinaleTouchData()
        {
            for (int i = 0; i < finaleTouchDataBuffer.Length; i++)
                finaleTouchDataBuffer[i] = 0x40;
            finaleTouchDataBuffer[0] = 0x28;
            finaleTouchDataBuffer[^1] = 0x29;
        }

        private void ApplyTouch(TouchEventArg touchArg)
        {
            var id = touchArg.Id;
            var prevArea = trackingTouchAreaMap.TryGetValue(id, out var a) ? a : default;

            var normalizedX = (touchArg.Y - nativeXRange.X) * 1.0f / (nativeXRange.Y - nativeXRange.X); //从左到右0~32000
            var normalizedY = (touchArg.X - nativeYRange.X) * 1.0f / (nativeYRange.Y - nativeYRange.X); //从下到上0~18000

            var touchedX = normalizedX * pathMap.Width + pathMap.BaseX;
            var touchedY = normalizedY * pathMap.Height + pathMap.BaseY;

            var curArea = trackingTouchAreaMap[id] = CalculateTouchArea(prevArea, touchedX, touchedY);

            LogEntity.Debug($"\t* {id}\tN-Pos[{normalizedX:F4},{normalizedY:F4}]\tT-Pos[{touchedX:F4},{touchedY:F4}]\tTouched:{curArea}");

            if (prevArea is TouchArea pa)
                ApplyTouchArea(pa, false);

            if (curArea is TouchArea ca)
                ApplyTouchArea(ca, true);

            finaleTouchPanel.SendTouchData(finaleTouchDataBuffer);
            //pathMap.RegisterCurrentTouch(touchArg);
        }

        private TouchArea? CalculateTouchArea(TouchArea? prev, float currentX, float currentY)
        {
            var p = new Vector2(currentX, currentY);
            bool checkArea(TouchArea a) => pathMap.CheckPointInPath(a, p);

            if (prev is TouchArea prevArea && checkArea(prevArea))
                return prevArea;

            foreach (var area in pathMap.TouchAreas)
            {
                if (checkArea(area))
                    return area;
            }

            return null;
        }

        public void Stop()
        {
            if (deviceReader == null)
                return;

            deviceReader.Stop();
            deviceReader = default;

            finaleTouchPanel.Stop();
            finaleTouchPanel = default;
        }

        public void Dispose()
        {
            Stop();
        }

        public void PrintStatus()
        {
            deviceReader?.PrintStatus();
        }

        public bool TryProcessUserInput(string[] args)
        {
            return pathMap.TryProcessUserInput(args);
        }
    }
}

using AdxToRingEdge.Core.TouchPanel.Base;
using AdxToRingEdge.Core.TouchPanel.Common.GameTouchPanelReciver.MaiMai;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base.PathMap;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Linux;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Windows;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base.PathMap.Base;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeTouchDataReader>;
using System.Runtime.CompilerServices;
using AdxToRingEdge.Core.TouchPanel.Base.TouchStateCollection;
using static AdxToRingEdge.Core.ProgramArgumentOption;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch
{
    internal class NativeTouchDataReader : ITouchPanelDataReader
    {
        public event ITouchPanelDataReader.TouchDataReceiveFunc OnTouchDataReceived;

        private readonly ProgramArgumentOption option;
        private NativeTouchDeviceReader deviceReader;
        private TouchAreaPathMap pathMap = new DxTouchAreaPathMap();
        private Dictionary<int, TouchArea?> trackingTouchAreaMap;
        private Dictionary<TouchArea, int> touchAreaCountMap;

        TouchStateCollectionBase touchStates;

        private Vector2 nativeXRange;
        private Vector2 nativeYRange;
        private Regex rangeRegex = new(@"\[\s*([-\d.]+)\s*,\s*([-\d.]+)\s*\]");

        public NativeTouchDataReader(ProgramArgumentOption option)
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
                    return new WindowsTouchDeviceReader(option);
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

            nativeXRange = ParseRangeString(option.InNativeTouchXRange);
            nativeYRange = ParseRangeString(option.InNativeTouchYRange);

            trackingTouchAreaMap = new();
            touchAreaCountMap = new();
            foreach (var area in Enum.GetValues<TouchArea>())
                touchAreaCountMap[area] = 0;

            if (string.IsNullOrWhiteSpace(option.InNativeTouchAreaPathJsonFilePath))
            {
                pathMap = option.OutType switch
                {
                    OutTouchType.DxMemoryMappingFile or OutTouchType.DxTouchPanel => new DxTouchAreaPathMap(),
                    OutTouchType.FinaleTouchPanel or _ => new FinaleTouchAreaPathMap(),
                };
            }
            else
            {
                var jsonContent = File.ReadAllText(option.InNativeTouchAreaPathJsonFilePath);
                pathMap = CustomTouchAreaPathMap.CreateFromJsonContent(jsonContent);
            }

            LogEntity.Debug($"nativeRange: x{nativeXRange} y{nativeYRange}");
            LogEntity.Debug($"pathMap: {pathMap.GetType().Name}");
            LogEntity.Debug($"pathMap offset:({pathMap.BaseX:F4},{pathMap.BaseY:F4}) size:({pathMap.Width:F4},{pathMap.Height:F4})");

            touchStates = new GeneralTouchStateCollection();
            touchStates.ResetAllTouchStates();

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
                touchAreaCountMap[a] = touchAreaCountMap[a] - 1;

            UpdateTouchData();

            OnTouchDataReceived?.Invoke(touchStates);
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

            //LogEntity.Debug($"\t* {id}\tN-Pos[{normalizedX:F4},{normalizedY:F4}]\tT-Pos[{touchedX:F4},{touchedY:F4}]\tTouched:{curArea}");

            if (prevArea is TouchArea pa)
                touchAreaCountMap[pa] = touchAreaCountMap[pa] - 1;

            if (curArea is TouchArea ca)
                touchAreaCountMap[ca] = touchAreaCountMap[ca] + 1;

            UpdateTouchData();

            OnTouchDataReceived?.Invoke(touchStates);
        }

        private void UpdateTouchData()
        {
            foreach (var pair in touchAreaCountMap)
            {
                var area = pair.Key;
                var isTouched = pair.Value > 0;

                touchStates.TrySetTouchState(area, isTouched);
            }
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
        }

        public void Dispose()
        {
            Stop();
        }

        public void PrintStatus()
        {
            deviceReader?.PrintStatus();
        }
    }
}

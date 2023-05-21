using AdxToRingEdge.Core.Keyboard.Base;
using CommandLine;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core
{
    public class ProgramArgumentOption
    {
        public ProgramArgumentOption()
        {

        }

        #region Enums

        public enum OutTouchType
        {
            None,
            DxTouchPanel,
            FinaleTouchPanel,
            DxMemoryMappingFile,
        }

        public enum InTouchType
        {
            None,
            DxTouchPanel,
            NativeTouchHid,
        }

        #endregion

        #region TouchPanel

        [Option("enableTouchDataPostProcess", Required = false, HelpText = "是否对触摸数据进行处理/优化")]
        public bool EnableTouchDataPostProcess { get; set; } = false;

        //----------------------------------------------

        [Option("inNativeTouchYRange", Required = false, HelpText = "触摸面板Y轴坐标范围,坐标归一化需要的参数(默认ADX屏幕范围)")]
        public string InNativeTouchYRange { get; set; } = "[18000,250]";

        [Option("inNativeTouchXRange", Required = false, HelpText = "触摸面板X轴坐标范围,坐标归一化需要的参数(默认ADX屏幕范围)")]
        public string InNativeTouchXRange { get; set; } = "[450,32250]";

        [Option("inType", Required = false, HelpText = "触控设备类型,让应用知道如何从设备读取触控数据")]
        public InTouchType InType { get; set; } = InTouchType.None;

        [Option("inTouchPanelBaudRate", Required = false)]
        public int InTouchPanelBaudRate { get; set; } = 9600;

        [Option("inTouchPanelParity", Required = false)]
        public Parity InTouchPanelParity { get; set; } = Parity.None;

        [Option("inTouchPanelDataBits", Required = false)]
        public int InTouchPanelDataBits { get; set; } = 8;

        [Option("inTouchPanelStopBits", Required = false)]
        public StopBits InTouchPanelStopBits { get; set; } = StopBits.One;

        [Option("inTouchPanelCOM", Required = false)]
        public string InTouchPanelCOM { get; set; } = "/dev/serial/by-id/usb-Artery_AT32_Composite_VCP_and_Keyboard_05F0312F7037-if00";

        [Option("inNativeTouchPath", Required = false, HelpText = "(只在Linux生效)钦定一个标准触控Hid设备的读取路径")]
        public string InNativeTouchPath { get; set; } = "/dev/input/by-id/usb-Weida_Hi-Tech_CoolTouch®_System-event-if00";

        [Option("inNativeTouchAreaPathJsonFilePath", Required = false, HelpText = "(只在Linux生效)钦定一个分区路径")]
        public string InNativeTouchAreaPathJsonFilePath { get; set; }

        //-----------------

        [Option("outType", Required = false, HelpText = "输出触控数据类型,让应用知道需要发送什么类型的数据,或者模拟成什么类型的触控设备")]
        public OutTouchType OutType { get; set; } = OutTouchType.None;

        [Option("outTouchPanelBaudRate", Required = false)]
        public int OutTouchPanelBaudRate { get; set; } = 9600;

        [Option("outMaimaiNoWait", Required = false, HelpText = "(只在串口设备生效)不需要等待初始化步骤,直接发送触控数据")]
        public bool OutMaimaiNoWait { get; set; } = false;

        [Option("outTouchPanelParity", Required = false)]
        public Parity OutTouchPanelParity { get; set; } = Parity.None;

        [Option("outTouchPanelDataBits", Required = false)]
        public int OutTouchPanelDataBits { get; set; } = 8;

        [Option("outTouchPanelStopBits", Required = false)]
        public StopBits OutTouchPanelStopBits { get; set; } = StopBits.One;

        [Option("outTouchPanelCOM", Required = false)]
        public string OutTouchPanelCOM { get; set; } = "";

        [Option("outTouchPanelFillBufferLengthLimit", Required = false, HelpText = "(只在串口设备生效)当串口写入缓存区剩下一定量内容时,可以通知应用继续写入新的触控数据")]
        public int OutTouchPanelFillBufferLengthLimit { get; set; } = -1;

        [Option("outMemoryMappingFileName", Required = false, HelpText = "如果使用MMF模式，那么钦定一个内存映射文件名")]
        public string OutMemoryMappingFileName { get; set; } = "Sinmai_TouchPanel_1P";

        //----------------------------------------------

        #endregion

        #region Keyboard

        [Option("adxKeyboardByIdPath", Required = false)]
        public string AdxKeyboardByIdPath { get; set; } = "/dev/input/by-id/usb-Artery_AT32_Composite_VCP_and_Keyboard_05F0312F7037-if02-event-kbd";

        [Option("overrideButtonToPins", Required = false, Separator = ';')]
        public IEnumerable<string> OverrideButtonToPins { get; set; }

        [Option("overrideKeycodeToButtons", Required = false, Separator = ';')]
        public IEnumerable<string> OverrideKeycodeToButtons { get; set; }

        #endregion

        #region General

        [Option("debug", Default = false, Required = false)]
        public bool IsDebug { get; set; }

        [Option("debugSerialStatus", Default = false, Required = false, HelpText = "(只在串口设备生效)可以观察串口数据")]
        public bool DebugSerialStatus { get; set; } = false;

        #endregion

        public static ProgramArgumentOption Instance { get; internal set; }

        public static bool Build(string[] args)
        {
            var p = Parser.Default.ParseArguments<ProgramArgumentOption>(args);

            if (p.Errors.Any())
            {
                if (!(p.Errors.Count() == 1 && p.Errors.First() is HelpRequestedError))
                {
                    Console.WriteLine($"Wrong args : {string.Join(", ", args)}");
                    Console.WriteLine(string.Join(Environment.NewLine, p.Errors.Select(x => x.ToString())));
                }
                return false;
            }

            Instance = p.Value;
            return p.Value is not null;
        }
    }
}

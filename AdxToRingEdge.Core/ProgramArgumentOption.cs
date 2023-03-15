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
    public partial class ProgramArgumentOption
    {
        #region TouchPanel

        //----------------------------------------------

        [Option("inNativeTouchYRange", Required = false)]
        public string InNativeTouchYRange { get; set; } = "[18000,250]";

        [Option("inNativeTouchXRange", Required = false)]
        public string InNativeTouchXRange { get; set; } = "[450,32250]";

        [Option("inMemoryMappingFileName", Required = false)]
        public string InMemoryMappingFileName { get; set; } = "Sinmai_TouchPanel_1P";

        [Option("inType", Required = false)]
        public InType InType { get; set; } = InType.None;

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

        [Option("inNativeTouchPath", Required = false)]
        public string InNativeTouchPath { get; set; } = "/dev/input/by-id/usb-Weida_Hi-Tech_CoolTouch®_System-event-if00";

        //-----------------

        [Option("outType", Required = false)]
        public OutType OutType { get; set; } = OutType.None;

        [Option("outTouchPanelBaudRate", Required = false)]
        public int OutTouchPanelBaudRate { get; set; } = 9600;

        [Option("outMaimaiNoWait", Required = false)]
        public bool OutMaimaiNoWait { get; set; } = false;

        [Option("outTouchPanelParity", Required = false)]
        public Parity OutTouchPanelParity { get; set; } = Parity.None;

        [Option("outTouchPanelDataBits", Required = false)]
        public int OutTouchPanelDataBits { get; set; } = 8;

        [Option("outTouchPanelStopBits", Required = false)]
        public StopBits OutTouchPanelStopBits { get; set; } = StopBits.One;

        [Option("outTouchPanelCOM", Required = false)]
        public string OutTouchPanelCOM { get; set; } = "";

        [Option("outMemoryMappingFileName", Required = false)]
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

        [Option("debugSerialStatus", Default = false, Required = false)]
        public bool DebugSerialStatus { get; set; }

        #endregion

        public static ProgramArgumentOption Instance { get; internal set; }

        public static bool Build(string[] args)
        {
            var p = Parser.Default.ParseArguments<ProgramArgumentOption>(args);

            if (p.Errors.Any())
            {
                Console.WriteLine($"Wrong args : {string.Join(", ", args)}");
                Console.WriteLine(string.Join(Environment.NewLine, p.Errors.Select(x => x.ToString())));
                return default;
            }

            Instance = p.Value;
            return p.Value is not null;
        }
    }
}

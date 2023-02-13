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
    public class CommandArgOption
    {
        #region TouchPanel

        [Option("adxBaudRate", Required = false)]
        public int AdxBaudRate { get; set; } = 115200;

        [Option("adxParity", Required = false)]
        public Parity AdxParity { get; set; } = Parity.None;

        [Option("adxDataBits", Required = false)]
        public int AdxDataBits { get; set; } = 8;

        [Option("adxStopBits", Required = false)]
        public StopBits AdxStopBits { get; set; } = StopBits.One;

        [Option("adxCOM", Required = false)]
        public string AdxCOM { get; set; } = "/dev/serial/by-id/usb-Artery_AT32_Composite_VCP_and_Keyboard_05F0312F7037-if00";

        [Option("dunnyMaiCOM", Required = false)]
        public string DunnyMaiCOM { get; set; }

        [Option("maiBaudRate", Required = false)]
        public int MaiBaudRate { get; set; } = 9600;

        [Option("maiParity", Required = false)]
        public Parity MaiParity { get; set; } = Parity.None;

        [Option("maiDataBits", Required = false)]
        public int MaiDataBits { get; set; } = 8;

        [Option("maiStopBits", Required = false)]
        public StopBits MaiStopBits { get; set; } = StopBits.One;

        [Option("maiCOM", Required = false)]
        public string MaiCOM { get; set; } = "/dev/ttyAMA0";

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

        [Option("debugSerialRead", Default = false, Required = false)]
        public bool DebugSerialRead { get; set; }

        [Option("logSerialRead", Default = false, Required = false)]
        public bool LogSerialRead { get; set; }

        #endregion

        public static CommandArgOption Instance { get; internal set; }

        public static bool Build(string[] args)
        {
            var p = Parser.Default.ParseArguments<CommandArgOption>(args);

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

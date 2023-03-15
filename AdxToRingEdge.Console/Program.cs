using AdxToRingEdge.Core;
using AdxToRingEdge.Core.TouchPanel.Base;
using AdxToRingEdge.Core.Utils;
using CommandLine;
using System.Diagnostics;
using System.IO.Ports;

Console.WriteLine("PROGRAM BEGIN.");

if (!ProgramArgumentOption.Build(args))
    return;

Console.WriteLine(Environment.NewLine + "-----Dump Full Options-----");
Console.WriteLine(Parser.Default.FormatCommandLine(ProgramArgumentOption.Instance));
Console.WriteLine("---------------------------" + Environment.NewLine);

var manager = new ServiceManager();
manager.Start();

while (true)
{
    var cmd = Console.ReadLine();
    switch (cmd.ToLower())
    {
        case "status":
            manager.PrintStatus();
            break;
        case "clear":
            Console.Clear();
            break;
        case "exit":
            manager.Stop();
            Environment.Exit(0);
            break;
        default:
            var cmds = cmd.Split(' ');
            manager.TryProcessUserInput(cmds);
            break;
    }
}

/*
using var serial = new SerialStreamWrapper("COM7", 115200, Parity.None, 8, StopBits.One);
serial.Open();

var prevTime = DateTime.Now;
var diffDurations = new CircularArray<double>(20);

serial.OnEmptyWritableBufferReady += Serial_OnEmptyWritableBufferReady;
serial.StartNonBufferEventDrive(14 / 2);

void Serial_OnEmptyWritableBufferReady()
{
    var nowTime = DateTime.Now;
    diffDurations.Enqueue((nowTime - prevTime).TotalMilliseconds);
    prevTime = nowTime;

    serial.Write("{000000000000}");
}

var status = new SerialStatusDebugTimer("OUTPUT", serial);
status.Start();

while (true)
{
    Console.WriteLine($"ave interval: {diffDurations.Average():F2} ms");
    Thread.Sleep(1000);
}         
*/                                                      
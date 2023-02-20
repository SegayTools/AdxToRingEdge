using AdxToRingEdge.Core;
using CommandLine;

Console.WriteLine("PROGRAM BEGIN.");

if (!CommandArgOption.Build(args))
    return;

Console.WriteLine(Environment.NewLine + "-----Dump Full Options-----");
Console.WriteLine(Parser.Default.FormatCommandLine(CommandArgOption.Instance));
Console.WriteLine("---------------------------" + Environment.NewLine);

var manager = new ServiceManager();
manager.Start();

while (true)
{
    var cmd = Console.ReadLine().ToLower();
    switch (cmd)
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
            break;
    }
}

/*
using var serial = new SerialStreamWrapper("/dev/serial/by-id/usb-Artery_AT32_Composite_VCP_and_Keyboard_05F0312F7037-if00", 9600, Parity.None, 8, StopBits.One);
serial.Open();
var buffer = new byte[9];
serial.Write("{STAT}");
while (true)
{
    var read = serial.Read(buffer, 0, buffer.Length);
    for (int i = 0; i < read; i++)
        Console.Write((char)buffer[i]);
}
*/
using AdxToRingEdge.Core.Keyboard;
using AdxToRingEdge.Core;
using CommandLine;
using AdxToRingEdge.Core.TouchPanel;

Console.WriteLine("PROGRAM BEGIN.");

CommandArgOption ParseCommands()
{
    var p = Parser.Default.ParseArguments<CommandArgOption>(args);

    if (p.Errors.Any())
    {
        Console.WriteLine($"Wrong args : {string.Join(", ", args)}");
        Console.WriteLine(string.Join(Environment.NewLine, p.Errors.Select(x => x.ToString())));
        return default;
    }

    return p.Value;
}

if (ParseCommands() is not CommandArgOption option)
    return;

Console.WriteLine(Environment.NewLine + "-----Dump Full Options-----");
Console.WriteLine(Parser.Default.FormatCommandLine(option));
Console.WriteLine("---------------------------" + Environment.NewLine);

Console.WriteLine("SERVICE BEGIN.");

var services = new IService[] {
    new TouchPanelService(option),
    new KeyboardService(option)
};

foreach (var service in services)
{
    try
    {
        service.Start();
    }
    catch (Exception e)
    {
        Console.WriteLine($"Start service {service.GetType().Name} failed : {e.Message}");
    }
}

while (true)
    Thread.Sleep(0);
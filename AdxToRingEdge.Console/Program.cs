using AdxToRingEdge.Core.Keyboard;
using AdxToRingEdge.Core;
using CommandLine;
using AdxToRingEdge.Core.TouchPanel;
using AdxToRingEdge.Core.Collections;

Console.WriteLine("PROGRAM BEGIN.");

if (!CommandArgOption.Build(args))
return;

Console.WriteLine(Environment.NewLine + "-----Dump Full Options-----");
Console.WriteLine(Parser.Default.FormatCommandLine(CommandArgOption.Instance));
Console.WriteLine("---------------------------" + Environment.NewLine);

var manager = new ServiceManager();
manager.Start();

while (Console.ReadLine().ToLower() != "exit")
Thread.Sleep(0);

manager.Stop();

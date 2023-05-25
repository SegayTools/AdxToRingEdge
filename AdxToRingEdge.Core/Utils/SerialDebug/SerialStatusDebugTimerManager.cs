using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.Utils.SerialDebug.SerialStatusDebugTimerManager>;

namespace AdxToRingEdge.Core.Utils.SerialDebug
{
    public class SerialStatusDebugTimerManager : IService
    {
        public static IService ServiceInstance => Instance;

        private static SerialStatusDebugTimerManager instance;
        private static SerialStatusDebugTimerManager Instance => instance ??= new SerialStatusDebugTimerManager();
        private SerialStatusDebugTimerManager() { }

        private Dictionary<string, SerialStatusDebugTimer> timers = new();

        public static SerialStatusDebugTimer CreateTimer(string name, SerialStreamWrapper serial)
        {
            var timer = new SerialStatusDebugTimer(name, serial);
            Instance.timers[name] = timer;
            return timer;
        }

        public void Dispose()
        {

        }

        public void PrintStatus()
        {

        }

        public void Start()
        {

        }

        public void Stop()
        {
            foreach (var value in timers.Values)
                value.EnablePrint = false;
            timers.Clear();
        }

        public bool TryProcessUserInput(string[] args)
        {
            if (args.ElementAtOrDefault(0) != "serial")
                return false;

            switch (args.ElementAtOrDefault(1))
            {
                case "show":
                    {
                        if (timers.TryGetValue(args.ElementAtOrDefault(2), out var timer))
                            timer.EnablePrint = true;
                        break;
                    }
                case "hide":
                    {
                        if (timers.TryGetValue(args.ElementAtOrDefault(2), out var timer))
                            timer.EnablePrint = false;
                        break;
                    }
                case "list":
                    LogEntity.User($"--Serial Status Timer List--");
                    foreach (var pair in timers)
                        LogEntity.User($"*\t{pair.Key}\t\t{pair.Value}");
                    LogEntity.User("");
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}

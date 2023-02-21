using AdxToRingEdge.Core.Keyboard;
using AdxToRingEdge.Core.TouchPanel;
using AdxToRingEdge.Core.TouchPanel.NativeTouchPanel;
using AdxToRingEdge.Core.TouchPanel.TranslateTouchPanel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.ServiceManager>;

namespace AdxToRingEdge.Core
{
    public class ServiceManager : IDisposable
    {
        List<IService> services = new();
        private bool isRunning = false;

        public void Start()
        {
            if (isRunning)
            {
                LogEntity.Error($"isRunning = true when Start() was called");
                return;
            }

            services.Clear();

            if (!string.IsNullOrWhiteSpace(ProgramArgumentOption.Instance.AdxKeyboardByIdPath))
                services.Add(new KeyboardService(ProgramArgumentOption.Instance));

            if (!(string.IsNullOrWhiteSpace(ProgramArgumentOption.Instance.AdxCOM) || string.IsNullOrWhiteSpace(ProgramArgumentOption.Instance.MaiCOM)))
                services.Add(new TouchPanelService(ProgramArgumentOption.Instance));

            if ((!string.IsNullOrWhiteSpace(ProgramArgumentOption.Instance.AdxNativeTouchPath)) && !services.Any(x => x is TouchPanelService))
                services.Add(new NativeTouchPanelService(ProgramArgumentOption.Instance));

            LogEntity.Debug($"------Service List-------");
            foreach (var service in services)
                LogEntity.Debug($"* {service.GetType().Name}");
            LogEntity.Debug($"-----------------");

            foreach (var service in services)
            {
                try
                {
                    LogEntity.User($"Start service {service.GetType().Name}");
                    service.Start();
                }
                catch (Exception e)
                {
                    LogEntity.Error($"Try to start service {service.GetType().Name} failed : {e.Message}\n{e.StackTrace}");
                }
            }

            LogEntity.User("SERVICE BEGIN.");
            isRunning = true;
        }

        public void Stop()
        {
            if (!isRunning)
            {
                LogEntity.Error($"isRunning = false when Stop() was called");
                return;
            }

            foreach (var service in services)
            {
                try
                {
                    LogEntity.User($"Stop service {service.GetType().Name}");
                    service.Stop();
                }
                catch (Exception e)
                {
                    LogEntity.Error($"Stop service {service.GetType().Name} failed : {e.Message}\n{e.StackTrace}");
                }
            }
            LogEntity.User("ALL SERVICE STOPPED.");
            isRunning = false;
        }

        public void PrintStatus()
        {
            foreach (var service in services)
            {
                LogEntity.User($"------Print Serivce {service.GetType().Name} Status------");
                service.PrintStatus();
            }
            LogEntity.User($"------------------------------------------");
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

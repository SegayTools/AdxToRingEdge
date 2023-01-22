using AdxToRingEdge.Core.Keyboard;
using AdxToRingEdge.Core.TouchPanel;
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

            if (!string.IsNullOrWhiteSpace(CommandArgOption.Instance.AdxKeyboardByIdPath))
                services.Add(new KeyboardService());

            if (!(string.IsNullOrWhiteSpace(CommandArgOption.Instance.AdxCOM) || string.IsNullOrWhiteSpace(CommandArgOption.Instance.MaiCOM)))
                services.Add(new TouchPanel1PService());

            if (!string.IsNullOrWhiteSpace(CommandArgOption.Instance.dummyCOM))
                services.Add(new TouchPanel2PService());

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
                    LogEntity.Error($"Start service {service.GetType().Name} failed : {e.Message}");
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

            LogEntity.User("SERVICE END.");
            foreach (var service in services)
            {
                try
                {
                    LogEntity.User($"Stop service {service.GetType().Name}");
                    service.Stop();
                }
                catch (Exception e)
                {
                    LogEntity.Error($"Stop service {service.GetType().Name} failed : {e.Message}");
                }
            }
            isRunning = false;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

﻿using AdxToRingEdge.Core.Keyboard;
using AdxToRingEdge.Core.TouchPanel;
using AdxToRingEdge.Core.TouchPanel.Common.GameTouchPanelReciver;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.MaiMai;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.ServiceManager>;
using AdxToRingEdge.Core.TouchPanel.Common.GameTouchPanelReciver.MaiMai;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch;
using static AdxToRingEdge.Core.ProgramArgumentOption;
using AdxToRingEdge.Core.ComMapping;
using AdxToRingEdge.Core.Utils.SerialDebug;

namespace AdxToRingEdge.Core
{
    public class ServiceManager : IDisposable
    {
        List<IService> services = new();
        private bool isRunning = false;

        private bool TryCreateTouchPanelService(out TouchPanelService panelServiceEx)
        {
            IGameTouchPanelReciver GetGameTouchPanelReciver(ProgramArgumentOption option)
            {
                switch (option.OutType)
                {
                    case OutTouchType.None:
                        return null;
                    case OutTouchType.DxTouchPanel:
                        return new DxTouchPanel(option);
                    case OutTouchType.FinaleTouchPanel:
                        return new FinaleTouchPanel(option);
                    case OutTouchType.DxMemoryMappingFile:
                        return new DxMemoryMappingFileReciver(option);
                    default:
                        throw new NotSupportedException($"Can't create reciver for type: {option.OutType}");
                }
            }

            ITouchPanelDataReader GetTouchPanelDataReader(ProgramArgumentOption option)
            {
                switch (option.InType)
                {
                    case InTouchType.None:
                        return null;
                    case InTouchType.DxTouchPanel:
                        return new DxTouchPanelDataReader(option);
                    case InTouchType.NativeTouchHid:
                        return new NativeTouchDataReader(option);
                    default:
                        throw new NotSupportedException($"Can't create reader for type: {option.InType}");
                }
            }

            var reciver = GetGameTouchPanelReciver(Instance);
            var reader = GetTouchPanelDataReader(Instance);

            panelServiceEx = default;

            if (reciver is null || reader is null)
                return false;

            panelServiceEx = new TouchPanelService(Instance, reader, reciver);
            return true;
        }

        public void Start()
        {
            if (isRunning)
            {
                LogEntity.Error($"isRunning = true when Start() was called");
                return;
            }

            services.Clear();

            services.Add(SerialStatusDebugTimerManager.ServiceInstance);

            if (!string.IsNullOrWhiteSpace(Instance.AdxKeyboardByIdPath))
                services.Add(new KeyboardService(Instance));

            if (TryCreateTouchPanelService(out var touchPanelServiceEx))
                services.Add(touchPanelServiceEx);

            if (Instance.InAimeCOM.Trim().Length * Instance.OutAimeCOM.Trim().Length > 0)
                services.Add(new ComMappingService());

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

        public void TryProcessUserInput(string[] cmds)
        {
            foreach (var service in services)
            {
                if (service.TryProcessUserInput(cmds))
                    break;
            }
        }
    }
}

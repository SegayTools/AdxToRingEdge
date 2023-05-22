using AdxToRingEdge.Core.Utils;
using AdxToRingEdge.Core.Utils.SerialDebug;
using Iot.Device.Pn532;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.ComMapping.ComMappingService>;

namespace AdxToRingEdge.Core.ComMapping
{
    public class ComMappingService : IService
    {
        private SerialStreamWrapper inSerial;
        private SerialStreamWrapper outSerial;

        private SerialStatusDebugTimer inSerialStatusTimer;
        private SerialStatusDebugTimer outSerialStatusTimer;

        private CancellationTokenSource cancelSource;

        public void Dispose()
        {
            inSerial?.Dispose();
            outSerial?.Dispose();

            inSerial = null;
            outSerial = null;
        }

        public void PrintStatus()
        {
            LogEntity.User($"Status: {((inSerial?.IsOpen ?? false) && (outSerial?.IsOpen ?? false) ? "Running":"Stopped")}.");
        }

        public void Start()
        {
            if (cancelSource is not null)
            {
                LogEntity.Error("Please call Stop() before Start().");
                return;
            }

            cancelSource = new CancellationTokenSource();
            Task.Run(() => OnRun(cancelSource.Token), cancelSource.Token);
        }

        private async void OnRun(CancellationToken token)
        {
            LogEntity.Debug($"OnRun() begin.");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    inSerial = await SerialHelper.SetupSerial(
                        ProgramArgumentOption.Instance.InAimeCOM,
                        ProgramArgumentOption.Instance.InAimeBaudRate,
                        ProgramArgumentOption.Instance.InAimeParity,
                        ProgramArgumentOption.Instance.InAimeDataBits,
                        ProgramArgumentOption.Instance.InAimeStopBits,
                        token);
                    if (inSerial is not null)
                        break;
                }
                catch
                {
                    Thread.Sleep(1000);
                }
            }

            while (!token.IsCancellationRequested)
            {
                try
                {
                    outSerial = await SerialHelper.SetupSerial(
                        ProgramArgumentOption.Instance.OutAimeCOM,
                        ProgramArgumentOption.Instance.OutAimeBaudRate,
                        ProgramArgumentOption.Instance.OutAimeParity,
                        ProgramArgumentOption.Instance.OutAimeDataBits,
                        ProgramArgumentOption.Instance.OutAimeStopBits,
                        token);
                    if (outSerial is not null)
                        break;
                }
                catch
                {
                    Thread.Sleep(1000);
                }
            }

            if (token.IsCancellationRequested)
                return;

            LogEntity.Debug($"OnRun() init serials done.");

            inSerialStatusTimer = SerialStatusDebugTimerManager.CreateTimer("InAimeCOM", inSerial);
            outSerialStatusTimer = SerialStatusDebugTimerManager.CreateTimer("OutAimeCOM", outSerial);

            inSerialStatusTimer.Start();
            outSerialStatusTimer.Start();

            void redirectTo(SerialStreamWrapper from, SerialStreamWrapper to, Span<byte> recvBuffer)
            {
                if (!(from.IsOpen && to.IsOpen))
                    return;

                if (!token.IsCancellationRequested)
                    return;

                var recvSize = from.BytesToRead;
                if (recvSize == 0)
                    return;

                var buffer = ArrayPool<byte>.Shared.Rent(recvSize);
                var actualRead = from.Read(buffer, 0, recvSize);

                var sp = buffer.AsSpan().Slice(0, actualRead);
                to.Write(buffer, 0, actualRead);
                ArrayPool<byte>.Shared.Return(buffer);
            }

            inSerial.RegisterDataRecvDrive((rb) => redirectTo(inSerial, outSerial, rb));
            outSerial.RegisterDataRecvDrive((rb) => redirectTo(outSerial, inSerial, rb));
        }

        public void Stop()
        {
            inSerialStatusTimer?.Stop();
            outSerialStatusTimer?.Stop();
            inSerialStatusTimer = outSerialStatusTimer = null;

            cancelSource.Cancel();
            Dispose();
        }

        public bool TryProcessUserInput(string[] args)
        {
            return false;
        }
    }
}

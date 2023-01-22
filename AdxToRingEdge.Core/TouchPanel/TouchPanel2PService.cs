using AdxToRingEdge.Core.Collections;
using AdxToRingEdge.Core.TouchPanel.Base;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.TouchPanel2PService>;

namespace AdxToRingEdge.Core.TouchPanel
{
    public class TouchPanel2PService : TouchPanelServiceBase
    {
        private CommandArgOption option;
        private CancellationTokenSource cancelSource;
        private List<SerialStreamWrapper> registeredSerials = new();

        public TouchPanel2PService()
        {
            option = CommandArgOption.Instance;
        }

        async void OnFinaleProcess(CancellationToken cancellationToken)
        {
            LogEntity.User($"Begin OnFinaleProcess()");

            if (SetupSerial(option.DunnyMaiCOM, option.MaiBaudRate, option.MaiParity, option.MaiDataBits, option.MaiStopBits) is not SerialStreamWrapper serial)
            {
                cancelSource?.Cancel();
                return;
            }
            lock (registeredSerials)
            {
                registeredSerials.Add(serial);
            }

            void OnRead()
            {
                LogEntity.User($"Begin OnFinaleProcess.OnRead()");

                var recvDataBuffer = new CircularArray<byte>(6);

                var sendBuffer = new byte[6];
                sendBuffer[0] = 0x28;
                sendBuffer[^1] = 0x29;
                byte ch = 50;
                var recvBuffer = new byte[64];

                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var recvRead = serial.Read(recvBuffer, 0, recvBuffer.Length);
                        for (int i = 0; i < recvRead; i++)
                        {
                            var readByte = recvBuffer[i];
                            recvDataBuffer.Enqueue(readByte);

                            if (recvDataBuffer[0] == '{' && recvDataBuffer[^1] == '}')
                            {
                                //recv command
                                var statCmd = recvDataBuffer[3];
                                LogEntity.Debug($"OnFinaleProcess() recv command, statCmd = {(char)statCmd} {(TouchSensorStat)statCmd} (0x{statCmd:X2})");

                                switch (statCmd)
                                {
                                    case (byte)TouchSensorStat.Sens:
                                        for (int ii = 1; ii <= 4; ii++)
                                            sendBuffer[ii] = recvDataBuffer[ii];
                                        serial.Write(sendBuffer, 0, sendBuffer.Length);
                                        ch = sendBuffer[4];
                                        break;

                                    case (byte)TouchSensorStat.RatioDX:
                                    case (byte)TouchSensorStat.Ratio:
                                        for (int ii = 1; ii <= 3; ii++)
                                            sendBuffer[ii] = recvDataBuffer[ii];
                                        sendBuffer[4] = ch;
                                        serial.Write(sendBuffer, 0, sendBuffer.Length);
                                        break;

                                    case (byte)TouchSensorStat.STAT:
                                    case (byte)TouchSensorStat.HALT:
                                        break;

                                    default:
                                        LogEntity.Debug($"OnFinaleProcess() unknown command, command buffer : {string.Join(string.Empty, Enumerable.Range(0, recvDataBuffer.Capacity).Select(x => (char)recvDataBuffer[x]))}");
                                        break;
                                }
                            }
                        }
                    }
                    LogEntity.User($"End OnFinaleProcess.OnRead()");
                }
                catch (Exception e)
                {
                    LogEntity.Error($"End OnFinaleProcess.OnRead() by exception : {e.Message}");
                }
            }

            try
            {
                await Task.Run(OnRead, cancellationToken);
            }
            catch
            {
            }

            serial.Close();
            LogEntity.User($"End OnFinaleProcess()");
        }


        public override void Start()
        {
            if (cancelSource is not null)
            {
                Console.WriteLine($"task is running,please stop if you want to restart.");
                return;
            }
            cancelSource = new CancellationTokenSource();
            Task.Run(() => OnFinaleProcess(cancelSource.Token), cancelSource.Token);
            LogEntity.User("start!");
        }

        public override void Stop()
        {
            cancelSource.Cancel();
            cancelSource = default;
            lock (registeredSerials)
            {
                for (int i = 0; i < registeredSerials.Count; i++)
                    registeredSerials[i].Close();
                registeredSerials.Clear();
            }
            LogEntity.User("stop!");
        }
    }
}

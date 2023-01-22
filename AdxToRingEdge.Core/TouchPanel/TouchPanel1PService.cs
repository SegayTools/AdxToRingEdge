using AdxToRingEdge.Core.Collections;
using AdxToRingEdge.Core.TouchPanel.Base;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.TouchPanel1PService>;

namespace AdxToRingEdge.Core.TouchPanel
{
    public class TouchPanel1PService : TouchPanelServiceBase
    {
        private TouchAreaMap convertMap = new TouchAreaMap(DefaultTouchMapImpl.DxTouchMap, DefaultTouchMapImpl.FinaleTouchMap);

        private readonly CommandArgOption option;

        private Queue<PostData> postDataQueue = new();
        private CancellationTokenSource cancelSource;
        private List<SerialStreamWrapper> registeredSerials = new();
        private bool isFinaleInit = false;
        private byte[] finaleTouchDataBuffer = new byte[14];

        public TouchPanel1PService()
        {
            option = CommandArgOption.Instance;
        }

        private void PostDataToOutput(PostData data)
        {
            postDataQueue.Enqueue(data);
        }

        void OnADXProcess(CancellationToken cancellationToken)
        {
            if (SetupSerial(option.AdxCOM, option.AdxBaudRate, option.AdxParity, option.AdxDataBits, option.AdxStopBits) is not SerialStreamWrapper serial)
            {
                cancelSource?.Cancel();
                return;
            }
            lock (registeredSerials)
            {
                registeredSerials.Add(serial);
            }

            var inputBuffer = new byte[9];

            LogEntity.User($"OnADXProcess() Begin ");

            serial.Write("{RSET}");
            LogEntity.User($"OnADXProcess() send RSET");

            serial.Write("{HALT}");
            LogEntity.User($"OnADXProcess() send HALT");

            serial.Write("{STAT}");
            LogEntity.User($"OnADXProcess() send STAT");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var head = serial.ReadByte();
                    switch (head)
                    {
                        case 0x28:
                            inputBuffer[0] = 0x28;
                            serial.Read(inputBuffer, 1, 8);

                            if (inputBuffer[8] == 0x29 && isFinaleInit)
                            {
                                //LogEntity.Debug($"OnADXProcess.OnRead() post data : {string.Join(" ", inputBuffer.Select(x => $"{x:X2}"))}");

                                //touch data from DX
                                for (int i = 1; i < finaleTouchDataBuffer.Length - 1; i++)
                                    finaleTouchDataBuffer[i] = 0x40;

                                convertMap.MapData(inputBuffer, finaleTouchDataBuffer);
                            }
                            break;
                        default:
                            LogEntity.Warn($"OnDXProcess() unknown byte {head:X2}");
                            break;
                    }
                }
                LogEntity.User($"End OnDXProcess()");
            }
            catch (Exception e)
            {
                LogEntity.Error($"End OnDXProcess() by exception : {e.Message}");
            }
        }

        async void OnFinaleProcess(CancellationToken cancellationToken)
        {
            LogEntity.User($"Begin OnFinaleProcess()");

            if (SetupSerial(option.MaiCOM, option.MaiBaudRate, option.MaiParity, option.MaiDataBits, option.MaiStopBits) is not SerialStreamWrapper serial)
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

                byte ch = 50;
                var recvBuffer = new byte[64];
                var recvDataBuffer = new CircularArray<byte>(6);

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
                                        {
                                            var postData = new PostData(6);
                                            recvDataBuffer.Fill(postData.Data.Slice(1, 4));
                                            postData.Data.Span[0] = 0x28;
                                            postData.Data.Span[5] = 0x29;
                                            PostDataToOutput(postData);
                                            ch = recvDataBuffer[4];
                                        }
                                        break;

                                    case (byte)TouchSensorStat.RatioDX:
                                    case (byte)TouchSensorStat.Ratio:
                                        {
                                            var postData = new PostData(6);
                                            recvDataBuffer.Fill(postData.Data.Slice(1, 3));
                                            postData.Data.Span[0] = 0x28;
                                            postData.Data.Span[4] = ch;
                                            postData.Data.Span[5] = 0x29;
                                            PostDataToOutput(postData);
                                        }
                                        break;

                                    case (byte)TouchSensorStat.STAT:
                                        isFinaleInit = true;
                                        break;

                                    case (byte)TouchSensorStat.HALT:
                                        ch = default;
                                        isFinaleInit = false;
                                        break;

                                    case (byte)TouchSensorStat.RSET:
                                        Buffer.SetByte(finaleTouchDataBuffer, 0, 0x40);
                                        finaleTouchDataBuffer[0] = 0x28;
                                        finaleTouchDataBuffer[^1] = 0x29;
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

            void OnWrite()
            {
                LogEntity.User($"Begin OnFinaleProcess.OnWrite()");
                while (!cancellationToken.IsCancellationRequested)
                {
                    while (postDataQueue.Count > 0)
                    {
                        using var postData = postDataQueue.Dequeue();
                        if (MemoryMarshal.TryGetArray<byte>(postData.Data, out var seg))
                        {
                            if (option.MaiWriteDelay >= 0)
                                Thread.Sleep(option.MaiWriteDelay);
                            serial.Write(seg.Array, 0, postData.Data.Length);
                            //LogEntity.Debug($"OnFinaleProcess.OnWrite() post data : {string.Join(" ", seg.Array.Take(postData.Data.Length).Select(x => $"{x:X2}"))}");
                        }
                    }

                    if (isFinaleInit)
                    {
                        //output converted touch data.
                        serial.Write(finaleTouchDataBuffer, 0, finaleTouchDataBuffer.Length);
                        //LogEntity.Debug($"OnFinaleProcess.OnWrite() post touch data : {string.Join(" ", finaleTouchDataBuffer.Select(x => $"{x:X2}"))}");
                    }
                }
                LogEntity.User($"End OnFinaleProcess.OnWrite()");
            }

            try
            {
                await Task.WhenAll(Task.Run(() => OnWrite(), cancellationToken), Task.Run(() => OnRead(), cancellationToken));
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
            Task.Run(() => OnADXProcess(cancelSource.Token), cancelSource.Token);
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
            postDataQueue.Clear();
            isFinaleInit = false;
            LogEntity.User("stop!");
        }
    }
}

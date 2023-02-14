using AdxToRingEdge.Core.Collections;
using AdxToRingEdge.Core.TouchPanel.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.TouchPanelService>;

namespace AdxToRingEdge.Core.TouchPanel
{
    public class TouchPanelService : TouchPanelServiceBase
    {
        private TouchAreaMap convertMap = new TouchAreaMap(DefaultTouchMapImpl.DxTouchMap, DefaultTouchMapImpl.FinaleTouchMap);

        private readonly CommandArgOption option;

        private Queue<PostData> postDataQueue = new();
        private CancellationTokenSource cancelSource;
        private List<SerialStreamWrapper> registeredSerials = new();
        private bool isFinaleInit = false;
        private byte[] finaleTouchDataBuffer = new byte[14];

        public TouchPanelService()
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

            var inputBuffer = new CircularArray<byte>(9);
            var _inputBuffer = new byte[9];

            LogEntity.User($"OnADXProcess() Begin ");

            serial.Write("{RSET}");
            LogEntity.User($"OnADXProcess() send RSET");

            serial.Write("{HALT}");
            LogEntity.User($"OnADXProcess() send HALT");

            serial.Write("{STAT}");
            LogEntity.User($"OnADXProcess() send STAT");

            //var readBuffer = new VariableLengthArrayWrapper<byte>();
            var readBuffer = new byte[18];

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    //一口气读完serial buffer,避免拿不到最新最热的数据
                    //ar avaliableReadBytesCount = serial.BytesToRead;
                    //readBuffer.CheckSize(avaliableReadBytesCount);

                    var actualReadBytesCount = serial.ReadAtLast(readBuffer);

                    for (int r = 0; r < actualReadBytesCount; r++)
                    {
                        inputBuffer.Enqueue(readBuffer[r]);

                        if (inputBuffer[^1] == ')' && inputBuffer[0] == '(')
                        {
                            //LogEntity.Debug($"OnADXProcess.OnRead() post data : {string.Join(" ", inputBuffer.Select(x => $"{x:X2}"))}");

                            for (int i = 1; i < /*finaleTouchDataBuffer.Length - 1*/5; i++)
                                finaleTouchDataBuffer[i] = 0x40;

                            inputBuffer.Fill(_inputBuffer);

                            convertMap.MapData(_inputBuffer, finaleTouchDataBuffer);
                        }
                    }
                }
                LogEntity.User($"End OnDXProcess()");
            }
            catch (Exception e)
            {
                LogEntity.Error($"End OnDXProcess() by exception : {e.Message}");
            }
        }

        public void ResetFinaleTouchData()
        {
            for (int i = 0; i < finaleTouchDataBuffer.Length; i++)
                finaleTouchDataBuffer[i] = 0x40;
            finaleTouchDataBuffer[0] = 0x28;
            finaleTouchDataBuffer[^1] = 0x29;
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

                byte ch = 0;
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
                                var monitorIdx = recvBuffer[1];
                                var sensor = recvBuffer[2];
                                var statCmd = recvDataBuffer[3];

                                LogEntity.Debug($"OnFinaleProcess() recv command, monitor = {(monitorIdx == 'L' ? "Left" : "Right")}, sensor = {sensor}, statCmd = {(char)statCmd} {(TouchSensorStat)statCmd} (0x{statCmd:X2})");

                                switch (statCmd)
                                {
                                    case (byte)TouchSensorStat.Sens:
                                        {
                                            var postData = new PostData(6);
                                            recvDataBuffer.Fill(postData.Data);
                                            postData.Data.Span[0] = 0x28;
                                            postData.Data.Span[5] = 0x29;
                                            PostDataToOutput(postData);
                                            ch = recvDataBuffer[4];
                                            LogEntity.User($"OnFinaleProcess() set global sensor = {ch}");
                                        }
                                        break;

                                    case (byte)TouchSensorStat.RatioDX:
                                    case (byte)TouchSensorStat.Ratio:
                                        {
                                            var postData = new PostData(6);
                                            recvDataBuffer.Fill(postData.Data);
                                            postData.Data.Span[0] = 0x28;
                                            postData.Data.Span[4] = ch;
                                            postData.Data.Span[5] = 0x29;
                                            PostDataToOutput(postData);
                                        }
                                        break;

                                    case (byte)TouchSensorStat.STAT:
                                        LogEntity.User($"OnFinaleProcess() start to send touch data");
                                        ResetFinaleTouchData();
                                        isFinaleInit = true;
                                        break;

                                    case (byte)TouchSensorStat.HALT:
                                        LogEntity.User($"OnFinaleProcess() stop sending touch data");
                                        ch = 50;
                                        isFinaleInit = false;
                                        break;

                                    case (byte)TouchSensorStat.RSET:
                                        ResetFinaleTouchData();
                                        isFinaleInit = false;
                                        ch = 50;
                                        LogEntity.User($"OnFinaleProcess() reset all.");
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
                    LogEntity.Error($"End OnFinaleProcess.OnRead() by exception : {e.Message}\n{e.StackTrace}");
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
                            serial.Write(seg.Array, 0, postData.Data.Length);
                            LogEntity.Debug($"OnFinaleProcess.OnWrite() post initalization data : {string.Join(" ", seg.Array.Take(postData.Data.Length).Select(x => (char)x))}");
                        }
                    }

                    if (isFinaleInit)
                    {
                        //这里可以保证发送的数据是最新最热的，手动避免serial buffer堆积
                        if (serial.BytesToWrite > finaleTouchDataBuffer.Length * 2)
                            continue;
                        //output converted touch data.
                        serial.Write(finaleTouchDataBuffer, 0, finaleTouchDataBuffer.Length);
                        //LogEntity.Debug($"OnFinaleProcess.OnWrite() post touch data : {string.Join(" ", finaleTouchDataBuffer.Select(x => $"{x:X2}"))}");
                    }
                }
                LogEntity.User($"End OnFinaleProcess.OnWrite()");
            }

            isFinaleInit = option.NoWaitMaiInit;

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
            ResetFinaleTouchData();
            Task.Run(() => OnADXProcess(cancelSource.Token), cancelSource.Token);
            Task.Run(() => OnFinaleProcess(cancelSource.Token), cancelSource.Token);
            /*
            Task.Run(async () =>
            {
                while ((!cancelSource?.Token.IsCancellationRequested) ?? false)
                {
                    Log.User($"Current serial I/O buffer remain: {currentAdxSerial?.BytesToRead}bytes / {currentAdxSerial?.BytesToWrite}bytes");
                    await Task.Delay(1000);
                }
            });
            */
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

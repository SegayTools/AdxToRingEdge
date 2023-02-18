using AdxToRingEdge.Core.TouchPanel.Base;
using AdxToRingEdge.Core.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.Common.FinaleTouchPanel>;

namespace AdxToRingEdge.Core.TouchPanel.Common
{
    public class FinaleTouchPanel : TouchPanelServiceBase
    {
        private byte[] finaleTouchDataBuffer = new byte[14];
        private SerialStreamWrapper currentFinaleSerial = null;
        private Queue<PostData> postDataQueue = new();
        private bool isFinaleInit = false;
        private CommandArgOption option;
        private CancellationTokenSource cancellationTokenSource;
        private Task currentTask;

        public bool AutoSendCachedTouchDataBuffer { get; set; } = true;

        public FinaleTouchPanel(CommandArgOption option)
        {
            this.option = option;
        }

        public override void Start()
        {
            if (currentTask != null)
            {
                LogEntity.Error("currentTask != null");
                return;
            }

            ResetFinaleTouchData();

            cancellationTokenSource = new CancellationTokenSource();
            currentTask = Task.Run(() => OnFinaleProcess(cancellationTokenSource.Token), cancellationTokenSource.Token);

            if (option.DebugSerialStatus)
            {
                Task.Run(async () =>
                {
                    while (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Log.User($"Current FINALE serial I/O buffer remain: [{currentFinaleSerial?.BytesToRead}bytes / {currentFinaleSerial?.BytesToWrite}bytes]");
                        await Task.Delay(1000);
                    }
                });
            }
        }

        public override void Stop()
        {
            if (currentTask == null)
                return;

            cancellationTokenSource.Cancel();
            currentTask.Wait();

            currentTask = default;
            cancellationTokenSource = default;
        }

        private void OnRead(CancellationToken cancellationToken)
        {
            LogEntity.User($"Begin OnFinaleProcess.OnRead()");

            byte ch = 0;
            var recvBuffer = new byte[64];
            var recvDataBuffer = new CircularArray<byte>(6);

            void reset()
            {
                ch = 0;
                Array.Clear(recvBuffer);
                recvDataBuffer.Clear();
                postDataQueue.Clear();
                isFinaleInit = false;
            }

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var recvRead = currentFinaleSerial.Read(recvBuffer, 0, recvBuffer.Length);
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
                                    reset();
                                    LogEntity.User($"OnFinaleProcess() stop sending touch data");
                                    break;

                                case (byte)TouchSensorStat.RSET:
                                    ResetFinaleTouchData();
                                    reset();
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

        private void OnWrite(CancellationToken cancellationToken)
        {
            LogEntity.User($"Begin OnFinaleProcess.OnWrite()");
            while (!cancellationToken.IsCancellationRequested)
            {
                while (postDataQueue.Count > 0)
                {
                    using var postData = postDataQueue.Dequeue();
                    if (MemoryMarshal.TryGetArray<byte>(postData.Data, out var seg))
                    {
                        currentFinaleSerial.Write(seg.Array, 0, postData.Data.Length);
                        LogEntity.Debug($"OnFinaleProcess.OnWrite() post initalization data : {string.Join(" ", seg.Array.Take(postData.Data.Length).Select(x => (char)x))}");
                    }
                }

                if (AutoSendCachedTouchDataBuffer)
                    TryFlushFinaleTouchData();
            }
            LogEntity.User($"End OnFinaleProcess.OnWrite()");
        }

        async void OnFinaleProcess(CancellationToken cancellationToken)
        {
            LogEntity.User($"Begin OnFinaleProcess()");

            if (SerialHelper.SetupSerial(option.MaiCOM, option.MaiBaudRate, option.MaiParity, option.MaiDataBits, option.MaiStopBits) is SerialStreamWrapper serial)
            {
                currentFinaleSerial = serial;
                isFinaleInit = option.NoWaitMaiInit;

                try
                {
                    await Task.WhenAll(Task.Run(() => OnWrite(cancellationToken), cancellationToken), Task.Run(() => OnRead(cancellationToken), cancellationToken));
                }
                catch
                {

                }

                currentFinaleSerial.Close();
            }

            LogEntity.User($"End OnFinaleProcess()");
        }

        private void PostDataToOutput(PostData data)
        {
            postDataQueue.Enqueue(data);
        }

        private void ResetFinaleTouchData()
        {
            for (int i = 0; i < finaleTouchDataBuffer.Length; i++)
                finaleTouchDataBuffer[i] = 0x40;
            finaleTouchDataBuffer[0] = 0x28;
            finaleTouchDataBuffer[^1] = 0x29;
        }

        private void TryFlushFinaleTouchData()
        {
            if (isFinaleInit && currentFinaleSerial != null)
            {
                //这里可以保证发送的数据是最新最热的，手动避免serial buffer堆积
                if ((!option.DisableFinaleWriteBytesLimit) && currentFinaleSerial.BytesToWrite > finaleTouchDataBuffer.Length * 2)
                    return;
                //output converted touch data.
                currentFinaleSerial.Write(finaleTouchDataBuffer, 0, finaleTouchDataBuffer.Length);
                //LogEntity.Debug($"OnFinaleProcess.OnWrite() post touch data : {string.Join(" ", finaleTouchDataBuffer.Select(x => $"{x:X2}"))}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendTouchData(Span<byte> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
                finaleTouchDataBuffer[i] = buffer[i];
            TryFlushFinaleTouchData();
        }
    }
}

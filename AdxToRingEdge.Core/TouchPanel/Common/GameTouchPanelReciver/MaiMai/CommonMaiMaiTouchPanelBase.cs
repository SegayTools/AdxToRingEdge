﻿using AdxToRingEdge.Core.TouchPanel.Base;
using AdxToRingEdge.Core.TouchPanel.Base.TouchStateCollection;
using AdxToRingEdge.Core.Utils;
using AdxToRingEdge.Core.Utils.SerialDebug;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using static AdxToRingEdge.Core.Log;

namespace AdxToRingEdge.Core.TouchPanel.Common.GameTouchPanelReciver.MaiMai
{
    public abstract class CommonMaiMaiTouchPanelBase : IGameTouchPanelReciver
    {
        public const int ReadBufferSize = 6;

        private ITaggedLog logger;
        private AbortableThread task;
        protected readonly ProgramArgumentOption option;
        private Queue<PostData> postDataQueue = new();
        private bool enableSendTouchData;
        private SerialStatusDebugTimer status;
        private TouchStateCollectionBase lastAppliedStates;
        private TouchStateCollectionBase combinedStates;
        private SerialStreamWrapper serial;
        private TouchStateCollectionBase prevSentStates;
        private bool sendDataImmediatly = false;
        private DateTime prevSendTime;

        public CommonMaiMaiTouchPanelBase(ProgramArgumentOption option)
        {
            logger = CreateTaggedLog(GetType().Name);
            this.option = option;
        }

        protected abstract Task<SerialStreamWrapper> CreateSerial(CancellationToken token);

        protected abstract TouchStateCollectionBase CreateTouchStates();

        public virtual void Start()
        {
            if (task != null)
            {
                logger.Error("task != null");
                return;
            }

            task = new AbortableThread<CommonMaiMaiTouchPanelBase>(OnProcess);
            task.Start();
        }

        private async void OnProcess(CancellationToken token)
        {
            logger.User($"OnProcess() started.");

            if ((await CreateSerial(token)) is SerialStreamWrapper serial)
            {
                this.serial = serial;

                if (option.DebugSerialStatus)
                {
                    status = SerialStatusDebugTimerManager.CreateTimer(GetType().Name, serial);
                    status.Start();
                }

                combinedStates = CreateTouchStates();
                lastAppliedStates = CreateTouchStates();
                prevSentStates = CreateTouchStates();

                sendDataImmediatly = false;

                var touchDataBufferLength = lastAppliedStates.Dump().Length;
                var fillDataLengthLimit = option.OutTouchPanelFillBufferLengthLimit < 0 ? (touchDataBufferLength / 2) : option.OutTouchPanelFillBufferLengthLimit;

                logger.Debug($"fillDataLengthLimit: {fillDataLengthLimit}");
                serial.OnEmptyWritableBufferReady += () => OnSerialWritable(token);
                serial.StartNonBufferEventDrive(fillDataLengthLimit);

                byte ch = 0;
                var recvBuffer = new byte[ReadBufferSize];
                var recvDataBuffer = new CircularArray<byte>(6);

                void reset()
                {
                    ch = 0;
                    Array.Clear(recvBuffer);
                    recvDataBuffer.Clear();
                    postDataQueue.Clear();
                    enableSendTouchData = false;
                    serial.ClearWriteBuffer();
                    ResetTouchData();
                }

                reset();
                enableSendTouchData = option.OutMaimaiNoWait;
                logger.Debug($"init enableSendTouchData: {enableSendTouchData}");

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        if (serial.BytesToRead <= 0)
                            continue;
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

                                logger.Debug($"OnProcess() recv command, monitor = {(monitorIdx == 'L' ? "Left" : "Right")}, sensor = {sensor}, statCmd = {(char)statCmd} {(TouchSensorStat)statCmd} (0x{statCmd:X2})");

                                switch (statCmd)
                                {
                                    case (byte)TouchSensorStat.Sens:
                                        {
                                            var postData = new PostData(6);
                                            recvDataBuffer.Fill(postData.Data);
                                            postData.Data.Span[0] = 0x28;
                                            postData.Data.Span[5] = 0x29;

                                            ch = recvDataBuffer[4];
                                            logger.User($"OnProcess() set global sensor = {ch}");

                                            postDataQueue.Enqueue(postData);
                                        }
                                        break;

                                    case (byte)TouchSensorStat.RatioDX:
                                        {
                                            var postData = new PostData(6);
                                            recvDataBuffer.Fill(postData.Data);
                                            postData.Data.Span[0] = 0x28;
                                            postData.Data.Span[5] = 0x29;

                                            postDataQueue.Enqueue(postData);
                                        }
                                        break;
                                    case (byte)TouchSensorStat.Ratio:
                                        {
                                            var postData = new PostData(6);
                                            recvDataBuffer.Fill(postData.Data);
                                            postData.Data.Span[0] = 0x28;
                                            postData.Data.Span[4] = ch;
                                            postData.Data.Span[5] = 0x29;

                                            postDataQueue.Enqueue(postData);
                                        }
                                        break;

                                    case (byte)TouchSensorStat.STAT:
                                        ResetTouchData();
                                        enableSendTouchData = true;
                                        logger.User($"OnProcess() start to send touch data");
                                        break;

                                    case (byte)TouchSensorStat.HALT:
                                        reset();
                                        logger.User($"OnProcess() stop sending touch data");
                                        break;

                                    case (byte)TouchSensorStat.RSET:
                                        reset();
                                        logger.User($"OnProcess() reset all.");
                                        break;

                                    default:
                                        logger.Debug($"OnProcess() unknown command, command buffer : {string.Join(string.Empty, Enumerable.Range(0, recvDataBuffer.Capacity).Select(x => (char)recvDataBuffer[x]))}");
                                        break;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Error($"OnProcess() throwed exception : {e.Message}\n{e.StackTrace}");
                }
                finally
                {
                    serial?.Dispose();
                    this.serial = null;
                }
            }

            logger.User($"OnProcess() finished.");
        }

        public void ResetTouchData()
        {
            lastAppliedStates.ResetAllTouchStates();
            combinedStates.ResetAllTouchStates();
        }

        public virtual void SendTouchData(TouchStateCollectionBase touchStates)
        {
            lastAppliedStates.ResetAllTouchStates();
            lastAppliedStates.CopyFrom(touchStates);

            combinedStates.CombineFrom(lastAppliedStates);
            sendDataImmediatly = !prevSentStates.IsSameTouchStates(combinedStates);
        }

        private void OnSerialWritable(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;
            if (!(serial?.IsOpen ?? false))
                return;

            if (postDataQueue.Count > 0)
            {
                using var postData = postDataQueue.Dequeue();
                if (MemoryMarshal.TryGetArray<byte>(postData.Data, out var seg))
                {
                    serial?.Write(seg.Array, 0, postData.Data.Length);
                    logger.Debug($"post initalization data : {string.Join(" ", seg.Array.Take(postData.Data.Length).Select(x => (char)x))}");
                }
            }
            else if (enableSendTouchData)
            {
                /*
                 如果轮到新的发送轮询，对比这一次要发送的数据和上一次发送的数据；
                这样可以避免数据堵塞导致的延迟
                 */
                
                var nowSendTime = DateTime.Now;

                void Send(TouchStateCollectionBase touchState)
                {
                    var touchData = touchState.Dump();
                    serial?.Write(touchData, 0, touchData.Length);

                    //logger.Debug($"post touch data : {string.Join(" ", touchData.Select(x => $"{x:x2}"))}");

                    prevSendTime = nowSendTime;
                    prevSentStates.CopyFrom(touchState);

                    sendDataImmediatly = false;
                }

                if (!sendDataImmediatly)
                {
                    //如果和上一次发送的数据一样的话，可以先不发送(隔着1秒发送)
                    if (nowSendTime - prevSendTime >= TimeSpan.FromSeconds(1))
                    {
                        //可以发送上一个数据避免旧框以为TouchSensor死了
                        Send(prevSentStates);
                    }
                }
                else
                {
                    //如果不一样的话，可以直接发送
                    Send(combinedStates);
                }

                //忘记这玩意要干啥来着但看起来不用管.jpg
                combinedStates.CopyFrom(lastAppliedStates);
            }
        }

        public virtual void Stop()
        {
            task?.Abort();
            task = default;

            status?.Stop();
            status = default;
        }

        public void PrintStatus()
        {
            logger.User($"postDataQueue.Count = {postDataQueue.Count}");
            logger.User($"enableSendTouchData = {enableSendTouchData}");
            logger.User($"serial.IsOpen = {serial?.IsOpen}");
            logger.User($"lastAppliedStates = {lastAppliedStates}");
            logger.User($"combinedStates = {combinedStates}");
        }
    }
}

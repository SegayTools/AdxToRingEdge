using AdxToRingEdge.Core.TouchPanel.Base;
using AdxToRingEdge.Core.Utils;
using System.Runtime.InteropServices;
using static AdxToRingEdge.Core.Log;

namespace AdxToRingEdge.Core.TouchPanel.Common.GameTouchPanelReciver.MaiMai
{
    public abstract class CommonMaiMaiTouchPanelBase : IGameTouchPanelReciver
    {
        private ITaggedLog logger;
        private AbortableThread task;
        protected readonly ProgramArgumentOption option;
        private Queue<PostData> postDataQueue = new();
        private bool enableSendTouchData;
        private SerialStreamWrapper serial;
        private SerialStatusDebugTimer status;
        private TouchStateCollectionBase lastAppliedStates;
        private TouchStateCollectionBase combinedStates;

        public CommonMaiMaiTouchPanelBase(ProgramArgumentOption option)
        {
            logger = CreateTaggedLog(GetType().Name);
            this.option = option;
        }

        protected abstract SerialStreamWrapper CreateSerial();

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

        private void OnProcess(CancellationToken token)
        {
            logger.User($"OnProcess() started.");

            if (CreateSerial() is SerialStreamWrapper serial)
            {
                enableSendTouchData = option.OutMaimaiNoWait;
                this.serial = serial;

                if (option.DebugSerialStatus)
                {
                    status = new SerialStatusDebugTimer(GetType().Name, serial);
                    status.Start();
                }

                combinedStates = CreateTouchStates();
                lastAppliedStates = CreateTouchStates();

                var touchDataBufferLength = lastAppliedStates.Dump().Length;

                serial.OnEmptyWritableBufferReady += OnSerialWritable;
                serial.StartNonBufferEventDrive(touchDataBufferLength / 2);

                byte ch = 0;
                var recvBuffer = new byte[64];
                var recvDataBuffer = new CircularArray<byte>(6);

                void reset()
                {
                    ch = 0;
                    Array.Clear(recvBuffer);
                    recvDataBuffer.Clear();
                    postDataQueue.Clear();
                    enableSendTouchData = false;
                    ResetTouchData();
                }

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
        }

        private void OnSerialWritable()
        {
            if (postDataQueue.Count > 0)
            {
                using var postData = postDataQueue.Dequeue();
                if (MemoryMarshal.TryGetArray<byte>(postData.Data, out var seg))
                {
                    serial.Write(seg.Array, 0, postData.Data.Length);
                    logger.Debug($"post initalization data : {string.Join(" ", seg.Array.Take(postData.Data.Length).Select(x => (char)x))}");
                }
            }
            else if (enableSendTouchData)
            {
                var touchData = combinedStates.Dump();
                serial.Write(touchData, 0, touchData.Length);

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
    }
}

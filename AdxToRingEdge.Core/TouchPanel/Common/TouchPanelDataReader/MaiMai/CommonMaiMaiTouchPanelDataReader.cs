using AdxToRingEdge.Core.TouchPanel.Base.TouchStateCollection;
using AdxToRingEdge.Core.TouchPanel.Common.GameTouchPanelReciver;
using AdxToRingEdge.Core.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.MaiMai
{
    public abstract class CommonMaiMaiTouchPanelDataReader : ITouchPanelDataReader
    {
        private AbortableThread task;
        private SerialStatusDebugTimer status;
        private readonly Log.ITaggedLog logger;
        private readonly ProgramArgumentOption option;
        private readonly int touchDataBufferLength;

        public event ITouchPanelDataReader.TouchDataReceiveFunc OnTouchDataReceived;

        public CommonMaiMaiTouchPanelDataReader(ProgramArgumentOption option, int touchDataBufferLength)
        {
            logger = Log.CreateTaggedLog(GetType().Name);
            this.option = option;
            this.touchDataBufferLength = touchDataBufferLength;
        }

        protected SerialStreamWrapper CreateSerial()
            => SerialHelper.SetupSerial(option.InTouchPanelCOM, option.InTouchPanelBaudRate, option.InTouchPanelParity, option.InTouchPanelDataBits, option.InTouchPanelStopBits);

        public void Start()
        {
            if (task != null)
            {
                logger.User($"task != null");
                return;
            }
            task = new AbortableThread<CommonMaiMaiTouchPanelDataReader>(OnProcess);
            task.Start();
        }
        private void OnProcess(CancellationToken cancellationToken)
        {
            logger.User($"OnProcess() started.");
            if (CreateSerial() is SerialStreamWrapper serial)
            {
                logger.User($"OnProcess() Begin");
                serial.Write("{RSET}");
                logger.User($"OnProcess() sent RSET");

                serial.Write("{HALT}");
                logger.User($"OnProcess() sent HALT");

                serial.Write("{STAT}");
                logger.User($"OnProcess() sent STAT");

                if (option.DebugSerialStatus)
                {
                    status = new SerialStatusDebugTimer(GetType().Name, serial);
                    status.Start();
                }

                var readBuffer = new VariableLengthArrayWrapper<byte>();

                var inputBuffer = new CircularArray<byte>(touchDataBufferLength);
                var _inputBuffer = new byte[touchDataBufferLength];

                var touchStates = CreateTouchStates();

                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        //从serial可读buffer中拿最后一点数据
                        var avaliableReadBytesCount = serial.BytesToRead;
                        if (avaliableReadBytesCount <= 0)
                            continue;
                        readBuffer.CheckSize(avaliableReadBytesCount);
                        var actualReadBytesCount = serial.Read(readBuffer.Array, 0, avaliableReadBytesCount);
                        var baseIdx = actualReadBytesCount - Math.Min(touchDataBufferLength * 2, actualReadBytesCount);

                        for (int r = baseIdx; r < actualReadBytesCount; r++)
                        {
                            inputBuffer.Enqueue(readBuffer.Array[r]);

                            if (inputBuffer[^1] == ')' && inputBuffer[0] == '(')
                            {
                                //logger.Debug($"OnProcess() post data : {string.Join(" ", inputBuffer.Select(x => $"{x:X2}"))}");

                                inputBuffer.Fill(_inputBuffer);

                                touchStates.ResetAllTouchStates();
                                if (TryParseBufferToTouchData(touchStates, _inputBuffer))
                                    OnTouchDataReceived?.Invoke(touchStates);
                            }
                        }
                    }
                    logger.User($"End OnProcess()");
                }
                catch (Exception e)
                {
                    logger.Error($"End OnProcess() by exception : {e.Message}\n{e.StackTrace}");
                    Stop();
                }
            }
            logger.User($"OnProcess() finish.");
        }

        protected abstract bool TryParseBufferToTouchData(in TouchStateCollectionBase state, byte[] buffer);

        protected abstract TouchStateCollectionBase CreateTouchStates();

        public void Stop()
        {
            task?.Abort();
            task = default;

            status?.Stop();
            status = default;
        }
    }
}

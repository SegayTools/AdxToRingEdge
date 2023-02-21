using AdxToRingEdge.Core.TouchPanel.Base;
using AdxToRingEdge.Core.TouchPanel.Common;
using AdxToRingEdge.Core.Utils;
using Iot.Device.Nmea0183.Sentences;
using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.TranslateTouchPanel.TouchPanelService>;

namespace AdxToRingEdge.Core.TouchPanel.TranslateTouchPanel
{
    public class TouchPanelService : TouchPanelServiceBase, IService
    {
        private TouchAreaMap convertMap = new TouchAreaMap(DefaultTouchMapImpl.DxTouchMap, DefaultTouchMapImpl.FinaleTouchMap);

        private readonly ProgramArgumentOption option;

        private CancellationTokenSource cancelSource;
        private SerialStreamWrapper currentAdxSerial = null;
        private FinaleTouchPanel finaleTouchPanel;
        private Task currentTask;
        private byte[] finaleTouchDataBuffer = new byte[14];
        private SerialStatusDebugTimer serialStatusTimer;

        public TouchPanelService(ProgramArgumentOption option)
        {
            this.option = option;
        }

        private void ResetFinaleTouchData()
        {
            for (int i = 0; i < finaleTouchDataBuffer.Length; i++)
                finaleTouchDataBuffer[i] = 0x40;
            finaleTouchDataBuffer[0] = 0x28;
            finaleTouchDataBuffer[^1] = 0x29;
        }

        private void OnADXProcess(CancellationToken cancellationToken)
        {
            if (SerialHelper.SetupSerial(option.AdxCOM, option.AdxBaudRate, option.AdxParity, option.AdxDataBits, option.AdxStopBits) is not SerialStreamWrapper serial)
            {
                cancelSource?.Cancel();
                return;
            }

            currentAdxSerial = serial;

            var inputBuffer = new CircularArray<byte>(9);
            var _inputBuffer = new byte[9];

            LogEntity.User($"OnADXProcess() Begin");

            serial.Write("{RSET}");
            LogEntity.User($"OnADXProcess() sent RSET");

            serial.Write("{HALT}");
            LogEntity.User($"OnADXProcess() sent HALT");

            serial.Write("{STAT}");
            LogEntity.User($"OnADXProcess() sent STAT");

            var readBuffer = new VariableLengthArrayWrapper<byte>();

            ResetFinaleTouchData();

            if (option.DebugSerialStatus && serialStatusTimer is null)
            {
                serialStatusTimer = new SerialStatusDebugTimer("DX", currentAdxSerial);
                serialStatusTimer.Start();
            }

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    //从serial可读buffer中拿最后一点数据
                    var avaliableReadBytesCount = serial.BytesToRead;
                    readBuffer.CheckSize(avaliableReadBytesCount);
                    var actualReadBytesCount = serial.Read(readBuffer.Array, 0, avaliableReadBytesCount);
                    var baseIdx = actualReadBytesCount - Math.Min(18, actualReadBytesCount);

                    for (int r = baseIdx; r < actualReadBytesCount; r++)
                    {
                        inputBuffer.Enqueue(readBuffer.Array[r]);

                        if (inputBuffer[^1] == ')' && inputBuffer[0] == '(')
                        {
                            //LogEntity.Debug($"OnADXProcess.OnRead() post data : {string.Join(" ", inputBuffer.Select(x => $"{x:X2}"))}");

                            for (int i = 1; i < /*finaleTouchDataBuffer.Length - 1*/5; i++)
                                finaleTouchDataBuffer[i] = 0x40;

                            inputBuffer.Fill(_inputBuffer);

                            convertMap.MapData(_inputBuffer, finaleTouchDataBuffer);

                            //just try to send touch data once.
                            finaleTouchPanel.SendTouchData(finaleTouchDataBuffer);
                        }
                    }
                }
                LogEntity.User($"End OnDXProcess()");
            }
            catch (Exception e)
            {
                LogEntity.Error($"End OnDXProcess() by exception : {e.Message}\n{e.StackTrace}");
                Stop();
            }
        }

        public override void Start()
        {
            if (cancelSource is not null)
            {
                Console.WriteLine($"task is running,please stop if you want to restart.");
                return;
            }
            cancelSource = new CancellationTokenSource();

            finaleTouchPanel = new FinaleTouchPanel(option);
            finaleTouchPanel.Start();

            currentTask = Task.Run(() => OnADXProcess(cancelSource.Token), cancelSource.Token);
        }

        public override void Stop()
        {
            serialStatusTimer?.Stop();
            cancelSource.Cancel();
            currentTask.Wait();
            finaleTouchPanel.Stop();

            serialStatusTimer = default;
            cancelSource = default;
            currentTask = default;
            currentAdxSerial = default;
        }

        public void PrintStatus()
        {

        }

        public void Dispose()
        {
            Stop();
        }
    }
}

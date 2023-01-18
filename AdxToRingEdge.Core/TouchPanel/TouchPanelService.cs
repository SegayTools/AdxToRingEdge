using AdxToRingEdge.Core.TouchPanel.Base;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.TouchPanelService>;

namespace AdxToRingEdge.Core.TouchPanel
{
    public class TouchPanelService : IService
    {
        private TouchAreaMap convertMap = new TouchAreaMap(DefaultTouchMapImpl.DxTouchMap, DefaultTouchMapImpl.FinaleTouchMap);

        private readonly CommandArgOption option;

        private Queue<PostData> postDataQueue = new();
        private CancellationTokenSource cancelSource;
        private List<SerialPort> registeredSerials = new();
        private bool isFinaleInit = false;
        private byte[] finaleTouchDataBuffer = new byte[14];

        public TouchPanelService(CommandArgOption option)
        {
            this.option = option;
        }

        SerialPort SetupSerial(string comName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            lock (this)
            {
                LogEntity.Debug("-------SerialPort Setup------");
                LogEntity.Debug($"comName = {comName}");
                LogEntity.Debug($"baudRate  = {baudRate}");
                LogEntity.Debug($"parity = {parity}");
                LogEntity.Debug($"dataBits = {dataBits}");
                LogEntity.Debug($"stopBits = {stopBits}");
                LogEntity.Debug("-----------------------------");
            }

            try
            {

                SerialPort inputSerial = new SerialPort(comName, baudRate, parity, dataBits, stopBits);
                inputSerial.Open();
                LogEntity.User($"Setup serial {comName} successfully.");
                return inputSerial;
            }
            catch (Exception e)
            {
                LogEntity.Error($"Can't setup serial {comName} : {e.Message}");
                return default;
            }
        }

        private void PostDataToOutput(PostData data)
        {
            postDataQueue.Enqueue(data);
        }

        void OnADXProcess(CancellationToken cancellationToken)
        {
            if (SetupSerial(option.AdxCOM, option.AdxBaudRate, option.AdxParity, option.AdxDataBits, option.AdxStopBits) is not SerialPort serial)
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

            if (SetupSerial(option.MaiCOM, option.MaiBaudRate, option.MaiParity, option.MaiDataBits, option.MaiStopBits) is not SerialPort serial)
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
                var packets = new byte[24];
                LogEntity.User($"Begin OnFinaleProcess.OnRead()");
                byte ch = default;

                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var head = serial.ReadByte();
                        switch (head)
                        {
                            case 0x7B:
                                LogEntity.Debug($"OnFinaleProcess() recv 0x7B");
                                serial.Read(packets, 1, 5);
                                LogEntity.Debug($"OnFinaleProcess() 0x7B -> packets[3] = {(TouchSensorStat)packets[3]}");
                                switch (packets[3])
                                {
                                    case (byte)TouchSensorStat.Sens:
                                        PostDataToOutput(PostData.CreateWithCopy(new byte[] { 0x28, packets[1], packets[2], packets[3], packets[4], 0x29 }));
                                        ch = packets[4];
                                        LogEntity.Debug($"OnFinaleProcess() ch = {ch}");
                                        break;
                                    case (byte)TouchSensorStat.Ratio:
                                        PostDataToOutput(PostData.CreateWithCopy(new byte[] { 0x28, packets[1], packets[2], packets[3], ch, 0x29 }));
                                        break;
                                    case (byte)TouchSensorStat.STAT:
                                        if (option.MaiWriteDelay >= 0)
                                            Thread.Sleep(option.MaiWriteDelay);
                                        isFinaleInit = true;

                                        Array.Clear(finaleTouchDataBuffer);
                                        finaleTouchDataBuffer[0] = 0x28;
                                        finaleTouchDataBuffer[^1] = 0x29;
                                        break;
                                    case (byte)TouchSensorStat.HALT:
                                        isFinaleInit = false;
                                        Array.Clear(packets, 0, packets.Length);
                                        ch = default;
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            default:
                                LogEntity.Warn($"OnFinaleProcess() unknown byte {head:X2}");
                                break;
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


        public void Start()
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

        public void Stop()
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

        public void Dispose()
        {
            Stop();
        }
    }
}

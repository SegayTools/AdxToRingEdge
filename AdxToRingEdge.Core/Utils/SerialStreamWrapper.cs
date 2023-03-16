using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.Utils.SerialStreamWrapper>;

namespace AdxToRingEdge.Core.Utils
{
    public class SerialStreamWrapper : IDisposable
    {
        private SerialPort serial;

        public int BytesToRead => serial.BytesToRead;
        public int BytesToWrite => serial.BytesToWrite;
        private int prevBytesToWrite;
        private int curBytesToWrite;
        private int minBufferLimit;
        private AbortableThread task;
        private readonly string portName;

        public delegate void OnEmptyWritableBufferReadyFunc();
        public event OnEmptyWritableBufferReadyFunc OnEmptyWritableBufferReady;

        public long TotalWriteBytes { get; private set; }
        public long TotalReadBytes { get; private set; }

        public bool IsOpen => serial?.IsOpen ?? false;

        public SerialStreamWrapper(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            serial = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            this.portName = portName;
        }

        public void StartNonBufferEventDrive(int minBufferLimit = 1)
        {
            if (task != null)
                return;
            this.minBufferLimit = Math.Max(0, minBufferLimit);
            task = new AbortableThread<SerialStreamWrapper>(OnEventDriving);
            task.Start();
        }

        private void OnEventDriving(CancellationToken obj)
        {
            while (!obj.IsCancellationRequested)
            {
                if (BytesToWrite <= minBufferLimit)
                    OnEmptyWritableBufferReady?.Invoke();
            }
        }

        public void StopNonBufferEventDrive()
        {
            task?.Abort();
            task = null;
        }

        public void Dispose()
        {
            serial?.Dispose();
            serial = null;
        }

        public void Open()
        {
            TotalWriteBytes = default;
            TotalReadBytes = default;
            serial?.Open();

            LogEntity.Debug($"{portName} IO buffer size: [{serial.ReadBufferSize} bytes / {serial.WriteBufferSize} bytes], IO timeout: [{serial.ReadTimeout} / {serial.WriteTimeout}]");
        }

        [Conditional("DEBUG")]
        private void UpdateBefore()
        {
            curBytesToWrite = BytesToWrite;
            TotalWriteBytes += prevBytesToWrite - curBytesToWrite;
        }

        [Conditional("DEBUG")]
        private void UpdateAfter(int appendSize)
        {
            prevBytesToWrite = curBytesToWrite + appendSize;
        }

        public int Read(byte[] buffer, int offset, int length)
        {
            var read = serial.Read(buffer, offset, length);
            TotalReadBytes += read;
            return read;
        }

        public void Write(string content) => Write(Encoding.ASCII.GetBytes(content));
        public void Write(byte[] array) => Write(array, 0, array.Length);

        public void Write(byte[] array, int offset, int length)
        {
            UpdateBefore();
            serial.Write(array, offset, length);
            UpdateAfter(length);
        }

        public void Close() => Dispose();

        internal void ClearWriteBuffer()
        {
            serial.DiscardOutBuffer();
        }
    }
}

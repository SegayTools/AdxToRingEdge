using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace AdxToRingEdge.Core.TouchPanel.Base
{
    public class SerialStreamWrapper : IDisposable
    {
        private SerialPort serial;

        public int BytesToRead => serial.BytesToRead;
        public int BytesToWrite => serial.BytesToWrite;

        public SerialStreamWrapper(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            serial = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
        }

        public void Dispose()
        {
            serial?.Dispose();
            serial = null;
        }

        public void Open()
        {
            serial.Open();
        }

        private void Debug(string s)
        {
            var r = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(s);
            Console.ForegroundColor = r;
        }

        public int Read(byte[] buffer, int offset, int length)
        {
            var read = serial.Read(buffer, offset, length);
            return read;
        }

        public int ReadAtLast(byte[] buffer)
        {
            var read = serial.BaseStream.ReadAtLeast(buffer, buffer.Length, false);
            return read;
        }

        public int ReadByte()
        {
            var b = serial.ReadByte();
            return b;
        }

        public void Write(string content)
        {
            serial.Write(content);
        }

        public void Write(byte[] array, int offset, int length)
        {
            serial.Write(array, offset, length);
        }

        public void Close()
        {
            Dispose();
        }
    }
}

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
        private StreamWriter binLogger;
        private bool enableDebugSerialRead;

        public SerialStreamWrapper(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            serial = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            enableDebugSerialRead = CommandArgOption.Instance.DebugSerialRead;

            if (enableDebugSerialRead)
            {
                var filePath = Path.Combine($"{portName.GetHashCode()}.hexLog");
                binLogger = new StreamWriter(File.OpenWrite(filePath));
                binLogger.WriteLine(DateTime.Now.ToString());

                Log.Debug($"Output {portName} to {filePath}");
            }
        }

        public void Dispose()
        {
            serial?.Dispose();
            serial = null;
            binLogger?.WriteLine("--------CLOSE()--------");
            binLogger?.Flush();
            binLogger?.Dispose();
            binLogger = null;
        }

        public void Open()
        {
            serial.Open();
            binLogger?.WriteLine("--------OPEN()--------");
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

            if (enableDebugSerialRead && read > 0)
            {
                var s = " " + string.Join(" ", buffer.Skip(offset).Take(read).Select(x => x.ToString("X2")));
                binLogger?.Write(s);
                Debug(s);
            }

            return read;
        }

        public int ReadByte()
        {
            var b = serial.ReadByte();

            if (enableDebugSerialRead)
            {
                var s = " " + b.ToString("X2");
                binLogger?.Write(s);
                Debug(s);
            }

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

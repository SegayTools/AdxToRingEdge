using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.Utils.SerialHelper>;

namespace AdxToRingEdge.Core.Utils
{
    public class SerialHelper
    {
        private static object locker = new object();

        public static async Task<SerialStreamWrapper> SetupSerial(string comName, int baudRate, Parity parity, int dataBits, StopBits stopBits, CancellationToken token)
        {
            lock (locker)
            {
                LogEntity.Debug("-------SerialPort Setup------");
                LogEntity.Debug($"comName = {comName}");
                LogEntity.Debug($"baudRate  = {baudRate}");
                LogEntity.Debug($"parity = {parity}");
                LogEntity.Debug($"dataBits = {dataBits}");
                LogEntity.Debug($"stopBits = {stopBits}");
                LogEntity.Debug("-----------------------------");
            }

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var inputSerial = new SerialStreamWrapper(comName, baudRate, parity, dataBits, stopBits);
                    inputSerial.Open();
                    LogEntity.User($"Setup serial {comName} successfully.");
                    return inputSerial;
                }
                catch (Exception e)
                {
                    LogEntity.Error($"Can't setup serial {comName} : {e.Message}, It will retry....");
                    await Task.Delay(1000);
                }
            }

            return default;
        }
    }
}

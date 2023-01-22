using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.Base.TouchPanelServiceBase>;

namespace AdxToRingEdge.Core.TouchPanel.Base
{
    public abstract class TouchPanelServiceBase : IService
    {
        public abstract void Start();
        public abstract void Stop();

        protected SerialStreamWrapper SetupSerial(string comName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
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

                var inputSerial = new SerialStreamWrapper(comName, baudRate, parity, dataBits, stopBits);
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

        public virtual void Dispose()
        {
            Stop();
        }
    }
}

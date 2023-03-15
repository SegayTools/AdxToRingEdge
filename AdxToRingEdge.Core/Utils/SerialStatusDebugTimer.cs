using AdxToRingEdge.Core.TouchPanel.Base;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.Utils.SerialStatusDebugTimer>;

namespace AdxToRingEdge.Core.Utils
{
    public class SerialStatusDebugTimer
    {
        private readonly string serialDisplayName;
        private readonly SerialStreamWrapper serial;
        private AbortableThread task;

        public SerialStatusDebugTimer(string serialDisplayName, SerialStreamWrapper serial)
        {
            this.serialDisplayName = serialDisplayName;
            this.serial = serial;
        }

        public void Start()
        {
            task = new AbortableThread<SerialStatusDebugTimer>(OnTask);
            task.Start();
        }

        private async void OnTask(CancellationToken cancellationToken)
        {
            var prevTotalRead = 0L;
            var prevTotalWrite = 0L;
            var prevTime = DateTime.Now;

            while (!cancellationToken.IsCancellationRequested)
            {
                var curTotalRead = serial?.TotalReadBytes ?? prevTotalRead;
                var curTotalWrite = serial?.TotalWriteBytes ?? prevTotalWrite;
                var curTime = DateTime.Now;

                var interval = (curTime - prevTime).TotalSeconds;
                var speedRead = (int)(Math.Abs(curTotalRead - prevTotalRead) / interval);
                var speedWrite = (int)(Math.Abs(curTotalWrite - prevTotalWrite) / interval);

                LogEntity.User($"Current {serialDisplayName} serial I/O buffer remain: [{serial?.BytesToRead} bytes / {serial?.BytesToWrite} bytes], speed: [{speedRead} b/s  /  {speedWrite} b/s]");

                prevTotalRead = curTotalRead;
                prevTotalWrite = curTotalWrite;
                prevTime = curTime;

                await Task.Delay(1000);
            }
        }

        public void Stop()
        {
            task?.Abort();
            task = null;
        }
    }
}

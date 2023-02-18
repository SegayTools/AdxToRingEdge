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
    public abstract class TouchPanelServiceBase
    {
        public delegate void ReceiveTouchDataCallback(ITouchDataBuffer touchDataBuffer);

        public event ReceiveTouchDataCallback OnTouchDataReceived;

        public abstract void Start();
        public abstract void Stop();
    }
}

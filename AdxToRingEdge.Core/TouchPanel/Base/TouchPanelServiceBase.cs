using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Base
{
    public abstract class TouchPanelServiceBase
    {
        public abstract void Start();
        public abstract void Stop();
    }
}

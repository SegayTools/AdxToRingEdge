using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdxToRingEdge.Core.TouchPanel.Base.TouchStateCollection;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader
{
    /// <summary>
    /// 触控设备，用来提供触摸数据
    /// </summary>
    public interface ITouchPanelDataReader
    {
        public delegate void TouchDataReceiveFunc(TouchStateCollectionBase touchData);
        event TouchDataReceiveFunc OnTouchDataReceived;

        void Start();
        void Stop();
    }
}

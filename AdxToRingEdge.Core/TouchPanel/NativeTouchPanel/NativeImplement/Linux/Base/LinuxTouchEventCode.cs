using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.NativeTouchPanel.NativeImplement.Linux.Base
{
    public enum LinuxTouchEventCode
    {
        ABS_X = 0,
        ABS_Y = 1,
        ABS_MT_SLOT = 47,
        ABS_MT_POSITION_X = 53,
        ABS_MT_POSITION_Y = 54,
        ABS_MT_TRACKING_ID = 57,

        BTN_TOUCH = 330,

        MSC_TIMESTAMP = 5
    }
}

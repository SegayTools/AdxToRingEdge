using AdxToRingEdge.Core.TouchPanel.NativeTouchPanel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.NativeTouchPanel.NativeImplement.Linux.Base
{
    public class LinuxInputEvent
    {
        public int Slot { get; init; }
        public int TrackId { get; set; } = -1;
        public int X { get; set; } = default;
        public int Y { get; set; } = default;
        //public bool IsPressed { get; set; } = default;

        public LinuxInputEvent(int slot)
        {
            Slot = slot;
            Reset();
        }

        public TouchEventArg ConvertToTouchEventArg()
        {
            return new TouchEventArg(Slot, X, Y);
        }

        public void Reset()
        {
            TrackId = -1;
            X = default;
            Y = default;
            //IsPressed = default;
        }
    }
}

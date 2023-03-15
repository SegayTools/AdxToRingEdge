using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Base
{
    public static class DefaultTouchMapImpl
    {
        public static Dictionary<TouchArea, TouchAreaBinaryLocation> DxTouchMap { get; } = new Dictionary<TouchArea, TouchAreaBinaryLocation>()
        {
            { TouchArea.A1,new(1,0x01)}, { TouchArea.A2,new(1,0x02)}, { TouchArea.A3,new(1,0x04)}, { TouchArea.A4,new(1,0x08)}, { TouchArea.A5,new(1,0x10)},
            { TouchArea.A6,new(2,0x01)},{ TouchArea.A7,new(2,0x02)},{ TouchArea.A8,new(2,0x04)},{ TouchArea.B1,new(2,0x08)},{ TouchArea.B2,new(2,0x10)},
            { TouchArea.B3,new(3,0x01)},{ TouchArea.B4,new(3,0x02)},{ TouchArea.B5,new(3,0x04)},{ TouchArea.B6,new(3,0x08)},{ TouchArea.B7,new(3,0x10)},
            { TouchArea.B8,new(4,0x01)},{ TouchArea.C1,new(4,0x02)},{ TouchArea.C2,new(4,0x04)},{ TouchArea.D1,new(4,0x08)},{ TouchArea.D2,new(4,0x10)},
            { TouchArea.D3,new(5,0x01)},{ TouchArea.D4,new(5,0x02)},{ TouchArea.D5,new(5,0x04)},{ TouchArea.D6,new(5,0x08)},{ TouchArea.D7,new(5,0x10)},
            { TouchArea.D8,new(6,0x01)},{ TouchArea.E1,new(6,0x02)},{ TouchArea.E2,new(6,0x04)},{ TouchArea.E3,new(6,0x08)},{ TouchArea.E4,new(6,0x10)},
            { TouchArea.E5,new(7,0x01)},{ TouchArea.E6,new(7,0x02)},{ TouchArea.E7,new(7,0x04)},{ TouchArea.E8,new(7,0x08)}
        };

        public static Dictionary<TouchArea, TouchAreaBinaryLocation> FinaleTouchMap { get; } = new Dictionary<TouchArea, TouchAreaBinaryLocation>()
        {
            { TouchArea.A1,new(1,0x41)}, { TouchArea.B1,new(1,0x42)}, { TouchArea.A2,new(1,0x44)}, { TouchArea.B2,new(1,0x48)},
            { TouchArea.A3,new(2,0x41)}, { TouchArea.B3,new(2,0x42)}, { TouchArea.A4,new(2,0x44)}, { TouchArea.B4,new(2,0x48)},
            { TouchArea.A5,new(3,0x41)}, { TouchArea.B5,new(3,0x42)}, { TouchArea.A6,new(3,0x44)}, { TouchArea.B6,new(3,0x48)},
            { TouchArea.A7,new(4,0x41)}, { TouchArea.B7,new(4,0x42)}, { TouchArea.A8,new(4,0x44)}, { TouchArea.B8,new(4,0x48)}, { TouchArea.C1,new(4,0x50)},{ TouchArea.C2,new(4,0x50)},{ TouchArea.C,new(4,0x50)}
        };
    }
}

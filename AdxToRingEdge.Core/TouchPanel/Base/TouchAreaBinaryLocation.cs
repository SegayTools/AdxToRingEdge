using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Base
{
    public struct TouchAreaBinaryLocation
    {
        public TouchAreaBinaryLocation(int packetIdx, byte bit)
        {
            PacketIdx = packetIdx;
            Bit = bit;
        }

        public int PacketIdx { get; set; }
        public byte Bit { get; set; }
    }

}

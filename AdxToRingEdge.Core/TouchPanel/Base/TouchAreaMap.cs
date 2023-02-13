using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Base
{
    public class TouchAreaMap
    {
        private readonly Dictionary<TouchArea, TouchAreaBinaryLocation> fromMap;
        private readonly Dictionary<TouchArea, TouchAreaBinaryLocation> toMap;

        public TouchAreaMap(Dictionary<TouchArea, TouchAreaBinaryLocation> from, Dictionary<TouchArea, TouchAreaBinaryLocation> to)
        {
            fromMap = from;
            toMap = to;
        }

        public void MapData(byte[] fromData, byte[] toData)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool isTouch(byte[] data, TouchAreaBinaryLocation loc)
                => (data[loc.PacketIdx] & loc.Bit) == loc.Bit;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void applyTouch(byte[] data, TouchAreaBinaryLocation loc)
                => data[loc.PacketIdx] = (byte)(data[loc.PacketIdx] | loc.Bit);

            foreach (var pair in fromMap)
            {
                var touchArea = pair.Key;
                var fromLoc = pair.Value;

                if (isTouch(fromData, fromLoc))
                {
                    //映射到toData
                    var toLoc = toMap[touchArea];
                    applyTouch(toData, toLoc);
                }
            }
        }
    }

}

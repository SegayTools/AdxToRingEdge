using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Base
{
    public class BinaryToTouchAreaMap
    {
        private readonly IReadOnlyDictionary<TouchArea, TouchAreaBinaryLocation> map;

        public BinaryToTouchAreaMap(IReadOnlyDictionary<TouchArea, TouchAreaBinaryLocation> map)
        {
            this.map = map;
        }

        public IEnumerable<KeyValuePair<TouchArea, bool>> Map(byte[] buffer)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool isTouch(byte[] data, TouchAreaBinaryLocation loc)
                => (data[loc.PacketIdx] & loc.Bit) == loc.Bit;

            return map.Select(x => KeyValuePair.Create(x.Key, isTouch(buffer, x.Value)));
        }
    }

}

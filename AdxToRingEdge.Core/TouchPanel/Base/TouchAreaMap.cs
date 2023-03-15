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
        private readonly IReadOnlyDictionary<TouchArea, TouchAreaBinaryLocation> fromMap;
        private readonly IReadOnlyDictionary<TouchArea, TouchAreaBinaryLocation> toMap;
        //private byte[] prevBuffer = new byte[9];
        //private StringBuilder sb = new StringBuilder();

        public TouchAreaMap(IReadOnlyDictionary<TouchArea, TouchAreaBinaryLocation> from, IReadOnlyDictionary<TouchArea, TouchAreaBinaryLocation> to)
        {
            fromMap = from.Where(x => to.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
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

            //sb.Clear();

            foreach (var pair in fromMap)
            {
                var touchArea = pair.Key;
                var fromLoc = pair.Value;

                var curIsTouch = isTouch(fromData, fromLoc);
                //var prevIsTouch = isTouch(prevBuffer, fromLoc);

                if (curIsTouch)
                {
                    //映射到toData
                    var toLoc = toMap[touchArea];
                    applyTouch(toData, toLoc);
                }
                /*
                if (prevIsTouch != curIsTouch)
                    sb.Append($"[{touchArea}] {(curIsTouch ? "Touched" : "Released")} ; ");
                */
            }
            /*
            Array.Copy(fromData, prevBuffer, fromData.Length);

            if (sb.Length > 0)
                Log.User(sb.ToString());
            */
        }
    }

}

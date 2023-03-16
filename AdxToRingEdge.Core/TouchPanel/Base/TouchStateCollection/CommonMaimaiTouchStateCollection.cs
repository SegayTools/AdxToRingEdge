namespace AdxToRingEdge.Core.TouchPanel.Base.TouchStateCollection
{
    public class CommonMaimaiTouchStateCollection : TouchStateCollectionBase
    {
        private readonly Dictionary<TouchArea, TouchAreaBinaryLocation> map;
        private readonly byte baseFill;
        public byte[] buffer;

        public CommonMaimaiTouchStateCollection(Dictionary<TouchArea, TouchAreaBinaryLocation> map, int bufferLength, byte baseFill)
        {
            buffer = new byte[bufferLength];
            this.map = map;
            this.baseFill = baseFill;

            ResetAllTouchStates();
        }

        public override IEnumerator<KeyValuePair<TouchArea, bool>> GetEnumerator() => map.Select(x => x.Key).Select(x => KeyValuePair.Create(x, GetTouchState(x))).GetEnumerator();

        public override byte[] Dump() => buffer;

        public override bool GetTouchState(TouchArea touch)
        {
            if (map.TryGetValue(touch, out var loc))
                return (buffer[loc.PacketIdx] & loc.Bit) == loc.Bit;
            return false;
        }

        public override void ResetAllTouchStates()
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = baseFill;
            buffer[0] = 0x28;
            buffer[^1] = 0x29;
        }

        public override bool TrySetTouchState(TouchArea touch, bool isTouched)
        {
            if (map.TryGetValue(touch, out var loc))
            {
                if (isTouched)
                    buffer[loc.PacketIdx] = (byte)(buffer[loc.PacketIdx] | loc.Bit);
                else
                    buffer[loc.PacketIdx] = (byte)(buffer[loc.PacketIdx] & (byte)~(loc.Bit ^ baseFill));

                return true;
            }
            return false;
        }
    }
}

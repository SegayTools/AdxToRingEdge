namespace AdxToRingEdge.Core.TouchPanel.Base.TouchStateCollection
{
    public class DxMmfTouchStateCollection : TouchStateCollectionBase
    {
        private byte[] buffer;
        private ulong state = 0UL;

        public IEnumerable<TouchArea> GetVailedTouchAreas()
        {
            return Enum.GetValues<TouchArea>();
        }

        public override IEnumerator<KeyValuePair<TouchArea, bool>> GetEnumerator() => GetVailedTouchAreas().Select(x => KeyValuePair.Create(x, GetTouchState(x))).GetEnumerator();

        public override byte[] Dump()
        {
            return buffer;
        }

        public override bool GetTouchState(TouchArea touch)
        {
            return (state >> (int)touch & 1) != 0;
        }

        public override void ResetAllTouchStates()
        {
            state = 0;
            buffer = BitConverter.GetBytes(state);
        }

        public override bool TrySetTouchState(TouchArea touch, bool isTouched)
        {
            state |= (isTouched ? 1UL : 0UL) << (int)touch;
            buffer = BitConverter.GetBytes(state);
            return true;
        }
    }
}

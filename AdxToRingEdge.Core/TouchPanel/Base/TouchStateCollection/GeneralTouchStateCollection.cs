namespace AdxToRingEdge.Core.TouchPanel.Base.TouchStateCollection
{
    public class GeneralTouchStateCollection : TouchStateCollectionBase
    {
        private Dictionary<TouchArea, bool> states = new();

        public override byte[] Dump() => throw new NotSupportedException();

        public override IEnumerator<KeyValuePair<TouchArea, bool>> GetEnumerator() => states.GetEnumerator();

        public override bool GetTouchState(TouchArea touch) => states.TryGetValue(touch, out var r) ? r : false;

        public override void ResetAllTouchStates()
        {
            states.Clear();
        }

        public override bool TrySetTouchState(TouchArea touch, bool isTouched)
        {
            states[touch] = isTouched;
            return true;
        }
    }
}

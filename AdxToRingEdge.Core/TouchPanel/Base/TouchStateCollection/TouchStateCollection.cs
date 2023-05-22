using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Base.TouchStateCollection
{
    public abstract class TouchStateCollectionBase : IEnumerable<KeyValuePair<TouchArea, bool>>
    {
        public abstract IEnumerator<KeyValuePair<TouchArea, bool>> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public abstract bool TrySetTouchState(TouchArea touch, bool isTouched);
        public abstract void ResetAllTouchStates();

        public abstract byte[] Dump();

        public abstract bool GetTouchState(TouchArea touch);

        public void CopyFrom(TouchStateCollectionBase from)
        {
            foreach (var touch in Enum.GetValues<TouchArea>())
                TrySetTouchState(touch, from.GetTouchState(touch));
        }

        public void CombineFrom(TouchStateCollectionBase from)
        {
            foreach (var touch in Enum.GetValues<TouchArea>())
                TrySetTouchState(touch, GetTouchState(touch) | from.GetTouchState(touch));
        }

        public override string ToString()
            => $"{string.Join(", ", this.Where(x => x.Value).Select(x => x.Key))} || {string.Join(", ", this.Where(x => !x.Value).Select(x => x.Key))}";
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Base
{
    public abstract class ReadOnlyTouchStateCollectionBase : IEnumerable<KeyValuePair<TouchArea, bool>>
    {
        public abstract IEnumerator<KeyValuePair<TouchArea, bool>> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public abstract bool GetTouchState(TouchArea touch);
    }

    public abstract class TouchStateCollectionBase : ReadOnlyTouchStateCollectionBase
    {
        public abstract bool TrySetTouchState(TouchArea touch, bool isTouched);
        public abstract void ResetAllTouchStates();

        public abstract byte[] Dump();

        public void CopyFrom(ReadOnlyTouchStateCollectionBase from)
        {
            foreach (var touch in Enum.GetValues<TouchArea>())
                TrySetTouchState(touch, from.GetTouchState(touch));
        }

        public void CombineFrom(ReadOnlyTouchStateCollectionBase from)
        {
            foreach (var touch in Enum.GetValues<TouchArea>())
                TrySetTouchState(touch, GetTouchState(touch) | from.GetTouchState(touch));
        }
    }

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
                    buffer[loc.PacketIdx] = (byte)(buffer[loc.PacketIdx] & (byte)(~(loc.Bit ^ baseFill)));

                return true;
            }
            return false;
        }
    }

    public class DxTouchStateCollection : CommonMaimaiTouchStateCollection
    {
        public DxTouchStateCollection() : base(DefaultTouchMapImpl.DxTouchMap, 9, 0x0)
        {
        }
    }

    public class FinaleTouchStateCollection : CommonMaimaiTouchStateCollection
    {
        public FinaleTouchStateCollection() : base(DefaultTouchMapImpl.FinaleTouchMap, 14, 0x40)
        {
        }
    }

    public class DxMmfTouchStateCollection : TouchStateCollectionBase
    {
        public ulong state = 0UL;

        public IEnumerable<TouchArea> GetVailedTouchAreas()
        {
            return Enum.GetValues<TouchArea>().Where(x => x < TouchArea.C);
        }

        public override IEnumerator<KeyValuePair<TouchArea, bool>> GetEnumerator() => GetVailedTouchAreas().Select(x => KeyValuePair.Create(x, GetTouchState(x))).GetEnumerator();

        public override byte[] Dump()
        {
            return BitConverter.GetBytes(state);
        }

        public override bool GetTouchState(TouchArea touch)
        {
            return ((state >> (int)touch) & 1) != 0;
        }

        public override void ResetAllTouchStates()
        {
            state = 0;
        }

        public override bool TrySetTouchState(TouchArea touch, bool isTouched)
        {
            state |= (isTouched ? 1UL : 0UL) << (int)touch;
            return true;
        }
    }
}

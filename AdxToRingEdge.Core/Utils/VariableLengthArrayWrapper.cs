using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.Utils
{
    public class VariableLengthArrayWrapper<T> : IDisposable
    {
        private T[] array = ArrayPool<T>.Shared.Rent(1024);
        public T[] Array => array;

        public void CheckSize(int size)
        {
            while (size > array.Length)
            {
                var newLength = array.Length * 2;
                ArrayPool<T>.Shared.Return(array);
                array = ArrayPool<T>.Shared.Rent(newLength);
                Log.Debug($"Resize VariableLengthArrayWrapper<{typeof(T)}> array to {newLength} (check {size})");
            }
        }

        public ReadOnlyMemory<T> GetPart(int length) => array.AsMemory().Slice(0, length);

        public void Dispose()
        {
            if (array != null)
            {
                ArrayPool<T>.Shared.Return(array);
                array = default;
            }
        }
    }
}

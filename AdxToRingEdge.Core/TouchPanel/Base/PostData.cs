using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel
{
    public struct PostData : IDisposable
    {
        private byte[] buffer;

        public Memory<byte> Data { get; private set; }

        public PostData(int size)
        {
            buffer = ArrayPool<byte>.Shared.Rent(size);
            Data = buffer.AsMemory().Slice(0, size);
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(buffer);
            Data = default;
        }

        public static PostData CreateWithCopy(Memory<byte> copyDataSource)
        {
            var copy = new PostData(copyDataSource.Length);
            copyDataSource.CopyTo(copy.Data);
            return copy;
        }
    }

}

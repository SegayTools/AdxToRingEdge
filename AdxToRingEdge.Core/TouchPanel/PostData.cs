using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel
{
    public class PostData : IDisposable
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

        public static PostData CreateWithCopy(byte[] copyDataSource)
        {
            var copy = new PostData(copyDataSource.Length);
            Array.Copy(copyDataSource, copy.buffer, copyDataSource.Length);
            return copy;
        }
    }

}

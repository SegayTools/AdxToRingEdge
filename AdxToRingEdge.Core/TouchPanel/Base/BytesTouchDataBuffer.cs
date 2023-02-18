using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Base
{
    public class BytesTouchDataBuffer : ITouchDataBuffer
    {
        private readonly byte[] data;
        public Span<byte> Buffer => data;

        public BytesTouchDataBuffer(int capacity) => data = new byte[capacity];

        public bool CopyConvertTo<T>(ref T toBuffer) where T : ITouchDataBuffer
        {
            if (toBuffer is BytesTouchDataBuffer toByteBuffer)
            {
                Array.Copy(data, toByteBuffer.data, data.Length);
                return true;
            }

            return false;
        }
    }
}

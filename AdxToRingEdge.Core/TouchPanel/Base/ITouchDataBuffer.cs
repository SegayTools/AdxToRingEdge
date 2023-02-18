using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Base
{
    public interface ITouchDataBuffer
    {
        bool CopyConvertTo<T>(ref T toBuffer) where T : ITouchDataBuffer;
    }
}

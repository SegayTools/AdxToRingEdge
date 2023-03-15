using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Windows.Interop.Native;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.NativeImplement.Windows.Interop
{
    internal class RawDevice
    {
        public string HWID { get; set; }
        public IntPtr Handle { get; set; }
        public RawInputDeviceType Type { get; set; }
        public string Name { get; set; }
        public int Id { get; set; }
    }
}

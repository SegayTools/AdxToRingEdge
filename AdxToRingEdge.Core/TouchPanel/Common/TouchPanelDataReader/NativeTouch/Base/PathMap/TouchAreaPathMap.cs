using AdxToRingEdge.Core.TouchPanel.Base;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base.PathMap.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base.PathMap
{
    public abstract class TouchAreaPathMap
    {
        public abstract IEnumerable<TouchArea> TouchAreas { get; }

        public abstract float Width { get; }
        public abstract float Height { get; }

        public abstract float BaseY { get; }
        public abstract float BaseX { get; }

        public abstract bool CheckPointInPath(TouchArea a, Vector2 p);
    }
}

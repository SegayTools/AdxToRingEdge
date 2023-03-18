using AdxToRingEdge.Core.TouchPanel.Base;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base.PathMap;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base.PathMap.Base;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base
{
    public class CustomTouchAreaPathMap : TouchAreaPathMap
    {
        private readonly Dictionary<TouchArea, TouchAreaPath> map;

        public override IEnumerable<TouchArea> TouchAreas => map.Keys;

        private float width;
        private float height;
        private float baseY;
        private float baseX;

        public override float Width => width;

        public override float Height => height;

        public override float BaseY => baseY;

        public override float BaseX => baseX;

        private CustomTouchAreaPathMap(Dictionary<TouchArea, TouchAreaPath> pathMap)
        {
            map = pathMap;

            var points = map.Values.SelectMany(x => x.Points).ToArray();
            var minY = points.Select(x => x.Y).Min();
            var maxY = points.Select(x => x.Y).Max();
            var minX = points.Select(x => x.X).Min();
            var maxX = points.Select(x => x.X).Max();

            width = maxX - minX;
            height = maxY - minY;
            baseY = minY;
            baseX = minX;
        }

        public override bool CheckPointInPath(TouchArea a, Vector2 p)
        {
            if (!map.TryGetValue(a, out var path))
                return false;

            return path.IsInArea(p);
        }

        public static CustomTouchAreaPathMap CreateFromJsonContent(string json)
        {
            var r = JsonSerializer.Deserialize<Dictionary<TouchArea, Vector2[]>>(json).ToDictionary(x => x.Key, x => new TouchAreaPath(x.Value));
            return new CustomTouchAreaPathMap(r);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base.PathMap.Base
{
    public record Vector2(float X, float Y)
    {
        public Vector2 RotateAround(Vector2 anchor, double degree)
        {
            var dx = anchor.X;
            var dy = anchor.Y;

            var a = degree * Math.PI / 180;

            var rx = (X - dx) * Math.Cos(a) - (Y - dy) * Math.Sin(a) + dx;
            var ry = (X - dx) * Math.Sin(a) + (Y - dy) * Math.Cos(a) + dy;

            return new((float)rx, (float)ry);
        }
    }
}

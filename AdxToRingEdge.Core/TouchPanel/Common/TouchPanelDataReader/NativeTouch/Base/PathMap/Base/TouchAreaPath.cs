using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base.PathMap.Base
{
    public record TouchAreaPath(Vector2[] Points)
    {
        bool onLine(Line l1, Vector2 p)
        {
            var maxY = Math.Max(l1.p1.Y, l1.p2.Y);
            var minY = Math.Min(l1.p1.Y, l1.p2.Y);
            var maxX = Math.Max(l1.p1.X, l1.p2.X);
            var minX = Math.Min(l1.p1.X, l1.p2.X);

            if (minX == maxX)
                return p.X == minX && minY <= p.Y && p.Y <= maxY;
            if (minY == maxY)
                return p.Y == minY && minX <= p.X && p.X <= maxX;

            var k = (l1.p1.Y - l1.p2.Y) / (l1.p1.X - l1.p2.X);
            var y = k * p.X;
            return y == p.Y;
        }

        bool direction(Vector2 a, Vector2 b, Vector2 c)
        {
            var val = (b.Y - a.Y) * (c.X - b.X)
              - (b.X - a.X) * (c.Y - b.Y);

            if (val == 0)

                // Colinear
                return false;

            else if (val < 0)

                // Anti-clockwise direction
                return false;

            // Clockwise direction
            return true;
        }

        int isIntersect(Line l1, Line l2)
        {
            // Four direction for two lines and points of other line
            var dir1 = direction(l1.p1, l1.p2, l2.p1);
            var dir2 = direction(l1.p1, l1.p2, l2.p2);
            var dir3 = direction(l2.p1, l2.p2, l1.p1);
            var dir4 = direction(l2.p1, l2.p2, l1.p2);

            // When intersecting
            if (dir1 != dir2 && dir3 != dir4)
                return 1;

            // When p2 of line2 are on the line1
            if (dir1 == false && onLine(l1, l2.p1) == true)
                return 1;

            // When p1 of line2 are on the line1
            if (dir2 == false && onLine(l1, l2.p2) == true)
                return 1;

            // When p2 of line1 are on the line2
            if (dir3 == false && onLine(l2, l1.p1) == true)
                return 1;

            // When p1 of line1 are on the line2
            if (dir4 == false && onLine(l2, l1.p2) == true)
                return 1;

            return 0;
        }

        public bool IsInArea(Vector2 p)
        {
            var poly = Points;
            var n = poly.Length;

            // Create a point at infinity, y is same as point p
            var pt = new Vector2(9999, p.Y);
            var exline = new Line(p, pt);
            int count = 0;
            int i = 0;
            do
            {
                // Forming a line from two consecutive points of
                // poly
                var side = new Line(poly[i], poly[(i + 1) % n]);
                if (isIntersect(side, exline) == 1)
                {

                    // If side is intersects exline
                    if (direction(side.p1, p, side.p2) == false)
                        return onLine(side, p);
                    count++;
                }
                i = (i + 1) % n;
            } while (i != 0);

            // When count is odd
            return (count & 1) != 0;
        }

        public override string ToString() => string.Join(" ", Points.Select(x => $"({x.X},{x.Y})"));
    }
}

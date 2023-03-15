using AdxToRingEdge.Core.TouchPanel.Base;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base.PathMap;
using AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base.PathMap.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;
using static AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base.FinaleTouchAreaPathMap;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base.FinaleTouchAreaPathMap>;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base
{
    public class FinaleTouchAreaPathMap : TouchAreaPathMap
    {
        private Dictionary<TouchArea, TouchAreaPath> pathMap = new();
        public override IEnumerable<TouchArea> TouchAreas => pathMap.Keys;

        private float width;
        public override float Width => width;

        private float height;
        public override float Height => height;

        private float baseY;
        public override float BaseY => baseY;

        private float baseX;
        public override float BaseX => baseX;

        public FinaleTouchAreaPathMap()
        {
            pathMap[TouchArea.A1] = new TouchAreaPath(new Vector2[] { new(579, 21), new(878, 145), new(765, 260), new(700, 260), new(623, 228), new(579, 183) });
            pathMap[TouchArea.A2] = new TouchAreaPath(new Vector2[] { new(819, 315), new(934, 200), new(1057, 500), new(897, 500), new(852, 455), new(819, 380) });
            pathMap[TouchArea.A3] = new TouchAreaPath(new Vector2[] { new(819, 700), new(852, 625), new(897, 579), new(1057, 580), new(934, 878), new(819, 764) });
            pathMap[TouchArea.A4] = new TouchAreaPath(new Vector2[] { new(579, 896), new(622, 853), new(700, 819), new(765, 819), new(878, 934), new(579, 1059) });
            pathMap[TouchArea.A5] = new TouchAreaPath(new Vector2[] { new(200, 934), new(315, 819), new(378, 819), new(455, 852), new(500, 896), new(500, 1059) });
            pathMap[TouchArea.A6] = new TouchAreaPath(new Vector2[] { new(21, 579), new(182, 579), new(228, 622), new(260, 700), new(260, 765), new(145, 878) });
            pathMap[TouchArea.A7] = new TouchAreaPath(new Vector2[] { new(21, 500), new(145, 200), new(260, 315), new(260, 380), new(228, 455), new(182, 500) });
            pathMap[TouchArea.A8] = new TouchAreaPath(new Vector2[] { new(200, 145), new(500, 21), new(500, 183), new(455, 228), new(378, 260), new(314, 260) });

            pathMap[TouchArea.B1] = new TouchAreaPath(new Vector2[] { new(566, 298), new(590, 274), new(618, 274), new(674, 296), new(691, 313), new(691, 348), new(632, 409), new(594, 409), new(566, 382) });
            pathMap[TouchArea.B2] = new TouchAreaPath(new Vector2[] { new(670, 448), new(730, 389), new(766, 389), new(782, 404), new(805, 462), new(805, 488), new(780, 512), new(679, 512), new(670, 486) });
            pathMap[TouchArea.B3] = new TouchAreaPath(new Vector2[] { new(670, 594), new(697, 567), new(780, 567), new(805, 591), new(805, 618), new(784, 671), new(766, 691), new(731, 691), new(670, 631) });
            pathMap[TouchArea.B4] = new TouchAreaPath(new Vector2[] { new(567, 697), new(594, 670), new(630, 670), new(691, 730), new(691, 766), new(672, 784), new(618, 805), new(590, 805), new(567, 781) });
            pathMap[TouchArea.B5] = new TouchAreaPath(new Vector2[] { new(388, 730), new(448, 670), new(485, 670), new(512, 697), new(512, 781), new(489, 805), new(460, 805), new(406, 783), new(388, 767) });
            pathMap[TouchArea.B6] = new TouchAreaPath(new Vector2[] { new(274, 590), new(298, 567), new(382, 567), new(409, 594), new(409, 631), new(349, 691), new(313, 691), new(297, 675), new(274, 618) });
            pathMap[TouchArea.B7] = new TouchAreaPath(new Vector2[] { new(274, 462), new(296, 406), new(313, 388), new(350, 388), new(409, 448), new(409, 486), new(382, 512), new(298, 512), new(274, 490) });
            pathMap[TouchArea.B8] = new TouchAreaPath(new Vector2[] { new(388, 313), new(405, 297), new(460, 274), new(489, 274), new(512, 298), new(512, 382), new(485, 409), new(449, 409), new(388, 349) });

            pathMap[TouchArea.C] = new TouchAreaPath(new Vector2[] { new(455, 504), new(504, 455), new(575, 455), new(625, 504), new(625, 575), new(575, 625), new(504, 625), new(455, 575) });

            var points = pathMap.Values.SelectMany(x => x.Points).ToArray();
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
            if (!pathMap.TryGetValue(a, out var path))
                return false;

            return path.IsInArea(p);
        }
        /*
        #region Drawing

        Dictionary<TouchArea, List<Vector2>> drawingAreaContainer = new();
        TouchArea currentDrawingArea = default;
        TouchEventArg currentDrawingTouch = default;

        public void AddTouch(TouchArea area, TouchEventArg t)
        {
            if (!drawingAreaContainer.TryGetValue(currentDrawingArea, out var list))
                list = drawingAreaContainer[currentDrawingArea] = new();

            list.Add(new Vector2(t.X, t.Y));
            LogEntity.User($"Add {area} last point: {t}");
        }

        public void RegisterCurrentTouch(TouchEventArg t)
        {
            currentDrawingTouch = t;
            //LogEntity.User($"Set currentDrawingTouch: {currentDrawingTouch}");
        }

        public void DelPrevTouch(TouchArea area)
        {
            if (drawingAreaContainer.TryGetValue(area, out var list) && list.Count > 0)
            {
                var p = list.LastOrDefault();
                list.Remove(p);
                LogEntity.User($"Deleted touch {area} last point: {p}");
            }
        }

        public void SaveFile()
        {
            var path = Path.GetFullPath("./saveDrawing.txt");
            using var fs = File.OpenWrite(path);
            using var writer = new StreamWriter(fs);

            foreach (var pair in drawingAreaContainer)
                writer.WriteLine($"a[TouchArea.{pair.Key}] = new TouchAreaPath(new Vector2[] {{{string.Join(",", pair.Value.Select(x => $"new ({x.X},{x.Y})"))}}})");
            LogEntity.User($"Saved to {path}");
        }

        #endregion
        */

        internal bool TryProcessUserInput(string[] args)
        {
            switch (args[0].ToLower().Trim())
            {
                /*
                case "set-area":
                    var r = args[1].Trim();
                    if (Enum.TryParse<TouchArea>(args[1].Trim(), true, out var c))
                    {
                        currentDrawingArea = c;
                        LogEntity.User($"set currentDrawingArea: {currentDrawingArea}");
                    }
                    else
                        LogEntity.User($"set currentDrawingArea failed, unknown content: {r}");
                    break;
                case "add-touch":
                    AddTouch(currentDrawingArea, currentDrawingTouch);
                    break;
                case "del-touch":
                    DelPrevTouch(currentDrawingArea);
                    break;
                case "save-file":
                    SaveFile();
                    break;

                */
                default:
                    return false;
            }

            return true;
        }
    }
}

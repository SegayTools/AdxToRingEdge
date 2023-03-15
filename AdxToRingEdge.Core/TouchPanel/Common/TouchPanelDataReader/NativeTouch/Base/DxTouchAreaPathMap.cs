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
using static AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base.DxTouchAreaPathMap;

using LogEntity = AdxToRingEdge.Core.Log<AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base.DxTouchAreaPathMap>;

namespace AdxToRingEdge.Core.TouchPanel.Common.TouchPanelDataReader.NativeTouch.Base
{
    public class DxTouchAreaPathMap : TouchAreaPathMap
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

        public DxTouchAreaPathMap()
        {
            void addTemplate(TouchAreaPath tempalte, string prefix, int count)
            {
                var centerAnchor = new Vector2(1080f / 2, 1080f / 2);

                for (int i = 0; i < count; i++)
                {
                    var appendDegree = 360.0f / count * i;
                    var copyTouch = new TouchAreaPath(tempalte.Points.Select((x, t) =>
                    {
                        var offset = Math.Atan2(tempalte.Points[t].Y - centerAnchor.Y, tempalte.Points[t].Y - centerAnchor.X);
                        var offsetDegree = offset * 180 / Math.PI;
                        return x.RotateAround(centerAnchor, (offsetDegree + appendDegree) % 360);
                    }).ToArray());

                    var newArea = Enum.Parse<TouchArea>($"{prefix}{i + 1}");
                    pathMap[newArea] = copyTouch;
                }
            }

            pathMap[TouchArea.A1] = new TouchAreaPath(new Vector2[] { new(620, 11), new(866, 4), new(859, 110), new(745, 260), new(695, 260), new(627, 231), new(593, 197) });
            pathMap[TouchArea.A2] = new TouchAreaPath(new Vector2[] { new(820, 335), new(972, 222), new(1077, 225), new(1070, 461), new(883, 487), new(849, 453), new(822, 385) });
            pathMap[TouchArea.A3] = new TouchAreaPath(new Vector2[] { new(884, 593), new(1071, 620), new(1079, 857), new(972, 859), new(821, 745), new(821, 695), new(849, 628) });
            pathMap[TouchArea.A4] = new TouchAreaPath(new Vector2[] { new(746, 821), new(860, 972), new(851, 1076), new(620, 1070), new(594, 883), new(629, 849), new(696, 821) });
            pathMap[TouchArea.A5] = new TouchAreaPath(new Vector2[] { new(488, 882), new(462, 1070), new(224, 1080), new(222, 972), new(337, 821), new(386, 820), new(456, 850) });
            pathMap[TouchArea.A6] = new TouchAreaPath(new Vector2[] { new(262, 762), new(111, 859), new(0, 863), new(13, 619), new(200, 594), new(233, 627), new(261, 696) });
            pathMap[TouchArea.A7] = new TouchAreaPath(new Vector2[] { new(200, 487), new(12, 462), new(2, 228), new(110, 222), new(262, 337), new(262, 385), new(232, 456) });
            pathMap[TouchArea.A8] = new TouchAreaPath(new Vector2[] { new(336, 259), new(223, 110), new(224, 2), new(463, 10), new(489, 189), new(455, 232), new(384, 260) });

            pathMap[TouchArea.D1] = new TouchAreaPath(new Vector2[] { new(472, 8), new(541, 0), new(610, 10), new(584, 189), new(542, 146), new(500, 189) });
            pathMap[TouchArea.D2] = new TouchAreaPath(new Vector2[] { new(760, 262), new(869, 118), new(1062, 41), new(967, 213), new(822, 323), new(822, 263) });
            pathMap[TouchArea.D3] = new TouchAreaPath(new Vector2[] { new(895, 499), new(1075, 473), new(1080, 544), new(1075, 610), new(894, 585), new(937, 541) });
            pathMap[TouchArea.D4] = new TouchAreaPath(new Vector2[] { new(821, 758), new(965, 868), new(981, 965), new(869, 964), new(760, 821), new(821, 820) });
            pathMap[TouchArea.D5] = new TouchAreaPath(new Vector2[] { new(584, 893), new(610, 1071), new(545, 1080), new(472, 1072), new(498, 894), new(541, 934) });
            pathMap[TouchArea.D6] = new TouchAreaPath(new Vector2[] { new(324, 820), new(213, 963), new(122, 956), new(117, 868), new(262, 760), new(262, 821) });
            pathMap[TouchArea.D7] = new TouchAreaPath(new Vector2[] { new(189, 583), new(11, 609), new(0, 545), new(10, 471), new(189, 496), new(147, 540) });
            pathMap[TouchArea.D8] = new TouchAreaPath(new Vector2[] { new(262, 322), new(117, 212), new(123, 129), new(213, 116), new(323, 261), new(262, 261) });

            pathMap[TouchArea.E1] = new TouchAreaPath(new Vector2[] { new(541, 161), new(613, 233), new(541, 305), new(468, 233) });
            pathMap[TouchArea.E2] = new TouchAreaPath(new Vector2[] { new(708, 273), new(809, 271), new(810, 375), new(707, 373) });
            pathMap[TouchArea.E3] = new TouchAreaPath(new Vector2[] { new(849, 468), new(921, 539), new(848, 611), new(775, 539) });
            pathMap[TouchArea.E4] = new TouchAreaPath(new Vector2[] { new(708, 706), new(809, 706), new(809, 807), new(707, 807) });
            pathMap[TouchArea.E5] = new TouchAreaPath(new Vector2[] { new(541, 776), new(613, 847), new(541, 919), new(469, 847) });
            pathMap[TouchArea.E6] = new TouchAreaPath(new Vector2[] { new(374, 705), new(375, 807), new(273, 807), new(273, 705) });
            pathMap[TouchArea.E7] = new TouchAreaPath(new Vector2[] { new(234, 468), new(306, 539), new(234, 611), new(161, 539) });
            pathMap[TouchArea.E8] = new TouchAreaPath(new Vector2[] { new(273, 272), new(373, 272), new(373, 373), new(273, 373) });

            pathMap[TouchArea.B1] = new TouchAreaPath(new Vector2[] { new(601, 261), new(699, 301), new(695, 379), new(667, 405), new(594, 403), new(547, 353), new(547, 314) });
            pathMap[TouchArea.B2] = new TouchAreaPath(new Vector2[] { new(781, 386), new(821, 480), new(765, 534), new(727, 534), new(676, 481), new(677, 412), new(705, 385) });
            pathMap[TouchArea.B3] = new TouchAreaPath(new Vector2[] { new(765, 545), new(821, 600), new(782, 694), new(704, 696), new(676, 668), new(676, 597), new(727, 545) });
            pathMap[TouchArea.B4] = new TouchAreaPath(new Vector2[] { new(696, 779), new(602, 819), new(546, 764), new(547, 726), new(597, 676), new(669, 676), new(697, 704) });
            pathMap[TouchArea.B5] = new TouchAreaPath(new Vector2[] { new(535, 726), new(535, 766), new(481, 819), new(385, 779), new(385, 702), new(412, 676), new(484, 676) });
            pathMap[TouchArea.B6] = new TouchAreaPath(new Vector2[] { new(378, 695), new(302, 696), new(261, 600), new(317, 545), new(355, 546), new(405, 597), new(405, 667) });
            pathMap[TouchArea.B7] = new TouchAreaPath(new Vector2[] { new(354, 535), new(316, 534), new(261, 479), new(303, 384), new(377, 384), new(405, 412), new(406, 483) });
            pathMap[TouchArea.B8] = new TouchAreaPath(new Vector2[] { new(481, 261), new(535, 315), new(535, 355), new(486, 404), new(413, 404), new(386, 377), new(385, 300) });

            pathMap[TouchArea.C1] = new TouchAreaPath(new Vector2[] { new(547, 438), new(584, 438), new(644, 498), new(643, 582), new(584, 643), new(547, 642) });
            pathMap[TouchArea.C2] = new TouchAreaPath(new Vector2[] { new(536, 438), new(535, 643), new(499, 643), new(438, 581), new(438, 498), new(498, 438) });

            //addTemplate(pathMap[TouchArea.A1], "A", 8);

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

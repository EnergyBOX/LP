using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace LP
{
    public static class GeometryUtils
    {
        /// <summary>
        /// Обчислює верхівку елемента як центр мас верхнього "слайсу" BoundingBox (товщина ~ 1 мм).
        /// </summary>
        public static List<XYZ> ComputeTipPoints(Document doc, List<Element> rods)
        {
            var tips = new List<XYZ>();
            double mm = UnitUtils.ConvertToInternalUnits(1.0, UnitTypeId.Millimeters);

            var opt = new Options
            {
                ComputeReferences = false,
                IncludeNonVisibleObjects = false,
                DetailLevel = ViewDetailLevel.Fine
            };

            foreach (var e in rods)
            {
                var bbox = e.get_BoundingBox(null);
                if (bbox == null) continue;

                double zTop = bbox.Max.Z;
                double zBottom = zTop - mm;

                // Проходимо по твердим тілам і вибираємо їхні вершини в тонкому шарі
                var geo = e.get_Geometry(opt);
                if (geo == null) continue;

                var pts = new List<XYZ>();
                foreach (var obj in geo)
                {
                    if (obj is GeometryInstance gi)
                    {
                        var inst = gi.GetInstanceGeometry();
                        GatherPointsInZBand(inst, zBottom, zTop, pts);
                    }
                    else if (obj is Solid s && s.Volume > 1e-9)
                    {
                        GatherPointsInZBand(s, zBottom, zTop, pts);
                    }
                }

                if (pts.Count == 0)
                {
                    // fallback — беремо центр верхньої площини bbox
                    tips.Add(new XYZ((bbox.Min.X + bbox.Max.X) * 0.5, (bbox.Min.Y + bbox.Max.Y) * 0.5, bbox.Max.Z));
                }
                else
                {
                    // центр мас точок у шарі
                    double cx = 0, cy = 0, cz = 0;
                    foreach (var p in pts) { cx += p.X; cy += p.Y; cz += p.Z; }
                    tips.Add(new XYZ(cx / pts.Count, cy / pts.Count, cz / pts.Count));
                }
            }

            return tips;
        }

        private static void GatherPointsInZBand(GeometryElement ge, double zBottom, double zTop, List<XYZ> acc)
        {
            foreach (var g in ge)
            {
                if (g is Solid s && s.Volume > 1e-9) GatherPointsInZBand(s, zBottom, zTop, acc);
                if (g is GeometryInstance gi) GatherPointsInZBand(gi.GetInstanceGeometry(), zBottom, zTop, acc);
            }
        }

        private static void GatherPointsInZBand(Solid s, double zBottom, double zTop, List<XYZ> acc)
        {
            foreach (Face f in s.Faces)
            {
                Mesh m = f.Triangulate();
                for (int i = 0; i < m.Vertices.Count; i++)
                {
                    var v = m.Vertices[i];
                    if (v.Z >= zBottom && v.Z <= zTop) acc.Add(v);
                }
            }
        }
    }
}

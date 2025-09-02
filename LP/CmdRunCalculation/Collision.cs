using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace LP
{
    public static class Collision
    {
        public static bool HasInteriorPoint(SpatialHash2D index, List<XYZ> pts, XYZ center, double R, int[] exclude)
        {
            double r2 = R * R - 1e-9;
            var neigh = index.Query(center, R);
            foreach (int id in neigh)
            {
                if (Array.Exists(exclude, e => e == id)) continue;
                var v = pts[id] - center;
                if (v.DotProduct(v) < r2) return true;
            }
            return false;
        }
    }

    public readonly struct Sphere
    {
        public XYZ Center { get; }
        public double Radius { get; }

        public Sphere(XYZ c, double r) { Center = c; Radius = r; }

        public static List<Sphere> Deduplicate(List<Sphere> list, double tol)
        {
            double tol2 = tol * tol;
            var res = new List<Sphere>();
            foreach (var s in list)
            {
                bool dup = false;
                foreach (var t in res)
                {
                    if (Math.Abs(s.Radius - t.Radius) <= tol &&
                        (s.Center - t.Center).DotProduct(s.Center - t.Center) <= tol2)
                    {
                        dup = true; break;
                    }
                }
                if (!dup) res.Add(s);
            }
            return res;
        }
    }
}

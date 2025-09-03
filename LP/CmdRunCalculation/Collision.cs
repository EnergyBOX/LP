using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace LP
{
    public static class Collision
    {
        public static bool HasInteriorPoint(SpatialHash3D index, List<XYZ> tips, XYZ center, double radius, int[] exclude)
        {
            var neighbors = index.Query(center, radius);
            foreach (int i in neighbors)
            {
                if (exclude.Contains(i)) continue;
                if (center.DistanceTo(tips[i]) < radius) return true;
            }
            return false;
        }
    }
}

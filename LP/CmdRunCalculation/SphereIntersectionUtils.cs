using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace LP
{
    /// <summary>
    /// Обчислення перетину трьох сфер однакового радіуса.
    /// </summary>
    public static class SphereIntersectionUtils
    {
        public static List<XYZ> IntersectThreeSpheres(XYZ c1, XYZ c2, XYZ c3, double radius)
        {
            List<XYZ> results = new List<XYZ>();

            XYZ ex = (c2 - c1).Normalize();
            double i = ex.DotProduct(c3 - c1);
            XYZ ey = ((c3 - c1) - i * ex).Normalize();
            XYZ ez = ex.CrossProduct(ey);

            double d = c1.DistanceTo(c2);
            double j = ey.DotProduct(c3 - c1);

            double x = d / 2.0; // спрощене рівняння
            double y = (i * i + j * j - 2 * i * x) / (2 * j);
            double z2 = radius * radius - x * x - y * y;

            if (z2 < 0) return results;

            double z = Math.Sqrt(z2);

            XYZ result1 = c1 + x * ex + y * ey + z * ez;
            XYZ result2 = c1 + x * ex + y * ey - z * ez;

            if (Math.Abs(z) < 1e-6)
            {
                results.Add(result1);
            }
            else
            {
                if (Math.Abs(result1.Z - result2.Z) < 1e-6) return results;
                results.Add(result1.Z > result2.Z ? result1 : result2);
            }

            return results;
        }
    }
}

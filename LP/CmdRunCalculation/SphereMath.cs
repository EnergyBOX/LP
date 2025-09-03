using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace LP
{
    public static class SphereMath
    {
        public static List<XYZ> IntersectThreeEqualSpheres(XYZ p1, XYZ p2, XYZ p3, double radius)
        {
            var results = new List<XYZ>();

            XYZ e1 = (p2 - p1).Normalize();
            double d = p1.DistanceTo(p2);
            if (d > 2 * radius) return results;

            XYZ temp = p3 - p1;
            double i = e1.DotProduct(temp);
            XYZ e2 = (temp - i * e1).Normalize();
            double j = e2.DotProduct(temp);

            double x = d / 2.0;
            double y = (i - x) * (j != 0 ? j / j : 1);
            double zSquared = radius * radius - x * x - y * y;
            if (zSquared < 0) return results;

            double z = Math.Sqrt(zSquared);
            XYZ zDir = e1.CrossProduct(e2).Normalize();

            XYZ center1 = p1 + x * e1 + y * e2 + z * zDir;
            XYZ center2 = p1 + x * e1 + y * e2 - z * zDir;

            results.Add(center1);
            results.Add(center2);
            return results;
        }
    }
}

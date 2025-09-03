using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace LP
{
    public class Sphere
    {
        public XYZ Center { get; }
        public double Radius { get; }
        public ElementId FamilyInstanceId { get; set; } = ElementId.InvalidElementId;

        public Sphere(XYZ center, double radius)
        {
            Center = center;
            Radius = radius;
        }

        public static List<Sphere> Deduplicate(List<Sphere> spheres, double tol)
        {
            var result = new List<Sphere>();
            foreach (var s in spheres)
            {
                bool exists = result.Exists(x => x.Center.DistanceTo(s.Center) < tol && Math.Abs(x.Radius - s.Radius) < tol);
                if (!exists) result.Add(s);
            }
            return result;
        }
    }
}

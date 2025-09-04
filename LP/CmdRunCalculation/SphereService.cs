using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace LP
{
    /// <summary>
    /// Сервіс розміщення сфер на основі трикутників між верхівками.
    /// </summary>
    public static class SphereService
    {
        public static int PlaceSpheres(Document doc, List<XYZ> tips, double radius, string familyName)
        {
            int placed = 0;
            double tolerance = 0.001;

            for (int i = 0; i < tips.Count; i++)
            {
                for (int j = i + 1; j < tips.Count; j++)
                {
                    for (int k = j + 1; k < tips.Count; k++)
                    {
                        var pts = SphereIntersectionUtils.IntersectThreeSpheres(tips[i], tips[j], tips[k], radius);
                        int[] indices = { i, j, k };

                        foreach (var pt in pts)
                        {
                            if (SphereUtils.IsTipInsideSphere(pt, radius, tips, indices)) continue;
                            if (SphereUtils.IsSphereAlreadyPlaced(doc, pt, familyName, tolerance)) continue;

                            XYZ chosen = pts.Count == 2
                                ? pts.OrderByDescending(p => p.Z).First()
                                : pt;

                            using (Transaction t = new Transaction(doc, "Place Sphere"))
                            {
                                t.Start();
                                FamilyUtils.PlaceSphereVoid(doc, chosen, radius);
                                t.Commit();
                            }

                            placed++;
                            break;
                        }
                    }
                }
            }

            return placed;
        }
    }
}

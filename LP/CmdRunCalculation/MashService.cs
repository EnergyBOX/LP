using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LP
{
    /// <summary>
    /// Сервіс розміщення LP_Mesh на основі трикутників між верхівками.
    /// Використовує перевірки колізій з іншими верхівками.
    /// </summary>
    public static class MashService
    {
        /// <summary>
        /// Розміщує LP_Mesh на всіх допустимих трійках верхівок.
        /// </summary>
        public static int PlaceMashes(Document doc, List<XYZ> tips, double radius, string familyName)
        {
            int placed = 0;

            // --- 1. Створюємо список усіх можливих трійок верхівок ---
            var triplets = new List<(XYZ, XYZ, XYZ)>();
            for (int i = 0; i < tips.Count; i++)
                for (int j = i + 1; j < tips.Count; j++)
                    for (int k = j + 1; k < tips.Count; k++)
                        triplets.Add((tips[i], tips[j], tips[k]));

            // --- 2. Для кожної трійки розраховуємо потенційні центри сфер ---
            var spheresCenters = new Dictionary<(XYZ, XYZ, XYZ), List<XYZ>>();
            foreach (var triplet in triplets)
            {
                var centers = SphereIntersectionUtils.IntersectThreeSpheres(
                    triplet.Item1, triplet.Item2, triplet.Item3, radius);
                spheresCenters[triplet] = centers;
            }

            // --- 3. Фільтруємо трійки, залишаємо лише ті, де верхня точка не колізує з іншими верхівками ---
            var filteredTriplets = new List<(XYZ, XYZ, XYZ, XYZ)>(); // останній XYZ = центр сфери
            foreach (var kvp in spheresCenters)
            {
                var triplet = kvp.Key;
                var centers = kvp.Value;
                XYZ[] tripletArray = new XYZ[] { triplet.Item1, triplet.Item2, triplet.Item3 };

                foreach (var center in centers)
                {
                    bool collision = false;
                    foreach (var tip in tips)
                    {
                        if (tripletArray.Contains(tip)) continue;
                        if (tip.DistanceTo(center) < radius)
                        {
                            collision = true;
                            break;
                        }
                    }

                    if (!collision)
                    {
                        filteredTriplets.Add((triplet.Item1, triplet.Item2, triplet.Item3, center));
                        break; // беремо перший допустимий центр
                    }
                }
            }

            // --- 4. Сортуємо трійки за годинниковою стрілкою ---
            var finalTriplets = filteredTriplets
                .Select(t => (
                    SortClockwise(t.Item1, t.Item2, t.Item3), // повертає масив/список XYZ
                    t.Item4
                ))
                .ToList();

            // --- 5. Вставляємо LP_Mesh у одну транзакцію ---
            using (Transaction t = new Transaction(doc, "Place all LP_Mesh"))
            {
                t.Start();
                foreach (var (sortedPoints, center) in finalTriplets)
                {
                    FamilyUtils.PlaceMash(doc, sortedPoints, radius);
                    placed++;
                }
                t.Commit();
            }

            return placed;
        }

        /// <summary>
        /// Сортування трьох точок за годинниковою стрілкою у площині трикутника.
        /// </summary>
        private static List<XYZ> SortClockwise(XYZ a, XYZ b, XYZ c)
        {
            var pts = new List<XYZ> { a, b, c };
            XYZ normal = (b - a).CrossProduct(c - a).Normalize();
            XYZ xAxis = (b - a).Normalize();
            XYZ yAxis = normal.CrossProduct(xAxis);

            var angles = pts.Select(p =>
            {
                XYZ v = p - a;
                double x = v.DotProduct(xAxis);
                double y = v.DotProduct(yAxis);
                return Math.Atan2(y, x);
            }).ToList();

            return pts.Zip(angles, (pt, ang) => new { pt, ang })
                      .OrderByDescending(x => x.ang) // годинникова стрілка
                      .Select(x => x.pt)
                      .ToList();
        }
    }
}

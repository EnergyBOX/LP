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
        /// Розміщує LP_Mesh на всіх допустимих трійках верхівок та повертає лог координат.
        /// </summary>
        /// <param name="doc">Документ Revit</param>
        /// <param name="tips">Список верхівок (XYZ)</param>
        /// <param name="radius">Радіус сфери для LP_Mesh</param>
        /// <param name="familyName">Назва сімейства (LP_Mesh)</param>
        /// <returns>Кортеж: кількість вставлених, лог координат (запропоновані / фактичні)</returns>
        public static (int placed, List<List<(XYZ proposed, XYZ actual)>> pointsLog) PlaceMashes(
            Document doc, List<XYZ> tips, double radius, string familyName)
        {
            int placed = 0;
            var allPointsLog = new List<List<(XYZ proposed, XYZ actual)>>();

            // --- 1. Створюємо список усіх можливих трійок верхівок ---
            var triplets = new List<(XYZ, XYZ, XYZ)>();
            for (int i = 0; i < tips.Count; i++)
                for (int j = i + 1; j < tips.Count; j++)
                    for (int k = j + 1; k < tips.Count; k++)
                        triplets.Add((tips[i], tips[j], tips[k]));

            // --- 2. Розраховуємо потенційні центри сфер ---
            var spheresCenters = new Dictionary<(XYZ, XYZ, XYZ), List<XYZ>>();
            foreach (var triplet in triplets)
            {
                var centers = SphereIntersectionUtils.IntersectThreeSpheres(
                    triplet.Item1, triplet.Item2, triplet.Item3, radius);
                spheresCenters[triplet] = centers;
            }

            // --- 3. Фільтруємо трійки ---
            var filteredTriplets = new List<(XYZ, XYZ, XYZ, XYZ)>();
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
                        break;
                    }
                }
            }

            // --- 4. Сортуємо трійки за годинниковою стрілкою ---
            var finalTriplets = filteredTriplets
                .Select(t => (SortClockwise(t.Item1, t.Item2, t.Item3), t.Item4))
                .ToList();

            // --- 5. Вставляємо LP_Mesh ---
            using (Transaction t = new Transaction(doc, "Place all LP_Mesh"))
            {
                t.Start();
                foreach (var (sortedPoints, center) in finalTriplets)
                {
                    // Вставка adaptive component та логування координат
                    var (instance, pointsLog) = FamilyUtils.PlaceMash(doc, sortedPoints, radius);
                    allPointsLog.Add(pointsLog);

                    placed++;
                }
                t.Commit();
            }

            return (placed, allPointsLog);
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
                      .OrderByDescending(x => x.ang)
                      .Select(x => x.pt)
                      .ToList();
        }
    }
}

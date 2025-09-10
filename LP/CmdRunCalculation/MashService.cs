using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LP
{
    /// <summary>
    /// Сервіс розміщення LP_Mesh на основі трикутників між верхівками.
    /// </summary>
    public static class MashService
    {
        /// <summary>
        /// Розміщує LP_Mesh на всіх допустимих трійках верхівок та повертає лог координат.
        /// </summary>
        public static (int placed, List<List<(XYZ proposed, XYZ actual)>> pointsLog) PlaceMashes(
            Document doc, List<XYZ> tips, double radius, string familyName)
        {
            int placedCount = 0;
            var allPointsLog = new List<List<(XYZ proposed, XYZ actual)>>();

            // --- 1. Створюємо всі можливі трійки верхівок, але лише ті, де сторони <= 2*radius ---
            var triplets = new List<(XYZ, XYZ, XYZ)>();
            for (int i = 0; i < tips.Count; i++)
            {
                for (int j = i + 1; j < tips.Count; j++)
                {
                    double dij = tips[i].DistanceTo(tips[j]);
                    if (dij > 2 * radius) continue; // відсікаємо надто великі відстані

                    for (int k = j + 1; k < tips.Count; k++)
                    {
                        double dik = tips[i].DistanceTo(tips[k]);
                        double djk = tips[j].DistanceTo(tips[k]);

                        if (dik <= 2 * radius && djk <= 2 * radius)
                        {
                            triplets.Add((tips[i], tips[j], tips[k]));
                        }
                    }
                }
            }


            // --- 2. Обчислюємо потенційні центри сфер ---
            var spheresCenters = new Dictionary<(XYZ, XYZ, XYZ), List<XYZ>>();
            foreach (var triplet in triplets)
            {
                spheresCenters[triplet] = SphereIntersectionUtils.IntersectThreeSpheres(
                    triplet.Item1, triplet.Item2, triplet.Item3, radius);
            }

            // --- 3. Фільтруємо трійки, щоб уникнути колізій ---
            var filteredTriplets = new List<(XYZ, XYZ, XYZ, XYZ)>();
            foreach (var kvp in spheresCenters)
            {
                var triplet = kvp.Key;
                var centers = kvp.Value;
                XYZ[] tripletArray = new XYZ[] { triplet.Item1, triplet.Item2, triplet.Item3 };

                foreach (var center in centers)
                {
                    if (!tips.Any(tip => !tripletArray.Contains(tip) && tip.DistanceTo(center) < radius))
                    {
                        filteredTriplets.Add((triplet.Item1, triplet.Item2, triplet.Item3, center));
                        break;
                    }
                }
            }

            // --- 4. Сортуємо трійки за годинниковою стрілкою у площині XY ---
            var finalTriplets = filteredTriplets
                .Select(t => (SortClockwiseXY(t.Item1, t.Item2, t.Item3), t.Item4))
                .ToList();

            // --- 5. Вставляємо LP_Mesh в одній транзакції і формуємо лог ---
            using (Transaction t = new Transaction(doc, "Place All LP_Meshes"))
            {
                t.Start();

                foreach (var (sortedPoints, center) in finalTriplets)
                {
                    FamilyUtils.PlaceMash(doc, sortedPoints, radius);

                    placedCount++;

                    var log = sortedPoints.Select(p => (proposed: p, actual: p)).ToList();
                    allPointsLog.Add(log);
                }

                t.Commit();
            }


            return (placedCount, allPointsLog);
        }

        /// <summary>
        /// Сортування трьох точок за годинниковою стрілкою у площині XY.
        /// </summary>
        private static List<XYZ> SortClockwiseXY(XYZ a, XYZ b, XYZ c)
        {
            var pts = new List<XYZ> { a, b, c };

            // Центр трикутника по XY
            double centerX = (a.X + b.X + c.X) / 3.0;
            double centerY = (a.Y + b.Y + c.Y) / 3.0;

            // Обчислюємо кути відносно центру
            var angles = pts.Select(p =>
            {
                double dx = p.X - centerX;
                double dy = p.Y - centerY;
                return Math.Atan2(dy, dx); // atan2 для XY
            }).ToList();

            // Сортуємо по куту за годинниковою стрілкою
            return pts.Zip(angles, (pt, ang) => new { pt, ang })
                      .OrderByDescending(x => x.ang)
                      .Select(x => x.pt)
                      .ToList();
        }
    }
}

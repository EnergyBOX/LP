using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

            // Формуємо рядок для TaskDialog
            StringBuilder sb = new StringBuilder();
            int index = 1;
            foreach (var item in filteredTriplets)
            {
                sb.AppendLine(
                    $"{index}. " +
                    $"Триплет: " +
                    $"({item.Item1.X:F2},{item.Item1.Y:F2},{item.Item1.Z:F2}); " +
                    $"({item.Item2.X:F2},{item.Item2.Y:F2},{item.Item2.Z:F2}); " +
                    $"({item.Item3.X:F2},{item.Item3.Y:F2},{item.Item3.Z:F2}) " +
                    $"=> Центр: ({item.Item4.X:F2},{item.Item4.Y:F2},{item.Item4.Z:F2})"
                );
                index++;
            }

            // Показуємо TaskDialog
            TaskDialog.Show("Фільтровані триплети і центри", sb.ToString());

            // --- 4. Сортуємо трійки за годинниковою стрілкою ---
            var finalTriplets = filteredTriplets
                .Select(t => (
                    SortClockwise(t.Item1, t.Item2, t.Item3), // повертає масив/список XYZ
                    t.Item4
                ))
                .ToList();

            // Формуємо рядок для TaskDialog після сортування
            StringBuilder sbFinal = new StringBuilder();
            int idx = 1;
            foreach (var item in finalTriplets)
            {
                var sorted = item.Item1; // відсортований масив (XYZ[])
                var center = item.Item2;

                sbFinal.AppendLine(
                    $"{idx}. " +
                    $"Триплет (CW): " +
                    $"({sorted[0].X:F2},{sorted[0].Y:F2},{sorted[0].Z:F2}); " +
                    $"({sorted[1].X:F2},{sorted[1].Y:F2},{sorted[1].Z:F2}); " +
                    $"({sorted[2].X:F2},{sorted[2].Y:F2},{sorted[2].Z:F2}) " +
                    $"=> Центр: ({center.X:F2},{center.Y:F2},{center.Z:F2})"
                );
                idx++;
            }

            // Показуємо TaskDialog
            TaskDialog.Show("Фінальні відсортовані триплети", sbFinal.ToString());


            // --- 5. Вставляємо LP_Mesh ---
            foreach (var (sortedPoints, center) in finalTriplets)
            {
                using (Transaction t = new Transaction(doc, "Place LP_Mesh"))
                {
                    t.Start();
                    FamilyUtils.PlaceMash(doc, sortedPoints, radius);
                    t.Commit();
                }
                placed++;
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

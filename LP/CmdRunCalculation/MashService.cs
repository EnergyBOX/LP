using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace LP
{
    /// <summary>
    /// Сервіс розміщення LP_Mash на основі трикутників між верхівками.
    /// Використовує перевірки з розрахунку сфер.
    /// </summary>
    public static class MashService
    {
        public static int PlaceMashes(Document doc, List<XYZ> tips, double radius, string familyName)
        {
            int placed = 0;
            double tolerance = 0.001;

            for (int i = 0; i < tips.Count; i++)
            {
                for (int j = i + 1; j < tips.Count; j++)
                {
                    for (int k = j + 1; k < tips.Count; k++)
                    {
                        // 1. Знаходимо точки перетину трьох сфер
                        var pts = SphereIntersectionUtils.IntersectThreeSpheres(tips[i], tips[j], tips[k], radius);
                        int[] indices = { i, j, k };

                        foreach (var pt in pts)
                        {
                            // 2. Перевірка: чи не потрапляє якась інша верхівка всередину сфери
                            if (SphereUtils.IsTipInsideSphere(pt, radius, tips, indices)) continue;

                            // 3. Перевірка: чи вже не стоїть сфера/mesh у цій точці
                            if (SphereUtils.IsSphereAlreadyPlaced(doc, pt, familyName, tolerance)) continue;

                            // 4. Вибираємо верхню точку, якщо є дві
                            XYZ chosen = pts.Count == 2
                                ? pts.OrderByDescending(p => p.Z).First()
                                : pt;

                            // 5. Вставляємо LP_Mash на три вершини (за год. стрілкою)
                            using (Transaction t = new Transaction(doc, "Place LP_Mash"))
                            {
                                t.Start();
                                FamilyUtils.PlaceMash(doc, tips[i], tips[j], tips[k], radius);
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

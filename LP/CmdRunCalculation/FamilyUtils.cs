using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LP
{
    /// <summary>
    /// Робота з сімействами (вставка інстансів LP_Mesh).
    /// </summary>
    public static class FamilyUtils
    {
        private const string MeshFamilyName = "LP_Mesh";

        /// <summary>
        /// Вставка інстансу LP_Mesh (3 adaptive points) з вибором типу за параметром LP_Sphere_Radius.
        /// </summary>
        /// <param name="doc">Документ Revit</param>
        /// <param name="sortedTriplet">Список трьох точок, відсортованих за годинниковою стрілкою</param>
        /// <param name="radius">Радіус для вибору типу сімейства</param>
        /// <returns>Вставлений FamilyInstance</returns>
        public static FamilyInstance PlaceMash(Document doc, List<XYZ> sortedTriplet, double radius)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (sortedTriplet == null || sortedTriplet.Count != 3)
                throw new ArgumentException("Треба передати рівно 3 точки.");

            // 1. Знаходимо символ LP_Mesh з потрібним радіусом
            FamilySymbol symbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs =>
                {
                    Parameter p = fs.LookupParameter("LP_Sphere_Radius");
                    return fs.Family.Name == MeshFamilyName &&
                           p != null &&
                           Math.Abs(p.AsDouble() - radius) < 1e-6;
                });

            if (symbol == null)
                throw new InvalidOperationException(
                    $"Не знайдено тип сімейства {MeshFamilyName} з LP_Sphere_Radius = {radius}."
                );

            if (!symbol.IsActive) symbol.Activate();

            // 2. Створюємо адаптивний компонент
            FamilyInstance fi = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, symbol);

            // 3. Отримуємо adaptive points
            IList<ElementId> placePointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(fi);
            if (placePointIds.Count != 3)
                throw new InvalidOperationException($"{MeshFamilyName} повинно мати рівно 3 adaptive points.");

            // 4. Присвоюємо координати трьох точок (відсортованих за годинниковою стрілкою)
            for (int i = 0; i < 3; i++)
            {
                ReferencePoint rp = doc.GetElement(placePointIds[i]) as ReferencePoint;
                if (rp != null) rp.Position = sortedTriplet[i];
            }

            return fi;
        }

        /// <summary>
        /// Сортування трьох точок за годинниковою стрілкою у площині трикутника.
        /// </summary>
        private static List<XYZ> SortClockwise(XYZ a, XYZ b, XYZ c)
        {
            var pts = new List<XYZ> { a, b, c };

            // нормаль до площини
            XYZ normal = (b - a).CrossProduct(c - a).Normalize();

            // локальна система координат
            XYZ xAxis = (b - a).Normalize();
            XYZ yAxis = normal.CrossProduct(xAxis);

            // кути для кожної точки
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

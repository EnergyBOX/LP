using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
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
        /// Використовує AdaptiveComponentFamilyUtils для перевірки adaptive points.
        /// </summary>
        /// <param name="doc">Документ Revit</param>
        /// <param name="sortedTriplet">Список трьох точок, відсортованих за годинниковою стрілкою</param>
        /// <param name="radius">Радіус для вибору типу сімейства</param>
        /// <returns>Вставлений FamilyInstance</returns>
        public static FamilyInstance PlaceMash(Document doc, List<XYZ> sortedTriplet, double radius)
        {
            TaskDialog.Show("Log", "1️⃣ Початок PlaceMash");

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
            {
                TaskDialog.Show("Error", $"Не знайдено тип сімейства {MeshFamilyName} з LP_Sphere_Radius = {radius}.");
                throw new InvalidOperationException(
                    $"Не знайдено тип сімейства {MeshFamilyName} з LP_Sphere_Radius = {radius}."
                );
            }

            TaskDialog.Show("Log", "2️⃣ FamilySymbol знайдено");

            // 2. Перевіряємо, що сімейство адаптивне
            if (!AdaptiveComponentFamilyUtils.IsAdaptiveComponentFamily(symbol.Family))
            {
                TaskDialog.Show("Error", $"{MeshFamilyName} не є Adaptive Component Family.");
                throw new InvalidOperationException($"{MeshFamilyName} не є Adaptive Component Family.");
            }

            TaskDialog.Show("Log", "3️⃣ Сімейство є Adaptive Component");

            // 3. Активуємо символ, якщо неактивний
            if (!symbol.IsActive)
            {
                symbol.Activate();
                doc.Regenerate();
                TaskDialog.Show("Log", "4️⃣ FamilySymbol активовано та регенеровано");
            }

            // 4. Створюємо адаптивний компонент
            FamilyInstance fi = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, symbol);
            doc.Regenerate();
            TaskDialog.Show("Log", "5️⃣ Адаптивний компонент створено");

            // 5. Отримуємо adaptive placement points екземпляра
            IList<ElementId> placePointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(fi);

            // 6. Перевірка кількості adaptive points
            int expectedCount = AdaptiveComponentFamilyUtils.GetNumberOfPlacementPoints(symbol.Family);
            if (placePointIds.Count != expectedCount)
            {
                TaskDialog.Show("Error", $"Очікувалось {expectedCount} adaptive points, знайдено {placePointIds.Count}");
                throw new InvalidOperationException($"Очікувалось {expectedCount} adaptive points, знайдено {placePointIds.Count}");
            }

            TaskDialog.Show("Log", "6️⃣ Кількість adaptive points перевірено");

            // 7. Перевірка типу кожної точки (необов'язково)
            for (int i = 0; i < placePointIds.Count; i++)
            {
                ReferencePoint rp = doc.GetElement(placePointIds[i]) as ReferencePoint;
                if (rp == null)
                    throw new InvalidOperationException($"Adaptive point {i} не знайдено!");

                // Не викликаємо IsAdaptivePlacementPoint, бо placePointIds вже Placement Points
            }


            TaskDialog.Show("Log", "7️⃣ Тип кожної adaptive point перевірено");

            // 8. Присвоюємо координати adaptive points
            for (int i = 0; i < sortedTriplet.Count; i++)
            {
                ReferencePoint rp = doc.GetElement(placePointIds[i]) as ReferencePoint;
                if (rp == null)
                {
                    TaskDialog.Show("Error", $"Adaptive point {i} не знайдено!");
                    throw new InvalidOperationException($"Adaptive point {i} не знайдено!");
                }

                rp.Position = sortedTriplet[i];

                // Перевірка: координата фактично встановлена
                if (!rp.Position.IsAlmostEqualTo(sortedTriplet[i]))
                {
                    TaskDialog.Show("Error", $"Adaptive point {i} не встановився на задану позицію!");
                    throw new InvalidOperationException($"Adaptive point {i} не встановився на задану позицію!");
                }
            }

            doc.Regenerate(); // фінальна регенерація після присвоєння координат
            TaskDialog.Show("Log", "8️⃣ Координати adaptive points присвоєно та регенеровано");

            return fi;
        }
    }
}

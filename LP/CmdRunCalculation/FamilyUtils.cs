using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LP
{
    public static class FamilyUtils
    {
        private const string MeshFamilyName = "LP_Mesh";

        /// <summary>
        /// Вставка інстансу LP_Mesh та повернення координат для логування.
        /// </summary>
        /// <param name="doc">Документ Revit</param>
        /// <param name="sortedTriplet">Список трьох точок, відсортованих за годинниковою стрілкою</param>
        /// <param name="radius">Радіус для вибору типу сімейства</param>
        /// <returns>Кортеж: FamilyInstance та лог координат (запропоновані та фактичні)</returns>
        public static (FamilyInstance instance, List<(XYZ proposed, XYZ actual)> pointsLog) PlaceMash(Document doc, List<XYZ> sortedTriplet, double radius)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (sortedTriplet == null || sortedTriplet.Count != 3)
                throw new ArgumentException("Треба передати рівно 3 точки.");

            // 1. Знаходимо символ LP_Mesh
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
                throw new InvalidOperationException($"Не знайдено тип сімейства {MeshFamilyName} з LP_Sphere_Radius = {radius}.");

            if (!AdaptiveComponentFamilyUtils.IsAdaptiveComponentFamily(symbol.Family))
                throw new InvalidOperationException($"{MeshFamilyName} не є Adaptive Component Family.");

            if (!symbol.IsActive)
            {
                symbol.Activate();
                doc.Regenerate();
            }

            // 2. Створюємо адаптивний компонент
            FamilyInstance fi = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, symbol);
            doc.Regenerate();

            // 3. Отримуємо adaptive placement points
            IList<ElementId> placePointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(fi);
            int expectedCount = AdaptiveComponentFamilyUtils.GetNumberOfPlacementPoints(symbol.Family);
            if (placePointIds.Count != expectedCount)
                throw new InvalidOperationException($"Очікувалось {expectedCount} adaptive points, знайдено {placePointIds.Count}");

            // 4. Присвоюємо координати та збираємо лог
            var pointsLog = new List<(XYZ proposed, XYZ actual)>();
            for (int i = 0; i < sortedTriplet.Count; i++)
            {
                ReferencePoint rp = doc.GetElement(placePointIds[i]) as ReferencePoint;
                if (rp == null)
                    throw new InvalidOperationException($"Adaptive point {i} не знайдено!");

                XYZ proposed = sortedTriplet[i];
                rp.Position = proposed;
                doc.Regenerate();
                XYZ actual = rp.Position;

                pointsLog.Add((proposed, actual));
            }

            return (fi, pointsLog);
        }
    }
}

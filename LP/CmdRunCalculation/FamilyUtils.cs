using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Linq;

namespace LP
{
    public static class FamilyUtils
    {
        private const string FamilyName = "LP_Sphere";

        /// <summary>
        /// Вставляє сімейство в документ. Не відкриває власну транзакцію.
        /// Транзакцію слід відкривати зовні (наприклад, для циклу всіх точок).
        /// </summary>
        public static FamilyInstance PlaceSphereVoid(Document doc, XYZ location, double radius)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (location == null) throw new ArgumentNullException(nameof(location));

            // Знаходимо символ сімейства з потрібним радіусом
            FamilySymbol symbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs =>
                {
                    Parameter p = fs.LookupParameter("LP_Sphere_Radius");
                    if (p == null) return false;
                    return Math.Abs(p.AsDouble() - radius) < 1e-6;
                });

            if (symbol == null)
                throw new InvalidOperationException($"Не знайдено тип сімейства LP_Sphere з радіусом {radius}.");

            // Активуємо символ, якщо він не активний
            if (!symbol.IsActive)
                symbol.Activate();

            // Вставляємо інстанс сімейства (без транзакції)
            return doc.Create.NewFamilyInstance(location, symbol, StructuralType.NonStructural);
        }
    }
}

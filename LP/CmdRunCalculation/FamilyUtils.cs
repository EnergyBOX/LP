using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Linq;

namespace LP
{
    /// <summary>
    /// Робота з сімействами (вставка інстансів).
    /// </summary>
    public static class FamilyUtils
    {
        private const string FamilyName = "LP_Sphere";

        public static FamilyInstance PlaceSphereVoid(Document doc, XYZ location, double radius)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (location == null) throw new ArgumentNullException(nameof(location));

            FamilySymbol symbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs =>
                {
                    Parameter p = fs.LookupParameter("LP_Sphere_Radius");
                    return p != null && Math.Abs(p.AsDouble() - radius) < 1e-6;
                });

            if (symbol == null)
                throw new InvalidOperationException($"Не знайдено тип сімейства LP_Sphere з радіусом {radius}.");

            if (!symbol.IsActive) symbol.Activate();

            return doc.Create.NewFamilyInstance(location, symbol, StructuralType.NonStructural);
        }
    }
}

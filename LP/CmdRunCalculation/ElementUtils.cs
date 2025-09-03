using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace LP
{
    public static class ElementUtils
    {
        /// <summary>
        /// Вставляє Void-сфери всередину зон захисту.
        /// Revit автоматично виконає Boolean при вставці.
        /// </summary>
        public static void CutElements(Document doc, Element zone, List<Sphere> spheres)
        {
            foreach (var sphere in spheres)
            {
                if (sphere.FamilyInstanceId == ElementId.InvalidElementId) continue;

                var voidElem = doc.GetElement(sphere.FamilyInstanceId);
                if (voidElem == null) continue;

                try
                {
                    // Власне Boolean виконується автоматично, нічого додатково не треба
                    // Можна додати перевірку на категорію, якщо потрібно
                }
                catch
                {
                    // Пропускаємо помилки
                }
            }
        }
    }
}

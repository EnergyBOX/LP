using Autodesk.Revit.DB;
using System.Linq;

namespace LP
{
    /// <summary>
    /// Сервіс для очистки результатів розрахунку LP_Mesh.
    /// Видаляє екземпляри з параметром LP_Is_Mesh = Yes.
    /// </summary>
    public static class CleaningResultsService
    {
        public class CleaningResult
        {
            public int TotalFound { get; set; }
            public int TotalDeleted { get; set; }
        }

        /// <summary>
        /// Видаляє всі незакріплені LP_Mesh з LP_Is_Mesh = Yes.
        /// </summary>
        public static CleaningResult Clear(Document doc)
        {
            var result = new CleaningResult();

            // 1. Збираємо всі екземпляри LP_Mesh
            var meshes = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(fi => fi.Symbol.Family.Name == "LP_Mesh")
                .ToList();

            result.TotalFound = meshes.Count;

            using (Transaction tx = new Transaction(doc, "Clear LP_Mesh Results"))
            {
                tx.Start();

                foreach (var mesh in meshes)
                {
                    // 2. Перевірка параметра LP_Is_Mesh (Yes/No → Int)
                    int? isMeshValue = mesh.LookupParameter("LP_Is_Mesh")?.AsInteger();
                    if (isMeshValue != 1) continue;

                    // 3. Видаляємо лише незакріплені
                    if (!mesh.Pinned)
                    {
                        doc.Delete(mesh.Id);
                        result.TotalDeleted++;
                    }
                }

                tx.Commit();
            }

            return result;
        }
    }
}

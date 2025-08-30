using Autodesk.Revit.DB;
using System.IO;
using System.Reflection;

namespace LP
{
    public static class FamilyLoader
    {
        /// <summary>
        /// Вивантажує сімейство LP_Sphere.rfa з ресурсу в TEMP і завантажує в проект.
        /// </summary>
        public static Family LoadLPSphere(Document doc)
        {
            string resourceName = "LP.Resources.LP_Sphere.rfa"; // 👈 namespace + шлях у проекті
            string tempFile = Path.Combine(Path.GetTempPath(), "LP_Sphere.rfa");

            // Скопіювати ресурс у тимчасовий файл
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new FileNotFoundException($"Embedded resource not found: {resourceName}");

                using (FileStream fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fileStream);
                }
            }

            // Завантажити у Revit
            using (Transaction t = new Transaction(doc, "Load LP_Sphere"))
            {
                t.Start();
                if (!doc.LoadFamily(tempFile, out Family family))
                    throw new IOException("Failed to load LP_Sphere family into project.");
                t.Commit();
                return family;
            }
        }
    }
}

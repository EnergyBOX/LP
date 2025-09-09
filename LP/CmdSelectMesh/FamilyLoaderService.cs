using Autodesk.Revit.DB;
using System.IO;
using System.Reflection;

namespace LP
{
    public static class FamilyLoaderService
    {
        /// <summary>
        /// Вивантажує сімейство LP_Mesh.rfa з ресурсу в TEMP і завантажує в проект.
        /// </summary>
        public static Family LoadLPMesh(Document doc)
        {
            string resourceName = "LP.Resources.LP_Mesh.rfa"; // 👈 правильний namespace + шлях
            string tempFile = Path.Combine(Path.GetTempPath(), "LP_Mesh.rfa");

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new FileNotFoundException($"Embedded resource not found: {resourceName}");

                using (FileStream fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fileStream);
                }
            }

            using (Transaction t = new Transaction(doc, "Load LP_Mesh"))
            {
                t.Start();
                if (!doc.LoadFamily(tempFile, out Family family))
                    throw new IOException("Failed to load LP_Mesh family into project.");
                t.Commit();
                return family;
            }
        }
    }
}

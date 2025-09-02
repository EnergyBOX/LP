using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace LP
{
    public static class DirectShapeUtils
    {
        /// <summary>
        /// Створює/оновлює DirectShape з новим Solid на місці зони (або, як варіант, створює дочірній елемент).
        /// </summary>
        public static void ReplaceGeometry(Document doc, Element source, Solid newSolid, string appId)
        {
            var ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
            ds.ApplicationId = "LP";
            ds.ApplicationDataId = appId + "_" + source.Id.IntegerValue;

            var geom = new List<GeometryObject> { newSolid };
            ds.SetShape(geom);

            // За бажання — скопіювати параметри, марку тощо із source
            // або видалити/сховати початковий елемент:
            // doc.Delete(source.Id); // обережно 🙂
        }
    }
}

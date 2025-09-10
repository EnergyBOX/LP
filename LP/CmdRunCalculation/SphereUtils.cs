//using Autodesk.Revit.DB;
//using System.Collections.Generic;
//using System.Linq;

//namespace LP
//{
//    /// <summary>
//    /// Допоміжні методи для перевірки сфер.
//    /// </summary>
//    public static class SphereUtils
//    {
//        public static bool IsTipInsideSphere(XYZ center, double radius, List<XYZ> tips, int[] exclude)
//        {
//            foreach (int i in Enumerable.Range(0, tips.Count))
//            {
//                if (exclude.Contains(i)) continue;
//                if (center.DistanceTo(tips[i]) < radius) return true;
//            }
//            return false;
//        }

//        public static bool IsSphereAlreadyPlaced(Document doc, XYZ location, string familyName, double tol)
//        {
//            var existingSpheres = new FilteredElementCollector(doc)
//                .OfClass(typeof(FamilyInstance))
//                .Cast<FamilyInstance>()
//                .Where(fi => fi.Symbol.Family.Name == familyName);

//            foreach (var fi in existingSpheres)
//            {
//                if (fi.Location is LocationPoint lp && lp.Point.DistanceTo(location) < tol)
//                    return true;
//            }
//            return false;
//        }
//    }
//}

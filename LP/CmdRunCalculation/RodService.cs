using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace LP
{
    /// <summary>
    /// Сервіс для роботи з блискавкоприймачами.
    /// </summary>
    public static class RodService
    {
        public static List<Element> GetLightningRods(Document doc, string paramName)
        {
            return Query.FindElementsWithYesParam(doc, paramName);
        }

        public static List<XYZ> GetTips(Document doc, List<Element> rods)
        {
            return GeometryUtils.ComputeTipPoints(doc, rods);
        }
    }
}

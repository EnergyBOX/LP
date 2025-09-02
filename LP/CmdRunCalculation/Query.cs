using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace LP
{
    public static class Query
    {
        public static List<Element> FindElementsWithYesParam(Document doc, string yesParamName)
        {
            var collector = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .ToElements();

            var result = new List<Element>();
            foreach (var e in collector)
            {
                var p = e.LookupParameter(yesParamName);
                if (p == null) continue;

                if (p.StorageType == StorageType.Integer)
                {
                    // Yes/No параметр: 1 = Yes
                    if (p.AsInteger() == 1) result.Add(e);
                }
                else if (p.StorageType == StorageType.String)
                {
                    var s = (p.AsString() ?? "").Trim();
                    if (string.Equals(s, "Yes", System.StringComparison.OrdinalIgnoreCase) ||
                        s == "1" || s == "True" || s == "true")
                        result.Add(e);
                }
            }
            return result;
        }
    }
}

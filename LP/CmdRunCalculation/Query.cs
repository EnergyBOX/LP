using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace LP
{
    public static class Query
    {
        public static List<Element> FindElementsWithYesParam(Document doc, string paramName)
        {
            return new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .ToElements()
                .Where(e => e.LookupParameter(paramName)?.AsInteger() == 1)
                .ToList();
        }
    }
}

using Autodesk.Revit.DB;

namespace LP
{
    public static class GlobalParams
    {
        public static double GetGlobalDouble(Document doc, string name)
        {
            var gp = GlobalParametersManager.FindByName(doc, name);
            if (gp == ElementId.InvalidElementId) return 0.0;

            var paramElem = doc.GetElement(gp) as GlobalParameter;
            if (paramElem == null) return 0.0;

            var v = paramElem.GetValue() as DoubleParameterValue;
            return v?.Value ?? 0.0; // В Revit — у внутрішніх одиницях (фути)
        }
    }
}

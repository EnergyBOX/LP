//using Autodesk.Revit.DB;

//namespace LP
//{
//    public static class GlobalParams
//    {
//        public static double GetGlobalDouble(Document doc, string name)
//        {
//            ElementId gpId = GlobalParametersManager.FindByName(doc, name);
//            if (gpId == ElementId.InvalidElementId) return 0.0;

//            var paramElem = doc.GetElement(gpId) as GlobalParameter;
//            if (paramElem == null) return 0.0;

//            var value = paramElem.GetValue() as DoubleParameterValue;
//            return value?.Value ?? 0.0;
//        }

//        public static void SetGlobalDouble(Document doc, string name, double value)
//        {
//            ElementId gpId = GlobalParametersManager.FindByName(doc, name);
//            GlobalParameter gp = null;

//            using (Transaction tx = new Transaction(doc, "Set Global Parameter"))
//            {
//                tx.Start();

//                if (gpId == ElementId.InvalidElementId)
//                {
//                    gp = GlobalParameter.Create(doc, name, SpecTypeId.Length);
//                }
//                else
//                {
//                    gp = doc.GetElement(gpId) as GlobalParameter;
//                }

//                if (gp != null) gp.SetValue(new DoubleParameterValue(value));
//                tx.Commit();
//            }
//        }
//    }
//}

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace LP
{
    [Transaction(TransactionMode.Manual)]
    public class CmdSelectSphere : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                string familyName = "LP_Sphere";

                // 1. Перевіряємо чи сімейство є у проекті
                Family family = new FilteredElementCollector(doc)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .FirstOrDefault(f => f.Name == familyName);

                if (family == null)
                {
                    family = FamilyLoader.LoadLPSphere(doc); // ⚠️ Реалізуй метод FamilyLoader.LoadLPSphere
                }

                // 2. Отримуємо всі типи сімейства
                var symbols = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .Where(fs => fs.Family.Name == familyName)
                    .OrderBy(fs => fs.Name)
                    .ToList();

                if (!symbols.Any())
                {
                    TaskDialog.Show("Error", "No LP_Sphere types found.");
                    return Result.Failed;
                }

                // 3. Формуємо TaskDialog з CommandLinks
                TaskDialog td = new TaskDialog("Select Sphere Radius")
                {
                    MainInstruction = "Select sphere radius to use in calculations",
                    TitleAutoPrefix = false,
                };

                int id = 100;
                var buttonMap = new Dictionary<int, FamilySymbol>();

                foreach (var fs in symbols)
                {
                    Parameter radiusParam = fs.LookupParameter("LP_Sphere_Radius");
                    double radiusMm = radiusParam != null ? radiusParam.AsDouble() * 304.8 : 0;
                    double radiusM = radiusMm / 1000.0;

                    td.AddCommandLink((TaskDialogCommandLinkId)id, $"{fs.Name}", $"Radius = {radiusM} m");
                    buttonMap[id] = fs;
                    id++;
                }

                TaskDialogResult result = td.Show();

                if (result == TaskDialogResult.Cancel)
                {
                    TaskDialog.Show("Info", "Operation cancelled.");
                    return Result.Cancelled;
                }

                if (!buttonMap.ContainsKey((int)result))
                {
                    TaskDialog.Show("Error", "Invalid selection.");
                    return Result.Failed;
                }

                FamilySymbol selectedSymbol = buttonMap[(int)result];
                Parameter selectedRadius = selectedSymbol.LookupParameter("LP_Sphere_Radius");
                double radiusFeet = selectedRadius.AsDouble(); // внутрішні одиниці Revit — фути
                double radiusMFinal = radiusFeet * 0.3048;

                TaskDialog.Show("Result", $"Selected: {selectedSymbol.Name}\nRadius = {radiusMFinal} m");

                // 4. Записуємо радіус у глобальний параметр LP_Radius
                SetGlobalParameter(doc, "LP_Radius", radiusFeet);

                return Result.Succeeded;
            }
            catch (System.Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private void SetGlobalParameter(Document doc, string paramName, double valueInFeet)
        {
            GlobalParameter gp = new FilteredElementCollector(doc)
                .OfClass(typeof(GlobalParameter))
                .Cast<GlobalParameter>()
                .FirstOrDefault(p => p.Name == paramName);

            using (Transaction tx = new Transaction(doc, "Set Global Parameter"))
            {
                tx.Start();

                if (gp == null)
                {
                    // ✅ У Revit 2024 використовуємо ForgeTypeId (SpecTypeId.Length)
                    gp = GlobalParameter.Create(doc, paramName, SpecTypeId.Length);
                }

                gp.SetValue(new DoubleParameterValue(valueInFeet));
                tx.Commit();
            }
        }
    }
}

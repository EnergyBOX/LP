using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace LP
{
    [Transaction(TransactionMode.Manual)]
    public class CmdSelectMesh : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                string familyName = "LP_Mesh";

                // 1. Перевіряємо чи сімейство є у проекті
                Family family = new FilteredElementCollector(doc)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .FirstOrDefault(f => f.Name == familyName);

                if (family == null)
                {
                    family = FamilyLoaderService.LoadLPMesh(doc);
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
                    TaskDialog.Show("Error", "No LP_Mesh types found.");
                    return Result.Failed;
                }

                // 3. Формуємо TaskDialog для вибору типу Mesh
                TaskDialog td = new TaskDialog("Select LP_Mesh Type")
                {
                    MainInstruction = "Select LP_Mesh type to use in calculations",
                    TitleAutoPrefix = false,
                };

                int id = 100;
                var buttonMap = new Dictionary<int, FamilySymbol>();

                foreach (var fs in symbols)
                {
                    td.AddCommandLink((TaskDialogCommandLinkId)id, $"{fs.Name}");
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

                TaskDialog.Show("Result", $"Selected LP_Mesh type: {selectedSymbol.Name}");

                // Використовуємо Id.Value (Revit 2024)
                SetGlobalParameter(doc, "LP_Mesh_Type", selectedSymbol.Id.Value);

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
                    gp = GlobalParameter.Create(doc, paramName, SpecTypeId.Length); // можливо, замінити на ForgeTypeId.Integer
                }

                gp.SetValue(new DoubleParameterValue(valueInFeet));

                tx.Commit();
            }
        }
    }
}

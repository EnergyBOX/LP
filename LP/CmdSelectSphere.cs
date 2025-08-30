using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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
                string familyPath = @"C:\Families\LP_Sphere.rfa"; // ⚠️ зміни шлях на свій

                // 1. Перевіряємо чи сімейство є у проекті
                Family family = new FilteredElementCollector(doc)
                                    .OfClass(typeof(Family))
                                    .Cast<Family>()
                                    .FirstOrDefault(f => f.Name == familyName);

                if (family == null)
                {
                    if (!File.Exists(familyPath))
                    {
                        TaskDialog.Show("Error", $"Family file not found: {familyPath}");
                        return Result.Failed;
                    }

                    using (Transaction t = new Transaction(doc, "Load LP_Sphere"))
                    {
                        t.Start();
                        doc.LoadFamily(familyPath, out family);
                        t.Commit();
                    }
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

                int id = 100; // будемо задавати свої ID для кнопок
                var buttonMap = new Dictionary<int, FamilySymbol>();

                foreach (var fs in symbols)
                {
                    Parameter radiusParam = fs.LookupParameter("LP_Sphere_Radius");
                    double radiusMm = radiusParam != null ? radiusParam.AsDouble() * 304.8 : 0; // ft -> мм
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
                double radiusMmFinal = selectedRadius.AsDouble() * 304.8; // ft → мм
                double radiusMFinal = radiusMmFinal / 1000.0;

                TaskDialog.Show("Result", $"Selected: {selectedSymbol.Name}\nRadius = {radiusMFinal} m");

                // ✅ Тут можна зберегти selectedSymbol або radiusMFinal у static-змінну для подальших розрахунків

                return Result.Succeeded;
            }
            catch (System.Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}

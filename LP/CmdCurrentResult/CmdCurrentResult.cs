using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using System.Text;

namespace LP
{
    [Transaction(TransactionMode.Manual)]
    public class CmdCurrentResult : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                var allElements = new FilteredElementCollector(doc)
                                    .WhereElementIsNotElementType()
                                    .ToElements();

                StringBuilder mainContent = new StringBuilder();

                // ===== Lightning Rods =====
                var rodsYes = allElements
                    .Where(el => el.LookupParameter("LP_Is_LightningRod")?.AsInteger() == 1)
                    .ToList();

                if (rodsYes.Any())
                {
                    mainContent.AppendLine($"=== Lightning Rods ({rodsYes.Count}) ===");
                    foreach (var el in rodsYes)
                    {
                        string typeName = el.Name;
                        string pos = el.Location is LocationPoint lp ?
                            $"X={lp.Point.X:F2}, Y={lp.Point.Y:F2}, Z={lp.Point.Z:F2}" : "N/A";
                        mainContent.AppendLine($"  Type: {typeName} | Id: {el.Id} | Position: {pos}");
                    }
                    mainContent.AppendLine();
                }

                // ===== Protected Zones =====
                var zonesYes = allElements
                    .Where(el => el.LookupParameter("LP_Is_ProtectedZone")?.AsInteger() == 1)
                    .ToList();

                if (zonesYes.Any())
                {
                    mainContent.AppendLine($"=== Protected Zones ({zonesYes.Count}) ===");
                    foreach (var el in zonesYes)
                    {
                        string typeName = el.Name;
                        string pos = el.Location is LocationPoint lp ?
                            $"X={lp.Point.X:F2}, Y={lp.Point.Y:F2}, Z={lp.Point.Z:F2}" : "N/A";
                        mainContent.AppendLine($"  Type: {typeName} | Id: {el.Id} | Position: {pos}");
                    }
                    mainContent.AppendLine();
                }

                // ===== Spheres =====
                var spheresYes = allElements
                    .Where(el => el.LookupParameter("LP_Is_SpereThatCutsOff")?.AsInteger() == 1)
                    .Cast<FamilyInstance>()
                    .ToList();

                if (spheresYes.Any())
                {
                    mainContent.AppendLine($"=== Spheres ({spheresYes.Count}) ===");
                    foreach (var s in spheresYes)
                    {
                        FamilySymbol symbol = s.Symbol;
                        string pos = s.Location is LocationPoint lp ?
                            $"X={lp.Point.X:F2}, Y={lp.Point.Y:F2}, Z={lp.Point.Z:F2}" : "N/A";

                        mainContent.AppendLine($"  Type: {symbol.Name} | Id: {s.Id} | Position: {pos}");
                    }
                    mainContent.AppendLine();
                }

                if (mainContent.Length == 0)
                {
                    mainContent.AppendLine("No elements with 'Yes' values found.");
                }

                // ===== Вивід TaskDialog =====
                TaskDialog td = new TaskDialog("Current Lightning Protection Status")
                {
                    MainInstruction = "Elements considered in lightning protection calculations by LP plugin",
                    MainContent = mainContent.ToString(),
                    TitleAutoPrefix = false,
                    CommonButtons = TaskDialogCommonButtons.Close
                };

                td.Show();

                // ===== Виділення елементів у активному виді =====
                var highlightElements = rodsYes.Select(e => e.Id)
                    .Concat(zonesYes.Select(e => e.Id))
                    .Concat(spheresYes.Select(e => e.Id))
                    .ToList();

                if (highlightElements.Any())
                {
                    uiDoc.Selection.SetElementIds(highlightElements);
                }

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

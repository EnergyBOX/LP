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
                                    .OfType<FamilyInstance>()
                                    .ToList();

                StringBuilder mainContent = new StringBuilder();

                // ===== Lightning Rods =====
                var rodsYes = allElements
                    .Where(el => el.LookupParameter("LP_Is_LightningRod")?.AsInteger() == 1)
                    .ToList();

                if (rodsYes.Any())
                {
                    var pinned = rodsYes.Where(e => e.Pinned).OrderBy(e => e.Symbol?.Name).ToList();
                    var unpinned = rodsYes.Where(e => !e.Pinned).OrderBy(e => e.Symbol?.Name).ToList();

                    mainContent.AppendLine($"=== Lightning Rods ({rodsYes.Count}) ===");

                    if (pinned.Any())
                    {
                        mainContent.AppendLine($"-- Pinned ({pinned.Count}) --");
                        foreach (var el in pinned)
                        {
                            string typeName = el.Symbol?.Name ?? el.Name;
                            mainContent.AppendLine($"  Type: {typeName} | Id: {el.Id}");
                        }
                    }

                    if (unpinned.Any())
                    {
                        mainContent.AppendLine($"-- Unpinned ({unpinned.Count}) --");
                        foreach (var el in unpinned)
                        {
                            string typeName = el.Symbol?.Name ?? el.Name;
                            mainContent.AppendLine($"  Type: {typeName} | Id: {el.Id}");
                        }
                    }

                    mainContent.AppendLine();
                }

                // ===== Meshes =====
                var meshesYes = allElements
                    .Where(el => el.LookupParameter("LP_Is_Mesh")?.AsInteger() == 1)
                    .ToList();

                if (meshesYes.Any())
                {
                    var pinned = meshesYes.Where(e => e.Pinned).OrderBy(e => e.Symbol?.Name).ToList();
                    var unpinned = meshesYes.Where(e => !e.Pinned).OrderBy(e => e.Symbol?.Name).ToList();

                    mainContent.AppendLine($"=== Meshes ({meshesYes.Count}) ===");

                    if (pinned.Any())
                    {
                        mainContent.AppendLine($"-- Pinned ({pinned.Count}) --");
                        foreach (var el in pinned)
                        {
                            string typeName = el.Symbol?.Name ?? el.Name;
                            mainContent.AppendLine($"  Type: {typeName} | Id: {el.Id}");
                        }
                    }

                    if (unpinned.Any())
                    {
                        mainContent.AppendLine($"-- Unpinned ({unpinned.Count}) --");
                        foreach (var el in unpinned)
                        {
                            string typeName = el.Symbol?.Name ?? el.Name;
                            mainContent.AppendLine($"  Type: {typeName} | Id: {el.Id}");
                        }
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
                var highlightElements = rodsYes.Concat(meshesYes).Select(e => e.Id).ToList();

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

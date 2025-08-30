using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;

namespace LP
{
    [Transaction(TransactionMode.Manual)]
    public class CmdIncludeRod : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                var selectedElements = uiDoc.Selection.GetElementIds()
                                            .Select(id => doc.GetElement(id))
                                            .Where(el => el != null)
                                            .ToList();

                if (!selectedElements.Any())
                {
                    TaskDialog.Show("Info", "No elements selected.");
                    return Result.Succeeded;
                }

                var defs = SharedParameters.GetOrCreate(doc.Application);
                var lightningDef = defs.FirstOrDefault(d => d.Name == "LP_Is_LightningRod");
                if (lightningDef == null)
                {
                    TaskDialog.Show("Error", "Shared parameter not found.");
                    return Result.Failed;
                }

                CategorySet categories = doc.Application.Create.NewCategorySet();
                foreach (var el in selectedElements)
                    if (el.Category != null)
                        categories.Insert(el.Category);

                using (Transaction t = new Transaction(doc, "Bind LP_Is_LightningRod"))
                {
                    t.Start();
                    SharedParameters.BindToCategories(doc, new[] { lightningDef }, categories);
                    t.Commit();
                }

                using (Transaction t = new Transaction(doc, "Set LP_Is_LightningRod"))
                {
                    t.Start();
                    foreach (var el in selectedElements)
                    {
                        Parameter p = el.LookupParameter("LP_Is_LightningRod");
                        if (p != null && !p.IsReadOnly)
                            p.Set(1);
                    }
                    t.Commit();
                }

                TaskDialog.Show("Info", "LP_Is_LightningRod applied to selected elements.");
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

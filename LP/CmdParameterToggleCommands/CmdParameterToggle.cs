using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;

namespace LP
{
    public abstract class CmdParameterToggle : IExternalCommand
    {
        protected abstract string ParameterName { get; }
        protected abstract int ParameterValue { get; }

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

                var defs = SharedParametersService.GetOrCreate(doc.Application);
                var targetDef = defs.FirstOrDefault(d => d.Name == ParameterName);
                if (targetDef == null)
                {
                    TaskDialog.Show("Error", $"Shared parameter {ParameterName} not found.");
                    return Result.Failed;
                }

                CategorySet categories = doc.Application.Create.NewCategorySet();
                foreach (var el in selectedElements)
                    if (el.Category != null)
                        categories.Insert(el.Category);

                using (Transaction t = new Transaction(doc, $"Bind {ParameterName}"))
                {
                    t.Start();
                    SharedParametersService.BindToCategories(doc, new[] { targetDef }, categories);
                    t.Commit();
                }

                using (Transaction t = new Transaction(doc, $"Set {ParameterName}"))
                {
                    t.Start();
                    foreach (var el in selectedElements)
                    {
                        Parameter p = el.LookupParameter(ParameterName);
                        if (p != null && !p.IsReadOnly)
                            p.Set(ParameterValue);
                    }
                    t.Commit();
                }

                TaskDialog.Show("Info", $"{ParameterName} set to {ParameterValue}.");
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

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LP
{
    [Transaction(TransactionMode.Manual)]
    public class CmdCutProtectedZones : IExternalCommand
    {
        private const string ParamIsProtectedZone = "LP_Is_ProtectedZone";
        private const string ParamIsSphereThatCutsOff = "LP_Is_SphereThatCutsOff";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                using (TransactionGroup tg = new TransactionGroup(doc, "LP | Cut Protected Zones"))
                {
                    tg.Start();

                    // 1. Знаходимо усі зони
                    var protectedZones = new FilteredElementCollector(doc)
                        .OfClass(typeof(FamilyInstance))
                        .Cast<FamilyInstance>()
                        .Where(f => f.LookupParameter(ParamIsProtectedZone)?.AsInteger() == 1)
                        .ToList();

                    if (protectedZones.Count == 0)
                    {
                        TaskDialog.Show("LP", "Зони для захисту не знайдені.");
                        tg.RollBack();
                        return Result.Succeeded;
                    }

                    // 2. Знаходимо всі сфери-обрізки
                    var cutSpheres = new FilteredElementCollector(doc)
                        .OfClass(typeof(FamilyInstance))
                        .Cast<FamilyInstance>()
                        .Where(f => f.LookupParameter(ParamIsSphereThatCutsOff)?.AsInteger() == 1)
                        .ToList();

                    if (cutSpheres.Count == 0)
                    {
                        TaskDialog.Show("LP", "Сфери для обрізки не знайдені.");
                        tg.RollBack();
                        return Result.Succeeded;
                    }

                    int cutCount = 0;

                    using (Transaction t = new Transaction(doc, "LP | Apply Void Cuts"))
                    {
                        t.Start();

                        foreach (var zone in protectedZones)
                        {
                            foreach (var sphere in cutSpheres)
                            {
                                try
                                {
                                    // Перевірка чи можна зробити обрізку
                                    if (InstanceVoidCutUtils.CanBeCutWithVoid(zone))
                                    {
                                        InstanceVoidCutUtils.AddInstanceVoidCut(doc, zone, sphere);
                                        cutCount++;
                                    }
                                }
                                catch
                                {
                                    // Якщо не вдалося обрізати цю зону цією сферою - пропускаємо
                                    continue;
                                }
                            }
                        }

                        t.Commit();
                    }

                    tg.Assimilate();

                    TaskDialog.Show("LP - Report",
                        $"Зон для обрізки: {protectedZones.Count}\n" +
                        $"Сфер-обрізок: {cutSpheres.Count}\n" +
                        $"Вдалих обрізок: {cutCount}");

                    return Result.Succeeded;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}

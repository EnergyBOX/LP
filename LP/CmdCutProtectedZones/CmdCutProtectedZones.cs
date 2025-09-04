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
        private const string ParamDebugUpdate = "LP_DebugUpdate"; // тимчасовий параметр для примусового оновлення

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

                    // 3. Формуємо список усіх пар zone + sphere для обрізки
                    var pairs = new List<(FamilyInstance zone, FamilyInstance sphere)>();
                    foreach (var zone in protectedZones)
                    {
                        foreach (var sphere in cutSpheres)
                        {
                            if (BoundingBoxesIntersect(zone, sphere))
                                pairs.Add((zone, sphere));
                        }
                    }

                    var pending = pairs.ToList();
                    int maxPasses = 5;
                    var rnd = new Random(12345);
                    int pass = 0;

                    using (Transaction t = new Transaction(doc, "LP | Apply Void Cuts"))
                    {
                        t.Start();

                        while (pending.Count > 0 && pass < maxPasses)
                        {
                            pass++;
                            bool anySuccessThisPass = false;
                            pending = pending.OrderBy(_ => rnd.Next()).ToList();

                            foreach (var pair in pending.ToList())
                            {
                                // Примусово оновлюємо сферу, змінюючи debug-параметр
                                var debugParam = pair.sphere.LookupParameter(ParamDebugUpdate);
                                if (debugParam != null && debugParam.StorageType == StorageType.Integer)
                                {
                                    debugParam.Set((debugParam.AsInteger() + 1) % 1000);
                                }

                                doc.Regenerate(); // перерахунок геометрії

                                if (!InstanceVoidCutUtils.CanBeCutWithVoid(pair.zone))
                                    continue;

                                if (TryAddVoidCutOnce(doc, pair.zone, pair.sphere))
                                {
                                    cutCount++;
                                    pending.Remove(pair);
                                    anySuccessThisPass = true;
                                }
                            }

                            doc.Regenerate();
                            if (!anySuccessThisPass) break;
                        }

                        t.Commit();
                    }

                    tg.Assimilate();

                    TaskDialog.Show("LP - Report",
                        $"Зон для обрізки: {protectedZones.Count}\n" +
                        $"Сфер-обрізок: {cutSpheres.Count}\n" +
                        $"Вдалих обрізок: {cutCount}\n" +
                        $"Не вдалося обрізати: {pending.Count}");

                    return Result.Succeeded;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private bool TryAddVoidCutOnce(Document doc, FamilyInstance zone, FamilyInstance sphere)
        {
            using (var st = new SubTransaction(doc))
            {
                try
                {
                    st.Start();
                    InstanceVoidCutUtils.AddInstanceVoidCut(doc, zone, sphere);
                    st.Commit();
                    return true;
                }
                catch
                {
                    st.RollBack();
                    return false;
                }
            }
        }

        private bool BoundingBoxesIntersect(FamilyInstance fi1, FamilyInstance fi2)
        {
            BoundingBoxXYZ box1 = fi1.get_BoundingBox(null);
            BoundingBoxXYZ box2 = fi2.get_BoundingBox(null);

            if (box1 == null || box2 == null) return false;

            return (box1.Min.X <= box2.Max.X && box1.Max.X >= box2.Min.X) &&
                   (box1.Min.Y <= box2.Max.Y && box1.Max.Y >= box2.Min.Y) &&
                   (box1.Min.Z <= box2.Max.Z && box1.Max.Z >= box2.Min.Z);
        }
    }
}

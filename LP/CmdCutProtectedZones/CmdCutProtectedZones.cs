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
                // 1. Знаходимо усі зони
                var protectedZones = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .Cast<FamilyInstance>()
                    .Where(f => f.LookupParameter(ParamIsProtectedZone)?.AsInteger() == 1)
                    .ToList();

                if (protectedZones.Count == 0)
                {
                    TaskDialog.Show("LP", "Зони для захисту не знайдені.");
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
                    return Result.Succeeded;
                }

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

                int cutCount = 0;
                var rnd = new Random(12345);
                var pending = pairs.ToList();

                while (pending.Count > 0)
                {
                    // Беремо випадковий елемент
                    var pair = pending[rnd.Next(pending.Count)];

                    using (Transaction t = new Transaction(doc, $"LP | Cut {pair.sphere.Id}"))
                    {
                        t.Start();
                        try
                        {
                            if (InstanceVoidCutUtils.CanBeCutWithVoid(pair.zone))
                            {
                                InstanceVoidCutUtils.AddInstanceVoidCut(doc, pair.zone, pair.sphere);
                                cutCount++;
                            }
                        }
                        catch
                        {
                            // якщо не вдалось - пропускаємо
                        }
                        t.Commit();
                    }

                    // прибираємо оброблений елемент зі списку
                    pending.Remove(pair);
                }

                TaskDialog.Show("LP - Report",
                    $"Зон для обрізки: {protectedZones.Count}\n" +
                    $"Сфер-обрізок: {cutSpheres.Count}\n" +
                    $"Вдалих обрізок: {cutCount}");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
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

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LP
{
    [Transaction(TransactionMode.Manual)]
    public class CmdRunCalculation : IExternalCommand
    {
        private const string ParamIsRod = "LP_Is_LightningRod";
        private const string ParamIsZone = "LP_Is_ProtectedZone";
        private const string ParamRadius = "LP_Radius"; // Global Parameter

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                using (TransactionGroup tg = new TransactionGroup(doc, "LP | Perform Calculation"))
                {
                    tg.Start();

                    // 1) Вхідні дані
                    double R = GlobalParams.GetGlobalDouble(doc, ParamRadius);
                    if (R <= 0)
                    {
                        TaskDialog.Show("LP", $"Глобальний параметр \"{ParamRadius}\" не знайдено або <= 0.");
                        tg.RollBack();
                        return Result.Failed;
                    }

                    var rods = Query.FindElementsWithYesParam(doc, ParamIsRod);
                    if (rods.Count == 0)
                    {
                        TaskDialog.Show("LP", "Блискавкоприймачі не знайдені.");
                        tg.RollBack();
                        return Result.Succeeded;
                    }

                    // 2) Верхівки
                    var tips = GeometryUtils.ComputeTipPoints(doc, rods);
                    if (tips.Count == 0)
                    {
                        TaskDialog.Show("LP", "Не вдалося визначити верхівки блискавкоприймачів.");
                        tg.RollBack();
                        return Result.Succeeded;
                    }

                    // 3) Просторовий індекс (локальні сусіди в радіусі ~ 2R)
                    var index = new SpatialHash2D(cell: R * 2.0);
                    for (int i = 0; i < tips.Count; i++) index.Insert(i, tips[i]);


                    // 4) Кандидатські сфери як перетини трьох сфер
                    var candidateSpheres = new List<Sphere>();
                    double neighborRadius = R * 2.0;

                    for (int i = 0; i < tips.Count; i++)
                    {
                        XYZ pi = tips[i];
                        var neighIdx = index.Query(pi, neighborRadius);
                        // Формуємо тріади на локальному підмножинному наборі
                        for (int a = 0; a < neighIdx.Count; a++)
                        {
                            int j = neighIdx[a];
                            if (j <= i) continue;
                            XYZ pj = tips[j];
                            for (int b = a + 1; b < neighIdx.Count; b++)
                            {
                                int k = neighIdx[b];
                                if (k <= j) continue;
                                XYZ pk = tips[k];

                                var centers = SphereMath.IntersectThreeEqualSpheres(pi, pj, pk, R);
                                if (centers.Count == 0) continue;

                                // Якщо 2 точки — беремо ту, що вище
                                XYZ center = centers.Count == 1
                                    ? centers[0]
                                    : (centers[0].Z >= centers[1].Z ? centers[0] : centers[1]);

                                // Перевірка: ніяка інша верхівка не всередині
                                if (!Collision.HasInteriorPoint(index, tips, center, R, exclude: new[] { i, j, k }))
                                {
                                    candidateSpheres.Add(new Sphere(center, R));
                                }

                            }
                        }
                    }

                    // 5) Дедуплікація (по центру і радіусу з толерансом)
                    var spheres = Sphere.Deduplicate(candidateSpheres, tol: 1e-6);

                    // 6) Обрізання зон захисту
                    var zones = Query.FindElementsWithYesParam(doc, ParamIsZone);
                    int zonesProcessed = 0;
                    int spheresCount = spheres.Count;

                    using (Transaction t = new Transaction(doc, "LP | Trim Protected Zones"))
                    {
                        t.Start();

                        // Побудувати solid-оболонки сфер та їх об’єднати
                        // (щоб не вибухнути пам’яттю, робимо union блоками)
                        var unionSolid = BooleanBatch.BuildUnionOfSpheres(doc, spheres, tessellation: 16);

                        foreach (var zone in zones)
                        {
                            var zoneSolid = SolidUtilsEx.GetMainSolid(zone);
                            if (zoneSolid == null || unionSolid == null) continue;

                            try
                            {
                                var trimmed = BooleanOperationsUtils.ExecuteBooleanOperation(zoneSolid, unionSolid, BooleanOperationsType.Intersect);
                                if (trimmed != null)
                                {
                                    // Записуємо результат у DirectShape (або оновлюємо існуючу геометрію, якщо це ваш власний сімейний тип)
                                    DirectShapeUtils.ReplaceGeometry(doc, zone, trimmed, "LP_Trimmed");
                                    zonesProcessed++;
                                }
                            }
                            catch
                            {
                                // пропускаємо проблемні екземпляри
                            }
                        }

                        t.Commit();
                    }

                    tg.Assimilate();

                    TaskDialog.Show("LP - Report",
                        $"Знайдено блискавкоприймачів: {rods.Count}\n" +
                        $"Обчислено верхівок: {tips.Count}\n" +
                        $"Кандидатських сфер: {candidateSpheres.Count}\n" +
                        $"Унікальних сфер (після дедуплікації): {spheresCount}\n" +
                        $"Обрізано зон захисту: {zonesProcessed}");

                    return Result.Succeeded;
                }
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                return Result.Failed;
            }
        }
    }
}

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
        private const string ParamIsRod = "LP_Is_LightningRod"; // Yes/No параметр
        private const string ParamRadius = "LP_Radius"; // Global parameter (double)

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                using (TransactionGroup tg = new TransactionGroup(doc, "LP | Sphere Calculation"))
                {
                    tg.Start();

                    // 1. Радіус із глобального параметра
                    double R = GlobalParams.GetGlobalDouble(doc, ParamRadius);
                    if (R <= 0)
                    {
                        TaskDialog.Show("LP", $"Глобальний параметр \"{ParamRadius}\" не знайдено або <= 0.");
                        tg.RollBack();
                        return Result.Failed;
                    }

                    // 2. Вибір блискавкоприймачів
                    var rods = Query.FindElementsWithYesParam(doc, ParamIsRod);
                    if (rods.Count == 0)
                    {
                        TaskDialog.Show("LP", "Блискавкоприймачі не знайдені.");
                        tg.RollBack();
                        return Result.Succeeded;
                    }

                    // 3. Вершини (верхівки)
                    var tips = GeometryUtils.ComputeTipPoints(doc, rods);
                    if (tips.Count == 0)
                    {
                        TaskDialog.Show("LP", "Не вдалося визначити верхівки блискавкоприймачів.");
                        tg.RollBack();
                        return Result.Succeeded;
                    }

                    int placed = 0;

                    // 4. Перебираємо всі трійки точок
                    for (int i = 0; i < tips.Count; i++)
                    {
                        for (int j = i + 1; j < tips.Count; j++)
                        {
                            for (int k = j + 1; k < tips.Count; k++)
                            {
                                XYZ p1 = tips[i];
                                XYZ p2 = tips[j];
                                XYZ p3 = tips[k];

                                // шукаємо точки перетину трьох сфер
                                List<XYZ> pts = Service_SphereIntersection.IntersectThreeSpheres(p1, p2, p3, R);

                                if (pts.Count == 1)
                                {
                                    using (Transaction t = new Transaction(doc, "Place Sphere"))
                                    {
                                        t.Start();
                                        FamilyUtils.PlaceSphereVoid(doc, pts[0], R);
                                        t.Commit();
                                    }
                                    placed++;
                                }
                                else if (pts.Count == 2)
                                {
                                    // вибираємо вищу точку
                                    XYZ chosen = pts.OrderByDescending(p => p.Z).First();
                                    using (Transaction t = new Transaction(doc, "Place Sphere"))
                                    {
                                        t.Start();
                                        FamilyUtils.PlaceSphereVoid(doc, chosen, R);
                                        t.Commit();
                                    }
                                    placed++;
                                }
                            }
                        }
                    }

                    tg.Assimilate();

                    TaskDialog.Show("LP - Report",
                        $"Блискавкоприймачів: {rods.Count}\n" +
                        $"Верхівок: {tips.Count}\n" +
                        $"Розміщено сфер: {placed}");

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

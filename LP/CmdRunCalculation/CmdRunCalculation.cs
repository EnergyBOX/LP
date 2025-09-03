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
        private const string FamilyName = "LP_Sphere"; // назва сімейства сфер

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
                    double tolerance = 0.001; // допустиме наближення для уникнення дублікатів

                    // 4. Допоміжні функції
                    bool IsTipInsideSphere(XYZ center, double radius, List<XYZ> tipPoints, int[] excludeIndices)
                    {
                        for (int i = 0; i < tipPoints.Count; i++)
                        {
                            if (excludeIndices.Contains(i)) continue;
                            double dx = center.X - tipPoints[i].X;
                            double dy = center.Y - tipPoints[i].Y;
                            double dz = center.Z - tipPoints[i].Z;
                            if ((dx * dx + dy * dy + dz * dz) < radius * radius)
                                return true;
                        }
                        return false;
                    }

                    bool IsSphereAlreadyPlaced(Document d, XYZ location, double tol)
                    {
                        var existingSpheres = new FilteredElementCollector(d)
                            .OfClass(typeof(FamilyInstance))
                            .Cast<FamilyInstance>()
                            .Where(fi => fi.Symbol.Family.Name == FamilyName);

                        foreach (var fi in existingSpheres)
                        {
                            LocationPoint lp = fi.Location as LocationPoint;
                            if (lp != null && lp.Point.DistanceTo(location) < tol)
                                return true; // вже є сфера в цій точці
                        }
                        return false;
                    }

                    // 5. Перебір всіх трійок точок
                    for (int i = 0; i < tips.Count; i++)
                    {
                        for (int j = i + 1; j < tips.Count; j++)
                        {
                            for (int k = j + 1; k < tips.Count; k++)
                            {
                                XYZ p1 = tips[i];
                                XYZ p2 = tips[j];
                                XYZ p3 = tips[k];

                                List<XYZ> pts = Service_SphereIntersection.IntersectThreeSpheres(p1, p2, p3, R);
                                int[] indices = { i, j, k }; // вершини трикутника

                                foreach (var pt in pts)
                                {
                                    // пропускаємо, якщо всередині сфери є вершина
                                    if (IsTipInsideSphere(pt, R, tips, indices)) continue;

                                    // пропускаємо, якщо сфера вже існує у цій точці
                                    if (IsSphereAlreadyPlaced(doc, pt, tolerance)) continue;

                                    XYZ chosen = pt;
                                    if (pts.Count == 2)
                                        chosen = pts.OrderByDescending(p => p.Z).First(); // верхня точка

                                    using (Transaction t = new Transaction(doc, "Place Sphere"))
                                    {
                                        t.Start();
                                        FamilyUtils.PlaceSphereVoid(doc, chosen, R);
                                        t.Commit();
                                    }

                                    placed++;
                                    break; // вставляємо лише одну сферу для цієї трійки
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

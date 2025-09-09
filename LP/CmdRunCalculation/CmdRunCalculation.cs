using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LP.Services;
using System;
using System.Collections.Generic;

namespace LP
{
    /// <summary>
    /// Головна команда: знаходить блискавкоприймачі, рахує верхівки та вставляє LP_Mesh.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CmdRunCalculation : IExternalCommand
    {
        private const string ParamIsRod = "LP_Is_LightningRod";
        private const string ParamRadius = "LP_Radius";
        private const string MeshFamilyName = "LP_Mesh";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                using (TransactionGroup tg = new TransactionGroup(doc, "LP | Mesh Calculation"))
                {
                    tg.Start();

                    // 1. Отримуємо радіус із глобального параметра
                    double radius = GlobalParamsService.GetGlobalDouble(doc, ParamRadius);
                    if (radius <= 0)
                    {
                        TaskDialog.Show("LP", $"Глобальний параметр {ParamRadius} не знайдено або <= 0.");
                        tg.RollBack();
                        return Result.Failed;
                    }

                    // 2. Знаходимо всі блискавкоприймачі
                    var rods = RodService.GetLightningRods(doc, ParamIsRod);
                    if (rods.Count == 0)
                    {
                        TaskDialog.Show("LP", "Блискавкоприймачі не знайдені.");
                        tg.RollBack();
                        return Result.Succeeded;
                    }

                    // 3. Визначаємо верхівки мачт
                    List<XYZ> tips = GeometryUtils.ComputeTipPoints(doc, rods);
                    if (tips.Count == 0)
                    {
                        TaskDialog.Show("LP", "Не вдалося визначити верхівки.");
                        tg.RollBack();
                        return Result.Succeeded;
                    }

                    // 4. Розрахунок і вставка LP_Mesh через MashService
                    int placed = MashService.PlaceMashes(doc, tips, radius, MeshFamilyName);

                    tg.Assimilate();

                    TaskDialog.Show("LP - Report",
                        $"Блискавкоприймачів: {rods.Count}\n" +
                        $"Верхівок: {tips.Count}\n" +
                        $"Розміщено LP_Mesh: {placed}");

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

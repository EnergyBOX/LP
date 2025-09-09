using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LP.Services;
using System;

namespace LP
{
    [Transaction(TransactionMode.Manual)]
    public class CmdRunCalculation : IExternalCommand
    {
        private const string ParamIsRod = "LP_Is_LightningRod";
        private const string ParamRadius = "LP_Radius";
        private const string FamilyName = "LP_Sphere";

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
                    double R = GlobalParamsService.GetGlobalDouble(doc, ParamRadius);
                    if (R <= 0)
                    {
                        TaskDialog.Show("LP", $"Глобальний параметр {ParamRadius} не знайдено або <= 0.");
                        tg.RollBack();
                        return Result.Failed;
                    }

                    // 2. Знаходимо блискавкоприймачі
                    var rods = RodService.GetLightningRods(doc, ParamIsRod);
                    if (rods.Count == 0)
                    {
                        TaskDialog.Show("LP", "Блискавкоприймачі не знайдені.");
                        tg.RollBack();
                        return Result.Succeeded;
                    }

                    // 3. Визначаємо верхівки
                    var tips = RodService.GetTips(doc, rods);
                    if (tips.Count == 0)
                    {
                        TaskDialog.Show("LP", "Не вдалося визначити верхівки.");
                        tg.RollBack();
                        return Result.Succeeded;
                    }

                    // 4. Вставляємо LP_Mash
                    var result = MashService.PlaceMashes(doc, tips, R, "LP_Mesh"); // передаємо R
                    int placed = result.placed;
                    var pointsLog = result.pointsLog; // якщо PlaceMashes повертає лог

                    tg.Assimilate();

                    TaskDialog.Show("LP - Report",
                        $"Блискавкоприймачів: {rods.Count}\n" +
                        $"Верхівок: {tips.Count}\n" +
                        $"Розміщено LP_Mash: {placed}");

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

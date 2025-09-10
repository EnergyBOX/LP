using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace LP
{
    [Transaction(TransactionMode.Manual)]
    public class CmdClearResults : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                // Викликаємо сервіс очистки
                var clearResult = CleaningResultsService.Clear(doc);

                // Показуємо звіт
                TaskDialog.Show("Clear Results",
                    $"Знайдено екземплярів LP_Mesh: {clearResult.TotalFound}\n" +
                    $"Видалено (IsMesh=Yes, Unpinned): {clearResult.TotalDeleted}");

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

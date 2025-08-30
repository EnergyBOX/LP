using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace LP
{
    //Current Calculation Result
    [Transaction(TransactionMode.Manual)]
    public class CmdCurrentResult : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Тут можна реалізувати логіку отримання поточного результату
            TaskDialog.Show("Current Result", "Current lightning protection calculation result displayed.");
            return Result.Succeeded;
        }
    }
}
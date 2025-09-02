using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace LP
{
    //Clear Calculation Results
    [Transaction(TransactionMode.Manual)]
    public class CmdClearResults : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog.Show("Info", "Clear Calculation Results command executed.");
            return Result.Succeeded;
        }
    }
}
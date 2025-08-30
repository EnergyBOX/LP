using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace LP
{
    //Perform Calculation
    [Transaction(TransactionMode.Manual)]
    public class CmdRunCalculation : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog.Show("Info", "Perform Calculation command executed.");
            return Result.Succeeded;
        }
    }
}
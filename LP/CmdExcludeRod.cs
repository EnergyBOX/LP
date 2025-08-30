using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace LP
{
    //Exclude Lightning Rod
    [Transaction(TransactionMode.Manual)]
    public class CmdExcludeRod : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog.Show("Info", "Exclude Lightning Rod command executed.");
            return Result.Succeeded;
        }
    }
}
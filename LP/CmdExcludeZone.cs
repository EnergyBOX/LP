using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace LP
{
    //Exclude Protected Zone
    [Transaction(TransactionMode.Manual)]
    public class CmdExcludeZone : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog.Show("Info", "Exclude Protected Zone command executed.");
            return Result.Succeeded;
        }
    }
}
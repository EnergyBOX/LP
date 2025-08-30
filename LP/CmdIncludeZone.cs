using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace LP
{
    //Include Protected Zone
    [Transaction(TransactionMode.Manual)]
    public class CmdIncludeZone : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog.Show("Info", "Include Protected Zone command executed.");
            return Result.Succeeded;
        }
    }
}
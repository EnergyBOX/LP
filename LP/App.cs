using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Xml.Linq;

namespace LP
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // 1. Create Ribbon Tab
                string tabName = "LP";
                try
                {
                    application.CreateRibbonTab(tabName);
                }
                catch (Exception) { /* if tab already exists */ }

                // 2. Create Ribbon Panel
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Lightning Protection Calculation");

                // 3. Add buttons
                string assemblyPath = typeof(App).Assembly.Location;

                PushButtonData includeRod = new PushButtonData(
                    "IncludeRod",
                    "Include \nLightning \nRod",
                    assemblyPath,
                    "LP.CmdIncludeRod");
                includeRod.ToolTip = "The parameter will be applied and enabled on all selected elements, marking each as a lightning rod for use in the calculation.";

                PushButtonData excludeRod = new PushButtonData(
                    "ExcludeRod",
                    "Exclude \nLightning \nRod",
                    assemblyPath,
                    "LP.CmdExcludeRod");
                excludeRod.ToolTip = "The parameter will be disabled on all selected elements, removing their designation as lightning rods for the calculation.";

                PushButtonData includeZone = new PushButtonData(
                    "IncludeZone",
                    "Include \nProtected \nZone",
                    assemblyPath,
                    "LP.CmdIncludeZone");
                includeZone.ToolTip = "The parameter will be applied and enabled on the selected element, marking it as the volume protected by the rod for calculation purposes.";

                PushButtonData excludeZone = new PushButtonData(
                    "ExcludeZone",
                    "Exclude \nProtected \nZone",
                    assemblyPath,
                    "LP.CmdExcludeZone");
                excludeZone.ToolTip = "The parameter will be disabled on the selected element, removing its designation as the volume protected by the rod for calculation purposes.";

                PushButtonData selectMesh = new PushButtonData(
                    "SelectSphere",
                    "Select \nSphere",
                    assemblyPath,
                    "LP.CmdSelectMesh");
                selectMesh.ToolTip = "Check if LP_Sphere family is loaded, then select a sphere radius for calculations.";

                PushButtonData runCalculation = new PushButtonData(
                    "RunCalculation",
                    "Perform \nCalculation",
                    assemblyPath,
                    "LP.CmdRunCalculation");
                runCalculation.ToolTip = "Previous results will be deleted, and the calculation will be performed for all involved elements.";

                PushButtonData cutZones = new PushButtonData(
                    "CutProtectedZones",
                    "Cut \nProtected \nZones",
                    assemblyPath,
                    "LP.CmdCutProtectedZones");
                cutZones.ToolTip = "Cut all protected zones using sphere-voids in the project.";

                PushButtonData clearResults = new PushButtonData(
                    "ClearResults",
                    "Clear \nCalculation \nResults",
                    assemblyPath,
                    "LP.CmdClearResults");
                clearResults.ToolTip = "All previous calculation results will be cleared";

                PushButtonData currentResult = new PushButtonData(
                    "CurrentResult",
                    "Current \nCalculation \nResult",
                    assemblyPath,
                    "LP.CmdCurrentResult");
                currentResult.ToolTip = "Show the current result of the lightning protection calculation";

                panel.AddItem(includeRod);
                panel.AddItem(excludeRod);
                panel.AddItem(includeZone);
                panel.AddItem(excludeZone);
                panel.AddItem(selectMesh);
                panel.AddItem(runCalculation);
                panel.AddItem(cutZones);
                panel.AddItem(clearResults);
                panel.AddItem(currentResult);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}

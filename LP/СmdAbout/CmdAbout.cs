using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Diagnostics;

namespace LP
{
    [Transaction(TransactionMode.Manual)]
    public class CmdAbout : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            string info =
                "LP v1.1.0 Add-in for Revit\n\n" +
                "Author: Volodymyr Davydovych\n\n" +
                "Quick Instruction:\n" +
                "0. First, save your project.\n" +
                "1. Select the masts you plan to use as lightning rods and activate them using the \"Include Lightning Rod\" button.\n" +
                "2. Click \"Select Mesh\" and choose one of the options to define the basis for the calculation.\n" +
                "3. Click \"Perform Calculation\" to run the calculation. After 1–2 minutes, the meshes will be placed in the current workset.\n" +
                "4. Use \"Clear Calculation Results\" to remove meshes before performing a new calculation.\n" +
                "5. Use \"Current Calculation Result\" to review which elements are related to the calculation or to select them.\n" +
                "Done.\n\n" +
                "Visit GitHub or Releases using the buttons below.";

            TaskDialog dialog = new TaskDialog("About LP Add-in")
            {
                MainInstruction = "About this Add-in",
                MainContent = info,
                CommonButtons = TaskDialogCommonButtons.Close
            };

            // Додамо кнопки для GitHub і Releases
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Open GitHub");
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Open Releases");

            TaskDialogResult result = dialog.Show();

            // Відкриваємо браузер за вибором користувача
            if (result == TaskDialogResult.CommandLink1)
                Process.Start(new ProcessStartInfo("https://github.com/EnergyBOX") { UseShellExecute = true });
            else if (result == TaskDialogResult.CommandLink2)
                Process.Start(new ProcessStartInfo("https://github.com/EnergyBOX/LP/releases") { UseShellExecute = true });

            return Result.Succeeded;
        }
    }
}

using Autodesk.Revit.Attributes;

namespace LP
{
    [Transaction(TransactionMode.Manual)]
    public class CmdIncludeRod : CmdParameterToggle
    {
        protected override string ParameterName => "LP_Is_LightningRod";
        protected override int ParameterValue => 1;
    }

    [Transaction(TransactionMode.Manual)]
    public class CmdExcludeRod : CmdParameterToggle
    {
        protected override string ParameterName => "LP_Is_LightningRod";
        protected override int ParameterValue => 0;
    }

    [Transaction(TransactionMode.Manual)]
    public class CmdIncludeZone : CmdParameterToggle
    {
        protected override string ParameterName => "LP_Is_ProtectedZone";
        protected override int ParameterValue => 1;
    }

    [Transaction(TransactionMode.Manual)]
    public class CmdExcludeZone : CmdParameterToggle
    {
        protected override string ParameterName => "LP_Is_ProtectedZone";
        protected override int ParameterValue => 0;
    }
}

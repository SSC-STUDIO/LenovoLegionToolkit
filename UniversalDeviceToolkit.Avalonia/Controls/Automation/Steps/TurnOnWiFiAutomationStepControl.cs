using System.Threading.Tasks;
using Avalonia.Controls;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class TurnOnWiFiAutomationStepControl : AbstractAutomationStepControl
{
    public TurnOnWiFiAutomationStepControl(TurnOnWiFiAutomationStep automationStep) : base(automationStep)
    {
        Icon = SymbolRegular.Wifi124;
        Title = Resource.TurnOnWiFiAutomationStepControl_Title;
    }

    public override IAutomationStep CreateAutomationStep() => new TurnOnWiFiAutomationStep();

    protected override Control? GetCustomControl() => null;

    protected override void OnFinishedLoading() { }

    protected override Task RefreshAsync() => Task.CompletedTask;
}

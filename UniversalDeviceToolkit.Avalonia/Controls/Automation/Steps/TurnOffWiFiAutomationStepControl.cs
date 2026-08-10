using System.Threading.Tasks;
using Avalonia.Controls;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class TurnOffWiFiAutomationStepControl : AbstractAutomationStepControl
{
    public TurnOffWiFiAutomationStepControl(TurnOffWiFiAutomationStep automationStep) : base(automationStep)
    {
        Icon = SymbolRegular.WifiOff24;
        Title = Resource.TurnOffWiFiAutomationStepControl_Title;
    }

    public override IAutomationStep CreateAutomationStep() => new TurnOffWiFiAutomationStep();

    protected override Control? GetCustomControl() => null;

    protected override void OnFinishedLoading() { }

    protected override Task RefreshAsync() => Task.CompletedTask;
}

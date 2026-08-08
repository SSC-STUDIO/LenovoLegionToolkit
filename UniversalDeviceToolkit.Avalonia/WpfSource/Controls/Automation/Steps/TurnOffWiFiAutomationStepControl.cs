using System.Threading.Tasks;
using System.Windows;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class TurnOffWiFiAutomationStepControl : AbstractAutomationStepControl
{
    public TurnOffWiFiAutomationStepControl(TurnOffWiFiAutomationStep automationStep) : base(automationStep)
    {
        Icon = SymbolRegular.WifiOff24;
        Title = Resource.TurnOffWiFiAutomationStepControl_Title;
    }

    public override IAutomationStep CreateAutomationStep() => new TurnOffWiFiAutomationStep();

    protected override UIElement? GetCustomControl() => null;

    protected override void OnFinishedLoading() { }

    protected override Task RefreshAsync() => Task.CompletedTask;
}


using System.Threading.Tasks;
using System.Windows;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class TurnOnWiFiAutomationStepControl : AbstractAutomationStepControl
{
    public TurnOnWiFiAutomationStepControl(TurnOnWiFiAutomationStep automationStep) : base(automationStep)
    {
        Icon = SymbolRegular.Wifi124;
        Title = Resource.TurnOnWiFiAutomationStepControl_Title;
    }

    public override IAutomationStep CreateAutomationStep() => new TurnOnWiFiAutomationStep();

    protected override UIElement? GetCustomControl() => null;

    protected override void OnFinishedLoading() { }

    protected override Task RefreshAsync() => Task.CompletedTask;
}


using System.Threading.Tasks;
using System.Windows;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class HideMainWindowAutomationStepControl : AbstractAutomationStepControl
{
    public HideMainWindowAutomationStepControl(HideMainWindowAutomationStep automationStep) : base(automationStep)
    {
        Icon = SymbolRegular.Dismiss24;
        Title = LocalizationHelper.GetStringOrEnglish(
            Resource.ResourceManager,
            "HideMainWindowAutomationStepControl_Title",
            "Hide main window",
            Resource.Culture);
    }

    public override IAutomationStep CreateAutomationStep() => new HideMainWindowAutomationStep();
    protected override UIElement? GetCustomControl() => null;
    protected override void OnFinishedLoading() { }
    protected override Task RefreshAsync() => Task.CompletedTask;
}

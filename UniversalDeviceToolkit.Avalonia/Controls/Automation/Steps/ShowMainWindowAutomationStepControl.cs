using System.Threading.Tasks;
using System.Windows;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class ShowMainWindowAutomationStepControl : AbstractAutomationStepControl
{
    public ShowMainWindowAutomationStepControl(ShowMainWindowAutomationStep automationStep) : base(automationStep)
    {
        Icon = SymbolRegular.Window24;
        Title = LocalizationHelper.GetStringOrEnglish(
            Resource.ResourceManager,
            "ShowMainWindowAutomationStepControl_Title",
            "Show main window",
            Resource.Culture);
    }

    public override IAutomationStep CreateAutomationStep() => new ShowMainWindowAutomationStep();
    protected override UIElement? GetCustomControl() => null;
    protected override void OnFinishedLoading() { }
    protected override Task RefreshAsync() => Task.CompletedTask;
}

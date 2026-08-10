using System.Threading.Tasks;
using Avalonia.Controls;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

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
    protected override Control? GetCustomControl() => null;
    protected override void OnFinishedLoading() { }
    protected override Task RefreshAsync() => Task.CompletedTask;
}

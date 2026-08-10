using System.Threading.Tasks;
using Avalonia.Controls;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

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
    protected override Control? GetCustomControl() => null;
    protected override void OnFinishedLoading() { }
    protected override Task RefreshAsync() => Task.CompletedTask;
}

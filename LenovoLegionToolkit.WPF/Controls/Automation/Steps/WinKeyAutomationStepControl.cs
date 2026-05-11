using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Automation.Steps;
using LenovoLegionToolkit.WPF.Resources;

namespace LenovoLegionToolkit.WPF.Controls.Automation.Steps;

public class WinKeyAutomationStepControl : AbstractComboBoxAutomationStepCardControl<WinKeyState>
{
    public WinKeyAutomationStepControl(IAutomationStep<WinKeyState> step) : base(step)
    {
        Icon = SymbolRegular.Keyboard24;
        Title = Resource.WinKeyAutomationStepControl_Title;
        Subtitle = Resource.WinKeyAutomationStepControl_Message;
    }
}

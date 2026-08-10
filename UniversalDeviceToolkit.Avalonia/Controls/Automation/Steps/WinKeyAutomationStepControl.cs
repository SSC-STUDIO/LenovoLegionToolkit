using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class WinKeyAutomationStepControl : AbstractComboBoxAutomationStepCardControl<WinKeyState>
{
    public WinKeyAutomationStepControl(IAutomationStep<WinKeyState> step) : base(step)
    {
        Icon = SymbolRegular.Keyboard24;
        Title = Resource.WinKeyAutomationStepControl_Title;
        Subtitle = Resource.WinKeyAutomationStepControl_Message;
    }
}

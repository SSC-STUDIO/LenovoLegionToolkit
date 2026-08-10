using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class OneLevelWhiteKeyboardBacklightAutomationStepControl : AbstractComboBoxAutomationStepCardControl<OneLevelWhiteKeyboardBacklightState>
{
    public OneLevelWhiteKeyboardBacklightAutomationStepControl(IAutomationStep<OneLevelWhiteKeyboardBacklightState> step) : base(step)
    {
        Icon = SymbolRegular.Keyboard24;
        Title = Resource.OneLevelWhiteKeyboardBacklightAutomationStepControl_Title;
        Subtitle = Resource.OneLevelWhiteKeyboardBacklightAutomationStepControl_Message;
    }
}

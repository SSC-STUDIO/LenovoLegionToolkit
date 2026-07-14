using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class OneLevelWhiteKeyboardBacklightAutomationStepControl : AbstractComboBoxAutomationStepCardControl<OneLevelWhiteKeyboardBacklightState>
{
    public OneLevelWhiteKeyboardBacklightAutomationStepControl(IAutomationStep<OneLevelWhiteKeyboardBacklightState> step) : base(step)
    {
        Icon = SymbolRegular.Keyboard24;
        Title = Resource.OneLevelWhiteKeyboardBacklightAutomationStepControl_Title;
        Subtitle = Resource.OneLevelWhiteKeyboardBacklightAutomationStepControl_Message;
    }
}


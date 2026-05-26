using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class SpectrumKeyboardBacklightBrightnessAutomationStepControl : AbstractComboBoxAutomationStepCardControl<int>
{
    public SpectrumKeyboardBacklightBrightnessAutomationStepControl(IAutomationStep<int> step) : base(step)
    {
        Icon = SymbolRegular.BrightnessHigh24;
        Title = Resource.SpectrumKeyboardBacklightBrightnessAutomationStepControl_Title;
        Subtitle = Resource.SpectrumKeyboardBacklightBrightnessAutomationStepControl_Message;
    }

    protected override string ComboBoxItemDisplayName(int value)
    {
        return value == 0
            ? Resource.SpectrumKeyboardBacklightBrightnessAutomationStepControl_Off
            : base.ComboBoxItemDisplayName(value);
    }
}


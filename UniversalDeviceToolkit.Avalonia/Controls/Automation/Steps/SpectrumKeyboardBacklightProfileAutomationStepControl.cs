using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class SpectrumKeyboardBacklightProfileAutomationStepControl : AbstractComboBoxAutomationStepCardControl<int>
{
    public SpectrumKeyboardBacklightProfileAutomationStepControl(IAutomationStep<int> step) : base(step)
    {
        Icon = SymbolRegular.BrightnessHigh24;
        Title = Resource.SpectrumKeyboardBacklightProfileAutomationStepControl_Title;
        Subtitle = Resource.SpectrumKeyboardBacklightProfileAutomationStepControl_Message;
    }
}

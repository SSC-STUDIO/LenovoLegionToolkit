using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class RGBKeyboardBacklightAutomationStepControl : AbstractComboBoxAutomationStepCardControl<RGBKeyboardBacklightPreset>
{
    public RGBKeyboardBacklightAutomationStepControl(IAutomationStep<RGBKeyboardBacklightPreset> step) : base(step)
    {
        Icon = SymbolRegular.Keyboard24;
        Title = Resource.RGBKeyboardBacklightAutomationStepControl_Title;
        Subtitle = Resource.RGBKeyboardBacklightAutomationStepControl_Message;
    }
}

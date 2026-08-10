using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class SpeakerAutomationStepControl : AbstractComboBoxAutomationStepCardControl<SpeakerState>
{
    public SpeakerAutomationStepControl(IAutomationStep<SpeakerState> step) : base(step)
    {
        Icon = SymbolRegular.Speaker224;
        Title = Resource.SpeakerAutomationStepControl_Title;
        Subtitle = Resource.SpeakerAutomationStepControl_Message;
    }
}

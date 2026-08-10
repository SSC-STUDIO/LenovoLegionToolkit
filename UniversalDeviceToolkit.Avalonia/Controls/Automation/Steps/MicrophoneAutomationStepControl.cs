using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class MicrophoneAutomationStepControl : AbstractComboBoxAutomationStepCardControl<MicrophoneState>
{
    public MicrophoneAutomationStepControl(IAutomationStep<MicrophoneState> step) : base(step)
    {
        Icon = SymbolRegular.Mic24;
        Title = Resource.MicrophoneAutomationStepControl_Title;
        Subtitle = Resource.MicrophoneAutomationStepControl_Message;
    }
}

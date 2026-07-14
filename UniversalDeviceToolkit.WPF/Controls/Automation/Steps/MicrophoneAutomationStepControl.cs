using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class MicrophoneAutomationStepControl : AbstractComboBoxAutomationStepCardControl<MicrophoneState>
{
    public MicrophoneAutomationStepControl(IAutomationStep<MicrophoneState> step) : base(step)
    {
        Icon = SymbolRegular.Mic24;
        Title = Resource.MicrophoneAutomationStepControl_Title;
        Subtitle = Resource.MicrophoneAutomationStepControl_Message;
    }
}


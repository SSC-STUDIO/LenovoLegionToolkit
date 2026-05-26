using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class SpeakerAutomationStepControl : AbstractComboBoxAutomationStepCardControl<SpeakerState>
{
    public SpeakerAutomationStepControl(IAutomationStep<SpeakerState> step) : base(step)
    {
        Icon = SymbolRegular.Speaker224;
        Title = Resource.SpeakerAutomationStepControl_Title;
        Subtitle = Resource.SpeakerAutomationStepControl_Message;
    }
}


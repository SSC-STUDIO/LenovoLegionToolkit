using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class PanelLogoBacklightAutomationStepControl : AbstractComboBoxAutomationStepCardControl<PanelLogoBacklightState>
{
    public PanelLogoBacklightAutomationStepControl(IAutomationStep<PanelLogoBacklightState> step) : base(step)
    {
        Icon = SymbolRegular.LightbulbCircle24;
        Title = Resource.PanelLogoBacklightAutomationStepControl_Title;
        Subtitle = Resource.PanelLogoBacklightAutomationStepControl_Message;
    }
}

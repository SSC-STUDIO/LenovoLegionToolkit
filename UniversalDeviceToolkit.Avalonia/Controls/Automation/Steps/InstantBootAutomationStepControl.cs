using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

internal class InstantBootAutomationStepControl : AbstractComboBoxAutomationStepCardControl<InstantBootState>
{
    public InstantBootAutomationStepControl(IAutomationStep<InstantBootState> step) : base(step)
    {
        Icon = SymbolRegular.PlugDisconnected24;
        Title = Resource.InstantBootAutomationStepControl_Title;
        Subtitle = Resource.InstantBootAutomationStepControl_Message;
    }
}

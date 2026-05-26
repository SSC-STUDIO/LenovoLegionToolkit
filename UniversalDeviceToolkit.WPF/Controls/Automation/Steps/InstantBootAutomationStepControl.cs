using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

internal class InstantBootAutomationStepControl : AbstractComboBoxAutomationStepCardControl<InstantBootState>
{
    public InstantBootAutomationStepControl(IAutomationStep<InstantBootState> step) : base(step)
    {
        Icon = SymbolRegular.PlugDisconnected24;
        Title = Resource.InstantBootAutomationStepControl_Title;
        Subtitle = Resource.InstantBootAutomationStepControl_Message;
    }
}


using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class MacroAutomationStepControl : AbstractComboBoxAutomationStepCardControl<MacroAutomationStepState>
{
    public MacroAutomationStepControl(IAutomationStep<MacroAutomationStepState> step) : base(step)
    {
        Icon = SymbolRegular.ReceiptPlay24;
        Title = Resource.MacroAutomationStepControl_Title;
        Subtitle = Resource.MacroAutomationStepControl_Message;
    }
}


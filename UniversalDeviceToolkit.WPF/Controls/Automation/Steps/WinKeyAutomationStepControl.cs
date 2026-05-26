using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class WinKeyAutomationStepControl : AbstractComboBoxAutomationStepCardControl<WinKeyState>
{
    public WinKeyAutomationStepControl(IAutomationStep<WinKeyState> step) : base(step)
    {
        Icon = SymbolRegular.Keyboard24;
        Title = Resource.WinKeyAutomationStepControl_Title;
        Subtitle = Resource.WinKeyAutomationStepControl_Message;
    }
}


using System;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using LenovoLegionToolkit.Lib.Listeners;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class DpiScaleAutomationStepControl : AbstractComboBoxAutomationStepCardControl<DpiScale>
{
    private readonly DisplayConfigurationListener _listener = IoCContainer.Resolve<DisplayConfigurationListener>();

    public DpiScaleAutomationStepControl(IAutomationStep<DpiScale> step) : base(step)
    {
        Icon = SymbolRegular.TextFontSize24;
        Title = Resource.DpiScaleAutomationStepControl_Title;
        Subtitle = Resource.DpiScaleAutomationStepControl_Message;

        _listener.Changed += Listener_Changed;
        Unloaded += (_, _) => _listener.Changed -= Listener_Changed;
    }

    protected override string ComboBoxItemDisplayName(DpiScale value)
    {
        var str = base.ComboBoxItemDisplayName(value);
        return LocalizationHelper.ForceLeftToRight(str);
    }

    private void Listener_Changed(object? sender, EventArgs e) => Dispatcher.InvokeTask(async () =>
    {
        if (IsLoaded)
            await RefreshAsync();
    }, "refresh DPI scale automation step");
}

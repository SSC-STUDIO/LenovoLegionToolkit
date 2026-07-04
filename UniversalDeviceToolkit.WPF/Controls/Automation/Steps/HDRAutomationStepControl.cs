using System;
using System.Windows;
using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using LenovoLegionToolkit.Lib.Listeners;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class HDRAutomationStepControl : AbstractComboBoxAutomationStepCardControl<HDRState>
{
    private readonly DisplayConfigurationListener _listener = IoCContainer.Resolve<DisplayConfigurationListener>();

    public HDRAutomationStepControl(IAutomationStep<HDRState> step) : base(step)
    {
        Icon = SymbolRegular.Hdr24;
        Title = Resource.HDRAutomationStepControl_Title;
        Subtitle = Resource.HDRAutomationStepControl_Message;

        _listener.Changed += Listener_Changed;
        Unloaded += HDRAutomationStepControl_Unloaded;
    }

    private void HDRAutomationStepControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _listener.Changed -= Listener_Changed;
    }

    private void Listener_Changed(object? sender, EventArgs e) => Dispatcher.InvokeTask(async () =>
    {
        if (IsLoaded)
            await RefreshAsync().ConfigureAwait(false);
    }, "refresh HDR automation step");
}

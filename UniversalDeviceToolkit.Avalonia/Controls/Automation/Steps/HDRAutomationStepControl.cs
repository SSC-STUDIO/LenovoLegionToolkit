using System;
using Avalonia.Interactivity;
using Avalonia.Threading;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

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

    private void Listener_Changed(object? sender, EventArgs e) => Dispatcher.UIThread.InvokeTask(async () =>
    {
        if (IsLoaded)
            await RefreshAsync();
    }, "refresh HDR automation step");
}

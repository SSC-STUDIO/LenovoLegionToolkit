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

public class RefreshRateAutomationStepControl : AbstractComboBoxAutomationStepCardControl<RefreshRate>
{
    private readonly DisplayConfigurationListener _listener = IoCContainer.Resolve<DisplayConfigurationListener>();

    public RefreshRateAutomationStepControl(IAutomationStep<RefreshRate> step) : base(step)
    {
        Icon = SymbolRegular.DesktopPulse24;
        Title = Resource.RefreshRateAutomationStepControl_Title;
        Subtitle = Resource.RefreshRateAutomationStepControl_Message;

        _listener.Changed += Listener_Changed;
        Unloaded += RefreshRateAutomationStepControl_Unloaded;
    }

    private void RefreshRateAutomationStepControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _listener.Changed -= Listener_Changed;
    }

    protected override string ComboBoxItemDisplayName(RefreshRate value)
    {
        var str = base.ComboBoxItemDisplayName(value);
        return LocalizationHelper.ForceLeftToRight(str);
    }

    private void Listener_Changed(object? sender, EventArgs e) => Dispatcher.UIThread.InvokeTask(async () =>
    {
        if (IsLoaded)
            await RefreshAsync();
    }, "refresh refresh rate automation step");
}

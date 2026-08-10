using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard;

public class RefreshRateControl : AbstractComboBoxFeatureCardControl<RefreshRate>
{
    private readonly DisplayConfigurationListener _listener = IoCContainer.Resolve<DisplayConfigurationListener>();

    public RefreshRateControl()
    {
        Icon = SymbolRegular.DesktopPulse24;
        Title = Resource.RefreshRateControl_Title;
        Subtitle = Resource.RefreshRateControl_Message;

        _listener.Changed += Listener_Changed;
        Unloaded += (_, _) => _listener.Changed -= Listener_Changed;
    }

    protected override async Task OnRefreshAsync()
    {
        await base.OnRefreshAsync();

        IsVisible = ItemsCount < 2 ? false : true;
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
    }, "refresh refresh rate control");
}

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Listeners;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard;

public class HDRControl : AbstractToggleFeatureCardControl<HDRState>
{
    private readonly DisplayConfigurationListener _listener = IoCContainer.Resolve<DisplayConfigurationListener>();

    protected override HDRState OnState => HDRState.On;

    protected override HDRState OffState => HDRState.Off;

    public HDRControl()
    {
        Icon = SymbolRegular.Hdr24;
        Title = Resource.HDRControl_Title;
        Subtitle = Resource.HDRControl_Message;

        _listener.Changed += Listener_Changed;
        Unloaded += (_, _) => _listener.Changed -= Listener_Changed;
    }

    protected override async Task OnRefreshAsync()
    {
        await base.OnRefreshAsync();

        try
        {
            var isHdrBlocked = await ((HDRFeature)Feature).IsHdrBlockedAsync();

            IsToggleEnabled = !isHdrBlocked;
            Warning = isHdrBlocked ? Resource.HDRControl_Warning : string.Empty;
        }
        catch
        {
            IsToggleEnabled = true;
            Warning = string.Empty;
        }

        IsVisible = true;
    }

    private void Listener_Changed(object? sender, EventArgs e) => Dispatcher.UIThread.InvokeTask(async () =>
    {
        if (IsLoaded)
            await RefreshAsync();
    }, "refresh HDR control");
}

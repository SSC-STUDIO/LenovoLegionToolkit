using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Listeners;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

public class TouchpadLockControl : AbstractToggleFeatureCardControl<TouchpadLockState>
{
    private readonly DriverKeyListener _listener = IoCContainer.Resolve<DriverKeyListener>();

    protected override TouchpadLockState OnState => TouchpadLockState.On;

    protected override TouchpadLockState OffState => TouchpadLockState.Off;

    public TouchpadLockControl()
    {
        Icon = SymbolRegular.Tablet24;
        Title = Resource.TouchpadLockControl_Title;
        Subtitle = Resource.TouchpadLockControl_Message;

        _listener.Changed += Listener_Changed;
        Unloaded += (_, _) => _listener.Changed -= Listener_Changed;
    }

    protected override async Task OnStateChange(ToggleSwitch toggle, IFeature<TouchpadLockState> feature)
    {
        await _listener.StopAsync().ConfigureAwait(false);
        await base.OnStateChange(toggle, feature).ConfigureAwait(false);
        await _listener.StartAsync().ConfigureAwait(false);
    }

    private void Listener_Changed(object? sender, DriverKeyListener.ChangedEventArgs e) => Dispatcher.InvokeTask(async () =>
    {
        if (!IsLoaded || !IsVisible)
            return;

        if (e.DriverKey.HasFlag(DriverKey.FnF10))
            await RefreshAsync().ConfigureAwait(false);
    }, "refresh touchpad lock control");
}

using Avalonia.Threading;
using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard;

public class FnLockControl : AbstractToggleFeatureCardControl<FnLockState>
{
    private readonly SpecialKeyListener _listener = IoCContainer.Resolve<SpecialKeyListener>();

    protected override FnLockState OnState => FnLockState.On;

    protected override FnLockState OffState => FnLockState.Off;

    public FnLockControl()
    {
        Icon = SymbolRegular.Keyboard24;
        Title = Resource.FnLockControl_Title;
        Subtitle = Resource.FnLockControl_Message;

        _listener.Changed += Listener_Changed;
        Unloaded += (_, _) => _listener.Changed -= Listener_Changed;
    }

    private void Listener_Changed(object? sender, SpecialKeyListener.ChangedEventArgs e) => Dispatcher.UIThread.InvokeTask(async () =>
    {
        if (!IsLoaded || !IsVisible)
            return;

        if (e.SpecialKey is SpecialKey.FnLockOn or SpecialKey.FnLockOff)
            await RefreshAsync();
    }, "refresh FnLock control");
}

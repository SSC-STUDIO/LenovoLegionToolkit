using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

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

    private void Listener_Changed(object? sender, SpecialKeyListener.ChangedEventArgs e) => Dispatcher.InvokeTask(async () =>
    {
        if (!IsLoaded || !IsVisible)
            return;

        if (e.SpecialKey is SpecialKey.FnLockOn or SpecialKey.FnLockOff)
            await RefreshAsync();
    }, "refresh FnLock control");
}

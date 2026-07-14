using System;
using LenovoLegionToolkit.Lib.Listeners;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

public class WinKeyControl : AbstractToggleFeatureCardControl<WinKeyState>
{
    private readonly WinKeyListener _listener = IoCContainer.Resolve<WinKeyListener>();

    protected override WinKeyState OnState => WinKeyState.On;

    protected override WinKeyState OffState => WinKeyState.Off;

    protected override bool DisablesWhileRefreshing => false;

    public WinKeyControl()
    {
        Icon = SymbolRegular.Keyboard24;
        Title = Resource.WinKeyControl_Title;
        Subtitle = Resource.WinKeyControl_Message;

        _listener.Changed += Listener_Changed;
        Unloaded += (_, _) => _listener.Changed -= Listener_Changed;
    }

    private void Listener_Changed(object? sender, EventArgs e) => Dispatcher.InvokeTask(async () =>
    {
        if (!IsLoaded || !IsVisible)
            return;

        await RefreshAsync();
    }, "refresh WinKey control");
}

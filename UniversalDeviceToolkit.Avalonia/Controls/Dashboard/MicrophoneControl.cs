using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Lib.Listeners;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard;

public class MicrophoneControl : AbstractToggleFeatureCardControl<MicrophoneState>
{
    private readonly DriverKeyListener _listener = IoCContainer.Resolve<DriverKeyListener>();

    protected override MicrophoneState OnState => MicrophoneState.On;
    protected override MicrophoneState OffState => MicrophoneState.Off;

    public MicrophoneControl()
    {
        Icon = SymbolRegular.Mic24;
        Title = Resource.MicrophoneControl_Title;
        Subtitle = Resource.MicrophoneControl_Message;

        _listener.Changed += Listener_Changed;
        Unloaded += (_, _) => _listener.Changed -= Listener_Changed;
    }

    private void Listener_Changed(object? sender, DriverKeyListener.ChangedEventArgs e) => Dispatcher.UIThread.InvokeTask(async () =>
    {
        if (!IsLoaded || !IsVisible)
            return;

        if (e.DriverKey.HasFlag(DriverKey.FnF4))
            await RefreshAsync();
    }, "refresh microphone control");
}

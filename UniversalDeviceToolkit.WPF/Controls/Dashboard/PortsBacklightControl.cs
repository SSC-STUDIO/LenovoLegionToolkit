using LenovoLegionToolkit.Lib.Listeners;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

public class PortsBacklightControl : AbstractToggleFeatureCardControl<PortsBacklightState>
{
    private readonly LightingChangeListener _listener = IoCContainer.Resolve<LightingChangeListener>();

    protected override PortsBacklightState OnState => PortsBacklightState.On;

    protected override PortsBacklightState OffState => PortsBacklightState.Off;

    public PortsBacklightControl()
    {
        Icon = SymbolRegular.UsbPlug24;
        Title = Resource.PortsBacklightControl_Title;
        Subtitle = Resource.PortsBacklightControl_Message;

        _listener.Changed += Listener_Changed;
        Unloaded += (_, _) => _listener.Changed -= Listener_Changed;
    }

    private void Listener_Changed(object? sender, LightingChangeListener.ChangedEventArgs e) => Dispatcher.InvokeTask(async () =>
    {
        if (e.State != LightingChangeState.Ports)
            return;

        await RefreshAsync();
    }, "refresh ports backlight control");
}

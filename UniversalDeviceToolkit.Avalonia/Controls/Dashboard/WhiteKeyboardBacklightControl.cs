using Avalonia.Threading;
using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard;

public class WhiteKeyboardBacklightControl : AbstractComboBoxFeatureCardControl<WhiteKeyboardBacklightState>
{
    private readonly DriverKeyListener _listener = IoCContainer.Resolve<DriverKeyListener>();

    public WhiteKeyboardBacklightControl()
    {
        Icon = SymbolRegular.Keyboard24;
        Title = Resource.WhiteKeyboardBacklightControl_Title;
        Subtitle = Resource.WhiteKeyboardBacklightControl_Message;

        _listener.Changed += ListenerChanged;
        Unloaded += (_, _) => _listener.Changed -= ListenerChanged;
    }

    private void ListenerChanged(object? sender, DriverKeyListener.ChangedEventArgs e) => Dispatcher.UIThread.InvokeTask(async () =>
    {
        if (!IsLoaded || !IsVisible)
            return;

        if (e.DriverKey.HasFlag(DriverKey.FnSpace))
            await RefreshAsync();
    }, "refresh white keyboard backlight control");
}

using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

internal class OneLevelWhiteKeyboardBacklightControl : AbstractToggleFeatureCardControl<OneLevelWhiteKeyboardBacklightState>
{
    protected override OneLevelWhiteKeyboardBacklightState OnState => OneLevelWhiteKeyboardBacklightState.On;

    protected override OneLevelWhiteKeyboardBacklightState OffState => OneLevelWhiteKeyboardBacklightState.Off;

    public OneLevelWhiteKeyboardBacklightControl()
    {
        Icon = SymbolRegular.Keyboard24;
        Title = Resource.OneLevelWhiteKeyboardBacklightControl_Title;
        Subtitle = Resource.OneLevelWhiteKeyboardBacklightControl_Message;
    }
}


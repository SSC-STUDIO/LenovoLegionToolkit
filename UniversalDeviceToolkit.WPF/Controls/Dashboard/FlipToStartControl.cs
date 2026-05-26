using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

public class FlipToStartControl : AbstractToggleFeatureCardControl<FlipToStartState>
{
    protected override FlipToStartState OnState => FlipToStartState.On;

    protected override FlipToStartState OffState => FlipToStartState.Off;

    public FlipToStartControl()
    {
        Icon = SymbolRegular.Power24;
        Title = Resource.FlipToStartControl_Title;
        Subtitle = Resource.FlipToStartControl_Message;
    }
}


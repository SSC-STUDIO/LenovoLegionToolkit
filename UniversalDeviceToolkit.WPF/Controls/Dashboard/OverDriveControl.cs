using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

public class OverDriveControl : AbstractToggleFeatureCardControl<OverDriveState>
{
    protected override OverDriveState OnState => OverDriveState.On;

    protected override OverDriveState OffState => OverDriveState.Off;

    public OverDriveControl()
    {
        Icon = SymbolRegular.TopSpeed24;
        Title = Resource.OverDriveControl_Title;
        Subtitle = Resource.OverDriveControl_Message;
    }
}


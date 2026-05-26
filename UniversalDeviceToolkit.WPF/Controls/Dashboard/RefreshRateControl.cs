using System;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Listeners;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

public class RefreshRateControl : AbstractComboBoxFeatureCardControl<RefreshRate>
{
    private readonly DisplayConfigurationListener _listener = IoCContainer.Resolve<DisplayConfigurationListener>();

    public RefreshRateControl()
    {
        Icon = SymbolRegular.DesktopPulse24;
        Title = Resource.RefreshRateControl_Title;
        Subtitle = Resource.RefreshRateControl_Message;

        _listener.Changed += Listener_Changed;
    }

    protected override async Task OnRefreshAsync()
    {
        await base.OnRefreshAsync();

        Visibility = ItemsCount < 2 ? Visibility.Collapsed : Visibility.Visible;
    }

    protected override string ComboBoxItemDisplayName(RefreshRate value)
    {
        var str = base.ComboBoxItemDisplayName(value);
        return LocalizationHelper.ForceLeftToRight(str);
    }

    private void Listener_Changed(object? sender, EventArgs e) => Dispatcher.InvokeTask(async () =>
    {
        if (IsLoaded)
            await RefreshAsync();
    }, "refresh refresh rate control");
}

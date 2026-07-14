using System;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib.Listeners;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;
using DpiScale = LenovoLegionToolkit.Lib.DpiScale;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

public class DpiScaleControl : AbstractComboBoxFeatureCardControl<DpiScale>
{
    private readonly DisplayConfigurationListener _listener = IoCContainer.Resolve<DisplayConfigurationListener>();

    public DpiScaleControl()
    {
        Icon = SymbolRegular.TextFontSize24;
        Title = Resource.DpiScaleControl_Title;
        Subtitle = Resource.DpiScaleControl_Message;

        _listener.Changed += Listener_Changed;
        Unloaded += (_, _) => _listener.Changed -= Listener_Changed;
    }

    protected override async Task OnRefreshAsync()
    {
        await base.OnRefreshAsync();

        Visibility = ItemsCount < 2 ? Visibility.Collapsed : Visibility.Visible;
    }

    protected override string ComboBoxItemDisplayName(DpiScale value)
    {
        var str = base.ComboBoxItemDisplayName(value);
        return LocalizationHelper.ForceLeftToRight(str);
    }

    private void Listener_Changed(object? sender, EventArgs e) => Dispatcher.InvokeTask(async () =>
    {
        if (IsLoaded)
            await RefreshAsync();
    }, "refresh DPI scale control");
}

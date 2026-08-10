using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using DpiScale = UniversalDeviceToolkit.Lib.DpiScale;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard;

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

        IsVisible = ItemsCount < 2 ? false : true;
    }

    protected override string ComboBoxItemDisplayName(DpiScale value)
    {
        var str = base.ComboBoxItemDisplayName(value);
        return LocalizationHelper.ForceLeftToRight(str);
    }

    private void Listener_Changed(object? sender, EventArgs e) => Dispatcher.UIThread.InvokeTask(async () =>
    {
        if (IsLoaded)
            await RefreshAsync();
    }, "refresh DPI scale control");
}

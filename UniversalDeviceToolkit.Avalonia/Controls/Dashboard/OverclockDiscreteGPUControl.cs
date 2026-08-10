using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Windows.Dashboard;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Lib.Utils;
using Button = UniversalDeviceToolkit.Avalonia.Controls.Button;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard;

public class OverclockDiscreteGPUControl : AbstractRefreshingControl
{
    private readonly GPUOverclockController _controller = IoCContainer.Resolve<GPUOverclockController>();
    private readonly NativeWindowsMessageListener _nativeWindowsMessageListener = IoCContainer.Resolve<NativeWindowsMessageListener>();

    private readonly CardControl _cardControl = new()
    {
        Icon = new SymbolIcon { Symbol = SymbolRegular.DeveloperBoardLightning20 },
        Margin = new(0, 0, 0, 8)
    };

    private readonly CardHeaderControl _cardHeaderControl = new()
    {
        Title = Resource.OverclockDiscreteGPUControl_Title,
        Subtitle = Resource.OverclockDiscreteGPUControl_Message
    };

    private readonly StackPanel _accessoryStackPanel = new()
    {
        Orientation = Orientation.Horizontal
    };

    private readonly ToggleSwitch _toggle = new()
    {
        IsVisible = false,
        Margin = new(8, 0, 0, 0)
    };

    private readonly Button _configButton = new()
    {
        Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
        FontSize = 20,
        Margin = new(8, 0, 0, 0)
    };

    public OverclockDiscreteGPUControl()
    {
        InitializeComponent();

        _nativeWindowsMessageListener.Changed += NativeWindowsMessageListener_Changed;
        _controller.Changed += Controller_Changed;
        Unloaded += (_, _) =>
        {
            _nativeWindowsMessageListener.Changed -= NativeWindowsMessageListener_Changed;
            _controller.Changed -= Controller_Changed;
        };
    }

    private void InitializeComponent()
    {
        AutomationProperties.SetName(_toggle, Resource.OverclockDiscreteGPUControl_Title);
        AutomationProperties.SetName(_configButton, Resource.OverclockDiscreteGPUControl_Title);
        AutomationProperties.SetHelpText(_configButton, Resource.Settings);

        _toggle.Click += Toggle_Click;
        _configButton.Click += ConfigButton_Click;

        _accessoryStackPanel.Children.Add(_toggle);
        _accessoryStackPanel.Children.Add(_configButton);

        _cardHeaderControl.Accessory = _accessoryStackPanel;
        _cardControl.Header = _cardHeaderControl;

        Content = _cardControl;
    }

    protected override void OnFinishedLoading() { }

    protected override async Task OnRefreshAsync()
    {
        if (!await _controller.IsSupportedAsync())
            throw new NotSupportedException();

        var (enabled, _) = _controller.GetState();
        _toggle.IsChecked = enabled;
        _toggle.IsVisible = true;
    }

    private async void NativeWindowsMessageListener_Changed(object? sender, NativeWindowsMessageListener.ChangedEventArgs e)
    {
        try
        {
            if (e.Message != NativeWindowsMessage.OnDisplayDeviceArrival)
                return;

            IsVisible = true;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(NativeWindowsMessageListener_Changed)}.", ex);
        }
    }

    private void Controller_Changed(object? sender, EventArgs e) => Dispatcher.UIThread.InvokeTask(async () =>
    {
        IsVisible = true;
        await RefreshAsync();
    }, "refresh discrete GPU overclock control");

    private async void Toggle_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (IsRefreshing || _toggle.IsChecked is null)
                return;

            var enabled = _toggle.IsChecked.Value;
            var (_, info) = _controller.GetState();
            _controller.SaveState(enabled, info);
            await _controller.ApplyStateAsync(true);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to change state.", ex);
        }
    }

    private void ConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? UdtAppContext.MainWindow;
        var window = new OverclockDiscreteGPUSettingsWindow();
        window.Closed += async (_, _) => await RefreshAsync();
        window.ShowDialog(owner);
    }
}

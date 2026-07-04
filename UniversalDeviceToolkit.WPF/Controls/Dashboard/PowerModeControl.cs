using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows.Dashboard;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

public class PowerModeControl : AbstractComboBoxFeatureCardControl<PowerModeState>, IDisposable
{
    private readonly ThermalModeListener _thermalModeListener = IoCContainer.Resolve<ThermalModeListener>();
    private readonly PowerModeListener _powerModeListener = IoCContainer.Resolve<PowerModeListener>();

    private readonly ThrottleLastDispatcher _throttleDispatcher = new(TimeSpan.FromMilliseconds(500), nameof(PowerModeControl));
    private readonly StackPanel _accessoryStackPanel = new()
    {
        Orientation = Orientation.Horizontal,
    };

    private readonly Button _configButton = new()
    {
        Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
        FontSize = 20,
        Margin = new(8, 0, 0, 0),
        Visibility = Visibility.Collapsed,
    };

    public PowerModeControl()
    {
        Icon = SymbolRegular.Gauge24;
        Title = Resource.PowerModeControl_Title;
        Subtitle = Resource.PowerModeControl_Message;

        AutomationProperties.SetName(_configButton, Resource.PowerModeControl_Title);
        AutomationProperties.SetHelpText(_configButton, Resource.PowerModeControl_Settings);
        AutomationProperties.SetAutomationId(_configButton, "PowerModeSettingsButton");
        _configButton.Click += ConfigButton_Click;

        _thermalModeListener.Changed += ThermalModeListener_Changed;
        _powerModeListener.Changed += PowerModeListener_Changed;
        Unloaded += (_, _) =>
        {
            _thermalModeListener.Changed -= ThermalModeListener_Changed;
            _powerModeListener.Changed -= PowerModeListener_Changed;

            Dispose();
        };
    }

    public void Dispose()
    {
        _throttleDispatcher.Dispose();
    }

    private async void ThermalModeListener_Changed(object? sender, ThermalModeListener.ChangedEventArgs e) => await _throttleDispatcher.DispatchAsync(async () =>
    {
        await Dispatcher.InvokeTaskAsync(async () =>
        {
            if (IsLoaded && IsVisible)
                await RefreshAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }).ConfigureAwait(false);

    private async void PowerModeListener_Changed(object? sender, PowerModeListener.ChangedEventArgs e) => await _throttleDispatcher.DispatchAsync(async () =>
    {
        await Dispatcher.InvokeTaskAsync(async () =>
        {
            if (IsLoaded && IsVisible)
                await RefreshAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }).ConfigureAwait(false);

    protected override async Task OnRefreshAsync()
    {
        await base.OnRefreshAsync().ConfigureAwait(false);

        await UpdateConfigButtonVisibilityAsync().ConfigureAwait(false);

        if (await Power.IsPowerAdapterConnectedAsync().ConfigureAwait(false) != PowerAdapterStatus.Connected
            && TryGetSelectedItem(out var state)
            && state is PowerModeState.Performance or PowerModeState.GodMode)
            Warning = Resource.PowerModeControl_Warning;
        else
            Warning = string.Empty;
    }

    protected override async Task OnStateChangeAsync(ComboBox comboBox, IFeature<PowerModeState> feature, PowerModeState? newValue, PowerModeState? oldValue)
    {
        await base.OnStateChangeAsync(comboBox, feature, newValue, oldValue).ConfigureAwait(false);

        await UpdateConfigButtonVisibilityAsync().ConfigureAwait(false);
    }

    private async Task UpdateConfigButtonVisibilityAsync()
    {
        if (!TryGetSelectedItem(out var state))
        {
            _configButton.ToolTip = null;
            _configButton.Visibility = Visibility.Collapsed;
            return;
        }

        var mi = await MachineCompatibility.GetMachineInformationAsync().ConfigureAwait(false);

        var shouldShowConfigButton = ShouldShowConfigButton(state, mi);
        _configButton.ToolTip = shouldShowConfigButton ? Resource.PowerModeControl_Settings : null;
        _configButton.Visibility = shouldShowConfigButton ? Visibility.Visible : Visibility.Collapsed;
    }

    internal static bool ShouldShowConfigButton(PowerModeState state, MachineInformation machineInformation) =>
        state switch
        {
            PowerModeState.Balance => machineInformation.Properties.SupportsAIMode,
            PowerModeState.Performance or PowerModeState.GodMode => MachineCompatibility.SupportsGodModeCustomization(machineInformation),
            _ => false
        };

    protected override void OnStateChangeException(Exception exception)
    {
        if (exception is PowerModeUnavailableWithoutACException ex1)
        {
            SnackbarHelper.Show(Resource.PowerModeUnavailableWithoutACException_Title,
                string.Format(Resource.PowerModeUnavailableWithoutACException_Message, ex1.PowerMode.GetDisplayName()),
                SnackbarType.Warning);
        }
    }

    protected override FrameworkElement GetAccessory(ComboBox comboBox)
    {
        AutomationProperties.SetAutomationId(comboBox, "PowerModeControl_ComboBox");

        if (_accessoryStackPanel.Children.Count == 0)
        {
            Panel.SetZIndex(_configButton, 1);
            _accessoryStackPanel.Children.Add(comboBox);
            _accessoryStackPanel.Children.Add(_configButton);
        }

        return _accessoryStackPanel;
    }

    private void ConfigButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedItem(out var state))
            return;

        switch (state)
        {
            case PowerModeState.Balance:
                {
                    var window = new BalanceModeSettingsWindow { Owner = Window.GetWindow(this) };
                    window.ShowDialog();
                    break;
                }
            case PowerModeState.Performance:
            case PowerModeState.GodMode:
                {
                    var window = new GodModeSettingsWindow(state) { Owner = Window.GetWindow(this) };
                    window.ShowDialog();
                    break;
                }
        }
    }
}

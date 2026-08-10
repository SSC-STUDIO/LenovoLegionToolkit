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
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Lib.System;
using UniversalDeviceToolkit.Lib.Utils;
using Button = UniversalDeviceToolkit.Avalonia.Controls.Button;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard;

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
        IsVisible = false,
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

    private async void ThermalModeListener_Changed(object? sender, ThermalModeListener.ChangedEventArgs e)
    {
        try
        {
            await _throttleDispatcher.DispatchAsync(async () =>
            {
                await Dispatcher.UIThread.InvokeTaskAsync(async () =>
                {
                    if (IsLoaded && IsVisible)
                        await RefreshAsync();
                });
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(ThermalModeListener_Changed)}.", ex);
        }
    }

    private async void PowerModeListener_Changed(object? sender, PowerModeListener.ChangedEventArgs e)
    {
        try
        {
            await _throttleDispatcher.DispatchAsync(async () =>
            {
                await Dispatcher.UIThread.InvokeTaskAsync(async () =>
                {
                    if (IsLoaded && IsVisible)
                        await RefreshAsync();
                });
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(PowerModeListener_Changed)}.", ex);
        }
    }

    protected override async Task OnRefreshAsync()
    {
        await base.OnRefreshAsync();

        await UpdateConfigButtonVisibilityAsync();

        if (await Power.IsPowerAdapterConnectedAsync() == PowerAdapterStatus.Disconnected
            && TryGetSelectedItem(out var state)
            && state is PowerModeState.Performance or PowerModeState.GodMode)
            Warning = Resource.PowerModeControl_Warning;
        else
            Warning = string.Empty;
    }

    protected override async Task OnStateChangeAsync(ComboBox comboBox, IFeature<PowerModeState> feature, PowerModeState? newValue, PowerModeState? oldValue)
    {
        await base.OnStateChangeAsync(comboBox, feature, newValue, oldValue);

        await UpdateConfigButtonVisibilityAsync();
    }

    private async Task UpdateConfigButtonVisibilityAsync()
    {
        if (!TryGetSelectedItem(out var state))
        {
            ToolTip.SetTip(_configButton, null);
            _configButton.IsVisible = false;
            return;
        }

        var mi = await MachineCompatibility.GetMachineInformationAsync();

        var shouldShowConfigButton = ShouldShowConfigButton(state, mi);
        ToolTip.SetTip(_configButton, shouldShowConfigButton ? Resource.PowerModeControl_Settings : null);
        _configButton.IsVisible = shouldShowConfigButton ? true : false;
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
        else
        {
            // Any other failure silently reverted the combo selection — tell the user why.
            SnackbarHelper.Show(Resource.PowerModeControl_Title, Resource.PowerModeControl_SwitchFailed_Message, SnackbarType.Warning);
        }
    }

    protected override Control GetAccessory(ComboBox comboBox)
    {
        AutomationProperties.SetAutomationId(comboBox, "PowerModeControl_ComboBox");

        if (_accessoryStackPanel.Children.Count == 0)
        {
            _configButton.ZIndex = 1;
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
                    var owner = TopLevel.GetTopLevel(this) as Window ?? UdtAppContext.MainWindow;
                    var window = new BalanceModeSettingsWindow();
                    window.ShowDialog(owner);
                    break;
                }
            case PowerModeState.Performance:
            case PowerModeState.GodMode:
                {
                    var owner = TopLevel.GetTopLevel(this) as Window ?? UdtAppContext.MainWindow;
                    var window = new GodModeSettingsWindow(state);
                    window.ShowDialog(owner);
                    break;
                }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using UniversalDeviceToolkit.Lib.Automation.Pipeline.Triggers;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;

namespace UniversalDeviceToolkit.Avalonia.Windows.Automation
{
public partial class CreateAutomationPipelineWindow : global::UniversalDeviceToolkit.Avalonia.Windows.BaseWindow
{
    private readonly IAutomationPipelineTrigger[] _triggers =
    [
        new ACAdapterConnectedAutomationPipelineTrigger(),
        new LowWattageACAdapterConnectedAutomationPipelineTrigger(),
        new ACAdapterDisconnectedAutomationPipelineTrigger(),
        new PowerModeAutomationPipelineTrigger(PowerModeState.Balance),
        new GodModePresetChangedAutomationPipelineTrigger(Guid.Empty),
        new GamesAreRunningAutomationPipelineTrigger(),
        new GamesStopAutomationPipelineTrigger(),
        new ProcessesAreRunningAutomationPipelineTrigger([]),
        new ProcessesStopRunningAutomationPipelineTrigger([]),
        new UserInactivityAutomationPipelineTrigger(TimeSpan.Zero),
        new UserInactivityAutomationPipelineTrigger(TimeSpan.FromMinutes(1)),
        new SessionLockAutomationPipelineTrigger(),
        new SessionUnlockAutomationPipelineTrigger(),
        new LidOpenedAutomationPipelineTrigger(),
        new LidClosedAutomationPipelineTrigger(),
        new DisplayOnAutomationPipelineTrigger(),
        new DisplayOffAutomationPipelineTrigger(),
        new HDROnAutomationPipelineTrigger(),
        new HDROffAutomationPipelineTrigger(),
        new DeviceConnectedAutomationPipelineTrigger([]),
        new DeviceDisconnectedAutomationPipelineTrigger([]),
        new ExternalDisplayConnectedAutomationPipelineTrigger(),
        new ExternalDisplayDisconnectedAutomationPipelineTrigger(),
        new WiFiConnectedAutomationPipelineTrigger([]),
        new WiFiDisconnectedAutomationPipelineTrigger(),
        new TimeAutomationPipelineTrigger(false, false, TimeExtensions.UtcNow, Enum.GetValues<DayOfWeek>()),
        new PeriodicAutomationPipelineTrigger(TimeSpan.FromMinutes(1)),
        new HardwareSensorAutomationPipelineTrigger(HardwareSensorMetric.CpuTemperature, HardwareSensorComparison.GreaterThanOrEqual, 90f, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1)),
        new BatteryPercentageAutomationPipelineTrigger(BatteryPercentageComparison.BelowOrEqual, 20, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5), BatteryChargeFilter.Any),
        new OnStartupAutomationPipelineTrigger(),
        new OnResumeAutomationPipelineTrigger()
    ];

    private readonly HashSet<Type> _existingTriggerTypes;
    private readonly Action<IAutomationPipelineTrigger> _createPipeline;

    private bool _multiSelect;

    public CreateAutomationPipelineWindow(HashSet<Type> existingTriggerTypes,
        Action<IAutomationPipelineTrigger> createPipeline)
    {
        _existingTriggerTypes = existingTriggerTypes;
        _createPipeline = createPipeline;

        InitializeComponent();

        PropertyChanged += CreateAutomationPipelineWindow_PropertyChanged;
    }

    private async void CreateAutomationPipelineWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Visual.IsVisibleProperty)
            return;

        try
        {
            if (IsVisible)
                await RefreshAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(CreateAutomationPipelineWindow_PropertyChanged)}.", ex);
        }
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        var triggers = System.Linq.Enumerable.ToArray(_content.Children)
            .OfType<CardControl>()
            .Select(c => c.Header)
            .OfType<CardHeaderControl>()
            .Select(c => c.Accessory)
            .OfType<CheckBox>()
            .Where(c => c.IsChecked ?? false)
            .Select(c => c.Tag)
            .OfType<IAutomationPipelineTrigger>()
            .ToArray();

        if (triggers.IsEmpty())
            return;

        var trigger = triggers.Length == 1 ? triggers[0] : new AndAutomationPipelineTrigger(triggers);
        _createPipeline(trigger);

        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    private Task RefreshAsync()
    {
        _content.Children.Clear();

        if (!_multiSelect)
            _content.Children.Add(CreateMultipleSelectCardControl());

        foreach (var trigger in _triggers)
            _content.Children.Add(CreateCardControl(trigger));

        _createButton.IsEnabled = false;
        _createButton.IsVisible = _multiSelect ? true : false;

        return Task.CompletedTask;
    }

    private CardControl CreateMultipleSelectCardControl()
    {
        var control = new CardControl
        {
            Icon = new SymbolIcon { Symbol = SymbolRegular.SquareMultiple24 },
            Header = new CardHeaderControl
            {
                Title = Resource.MultipleTriggersAutomationPipelineTrigger_DisplayName,
                Accessory = new SymbolIcon { Symbol = SymbolRegular.ChevronRight24 }
            },
            Margin = new(0, 8, 0, 0),
        };

        control.Click += MultipleSelectCardControl_Click;

        return control;
    }

    private async void MultipleSelectCardControl_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            _multiSelect = true;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(MultipleSelectCardControl_Click)}.", ex);
        }
    }

    private CardControl CreateCardControl(IAutomationPipelineTrigger trigger)
    {
        Control accessory;

        if (_multiSelect)
        {
            var checkbox = new CheckBox
            {
                Tag = trigger,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            checkbox.Click += (_, e) =>
            {
                RefreshCreateButton();
                e.Handled = true;
            };
            accessory = checkbox;
        }
        else
        {
            accessory = new SymbolIcon { Symbol = SymbolRegular.ChevronRight24 };
        }

        var control = new CardControl
        {
            Icon = new SymbolIcon { Symbol = trigger.Icon() },
            Header = new CardHeaderControl
            {
                Title = trigger.DisplayName,
                Accessory = accessory
            },
            Margin = new(0, 8, 0, 0),
        };

        if (!_multiSelect && trigger is IDisallowDuplicatesAutomationPipelineTrigger)
            control.IsEnabled = !_existingTriggerTypes.Contains(trigger.GetType());

        control.Click += (_, _) =>
        {
            if (_multiSelect)
            {
                if (accessory is not CheckBox checkbox)
                    return;

                var isChecked = checkbox.IsChecked ?? false;
                checkbox.IsChecked = !isChecked;
                RefreshCreateButton();
            }
            else
            {
                _createPipeline(trigger);
                Close();
            }
        };

        return control;
    }

    private void RefreshCreateButton()
    {
        if (!_multiSelect)
        {
            _createButton.IsEnabled = false;
            return;
        }

        var anyChecked = System.Linq.Enumerable.ToArray(_content.Children)
            .OfType<CardControl>()
            .Select(c => c.Header)
            .OfType<CardHeaderControl>()
            .Select(c => c.Accessory)
            .OfType<CheckBox>()
            .Any(c => c.IsChecked ?? false);

        _createButton.IsEnabled = anyChecked;
    }
}
}

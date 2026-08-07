#if WINDOWS
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Pages.Windows;

/// <summary>
/// Native Avalonia editor for the per-device-state Windows power mappings that
/// are configured by the WPF WindowsPowerModesWindow and WindowsPowerPlansWindow.
/// </summary>
internal sealed class PowerMappingSettingsWindow : Window
{
    private readonly PowerMappingKind _kind;
    private readonly ApplicationSettings _settings;
    private readonly PowerModeFeature _powerModeFeature;
    private readonly WindowsPowerPlanController? _powerPlanController;
    private readonly StackPanel _rows = new() { Spacing = 8 };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private bool _isLoading;

    public PowerMappingSettingsWindow(PowerMappingKind kind)
    {
        _kind = kind;
        _settings = IoCContainer.Resolve<ApplicationSettings>();
        _powerModeFeature = IoCContainer.Resolve<PowerModeFeature>();
        _powerPlanController = kind == PowerMappingKind.WindowsPowerPlan
            ? IoCContainer.Resolve<WindowsPowerPlanController>()
            : null;

        Title = kind == PowerMappingKind.WindowsPowerMode
            ? Get("WindowsPowerModesWindow_Title", "Windows power modes")
            : Get("WindowsPowerPlansWindow_Title", "Windows power plans");
        Width = 560;
        MinWidth = 480;
        MaxWidth = 760;
        MinHeight = 340;
        MaxHeight = 760;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var automationId = kind == PowerMappingKind.WindowsPowerMode
            ? "AvaloniaWindowsPowerModesWindow"
            : "AvaloniaWindowsPowerPlansWindow";
        AutomationProperties.SetAutomationId(this, automationId);
        AutomationProperties.SetName(this, Title);

        var title = new LocalizedTextBlock
        {
            Text = Title,
            FontSize = 20,
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var description = new LocalizedTextBlock
        {
            Text = kind == PowerMappingKind.WindowsPowerMode
                ? Get("SettingsPage_WindowsPowerModes_Message", "Map device power modes to Windows power modes.")
                : Get("SettingsPage_WindowsPowerPlans_Message", "Map device power modes to Windows power plans."),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        };
        _status.Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush");
        AutomationProperties.SetAutomationId(_status, "AvaloniaPowerMappingStatusText");

        var content = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(24),
            FlowDirection = LocalizationCatalog.IsRightToLeft(LocalizationRuntime.CurrentCulture)
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight,
            Children = { title, description, _status, _rows },
        };
        Content = new ScrollViewer { Content = content };
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task LoadAsync()
    {
        _isLoading = true;
        try
        {
            _rows.Children.Clear();
            var states = (await _powerModeFeature.GetAllStatesAsync().ConfigureAwait(true)).ToHashSet();
            var choices = _kind == PowerMappingKind.WindowsPowerMode
                ? CreatePowerModeChoices()
                : CreatePowerPlanChoices();

            foreach (var state in new[]
                     {
                         PowerModeState.Quiet,
                         PowerModeState.Balance,
                         PowerModeState.Performance,
                         PowerModeState.GodMode,
                     })
            {
                if (state == PowerModeState.GodMode && !states.Contains(state))
                    continue;

                AddMappingRow(state, choices);
            }

            _status.Text = Get("Settings_Page_StatusMessage", "Changes are saved immediately.");
            _status.Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush");
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            _status.Foreground = GetResource<IBrush>("StatusWarningBrush");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private IReadOnlyList<PowerMappingChoice> CreatePowerModeChoices() =>
        Enum.GetValues<WindowsPowerMode>()
            .Select(mode => new PowerMappingChoice(mode.GetDisplayName(), mode, null))
            .ToArray();

    private IReadOnlyList<PowerMappingChoice> CreatePowerPlanChoices()
    {
        var defaultPlan = new PowerMappingChoice(
            Get("WindowsPowerPlansWindow_DefaultPowerPlan", "Windows default power plan"),
            null,
            Guid.Empty);
        var plans = _powerPlanController!
            .GetPowerPlans()
            .OrderBy(plan => plan.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(plan => new PowerMappingChoice(plan.Name, null, plan.Guid));
        return [defaultPlan, .. plans];
    }

    private void AddMappingRow(PowerModeState state, IReadOnlyList<PowerMappingChoice> choices)
    {
        var current = _kind == PowerMappingKind.WindowsPowerMode
            ? choices.FirstOrDefault(choice => choice.PowerMode == _settings.Store.PowerModes.GetValueOrDefault(state, WindowsPowerMode.Balanced))
            : choices.FirstOrDefault(choice => choice.PowerPlanId == _settings.Store.PowerPlans.GetValueOrDefault(state));

        var title = new LocalizedTextBlock
        {
            Text = state.GetDisplayName(),
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
            MinWidth = 0,
        };
        var comboBox = new ComboBox
        {
            ItemsSource = choices,
            SelectedItem = current ?? choices.FirstOrDefault(),
            IsEnabled = choices.Count > 0,
            MinWidth = 200,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        AutomationProperties.SetAutomationId(comboBox, $"AvaloniaPowerMapping_{_kind}_{state}");
        AutomationProperties.SetName(comboBox, state.GetDisplayName());
        ToolTip.SetTip(comboBox, state.GetDisplayName());
        comboBox.SelectionChanged += async (_, _) =>
        {
            if (_isLoading || comboBox.SelectedItem is not PowerMappingChoice selected)
                return;

            await PersistMappingAsync(state, selected, comboBox).ConfigureAwait(true);
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 };
        grid.Children.Add(title);
        Grid.SetColumn(comboBox, 1);
        grid.Children.Add(comboBox);

        _rows.Children.Add(new Border
        {
            Background = GetResource<IBrush>("CardBackgroundBrush"),
            BorderBrush = GetResource<IBrush>("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetCornerRadius(),
            Padding = new Thickness(16),
            Child = grid,
        });
    }

    private async Task PersistMappingAsync(PowerModeState state, PowerMappingChoice selected, ComboBox comboBox)
    {
        var previousMode = _settings.Store.PowerModes.GetValueOrDefault(state, WindowsPowerMode.Balanced);
        var previousPlan = _settings.Store.PowerPlans.GetValueOrDefault(state);
        comboBox.IsEnabled = false;
        try
        {
            if (_kind == PowerMappingKind.WindowsPowerMode && selected.PowerMode is { } mode)
                _settings.Store.PowerModes[state] = mode;
            else if (_kind == PowerMappingKind.WindowsPowerPlan && selected.PowerPlanId is { } planId)
                _settings.Store.PowerPlans[state] = planId;
            else
                return;

            _settings.SynchronizeStore();
            await _powerModeFeature.EnsureCorrectWindowsPowerSettingsAreSetAsync().ConfigureAwait(true);
            _status.Text = Get("Settings_Page_StatusMessage", "Changes are saved immediately.");
            _status.Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush");
        }
        catch (Exception ex)
        {
            if (_kind == PowerMappingKind.WindowsPowerMode)
                _settings.Store.PowerModes[state] = previousMode;
            else
                _settings.Store.PowerPlans[state] = previousPlan;
            _settings.SynchronizeStore();

            _isLoading = true;
            comboBox.SelectedItem = _kind == PowerMappingKind.WindowsPowerMode
                ? ((IReadOnlyList<PowerMappingChoice>)comboBox.ItemsSource!).FirstOrDefault(choice => choice.PowerMode == previousMode)
                : ((IReadOnlyList<PowerMappingChoice>)comboBox.ItemsSource!).FirstOrDefault(choice => choice.PowerPlanId == previousPlan);
            _isLoading = false;
            _status.Text = ex.Message;
            _status.Foreground = GetResource<IBrush>("StatusWarningBrush");
        }
        finally
        {
            comboBox.IsEnabled = true;
        }
    }

    private static string Get(string key, string fallback) => AvaloniaLocalization.GetString(key, fallback);

    private static T GetResource<T>(string key)
        where T : class =>
        Application.Current?.TryGetResource(key, out var value) == true && value is T resource
            ? resource
            : (T)(object)new SolidColorBrush(Colors.Gray);

    private static CornerRadius GetCornerRadius() =>
        Application.Current?.TryGetResource("CornerRadiusCard", out var value) == true
        && value is CornerRadius cornerRadius
            ? cornerRadius
            : new CornerRadius(8);

    private sealed record PowerMappingChoice(string DisplayName, WindowsPowerMode? PowerMode, Guid? PowerPlanId)
    {
        public override string ToString() => DisplayName;
    }
}

internal enum PowerMappingKind
{
    WindowsPowerMode,
    WindowsPowerPlan,
}
#endif

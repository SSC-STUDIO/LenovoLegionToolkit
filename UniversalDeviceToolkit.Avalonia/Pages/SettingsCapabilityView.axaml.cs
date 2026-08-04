using Avalonia.Automation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class SettingsCapabilityView : UserControl
{
    private readonly string _pageKey;
    private readonly IAvaloniaSettingsService _settingsService;
    private bool _isApplying;

    protected SettingsCapabilityView(string pageKey, string titleFallback, string descriptionFallback)
    {
        _pageKey = pageKey;
        _settingsService = AvaloniaSettingsServiceFactory.Create();
        InitializeComponent();

        TitleBlock.Text = titleFallback;
        DescriptionBlock.Text = descriptionFallback;
        StatusTitleBlock.Text = AvaloniaLocalization.GetString(
            "Settings_PlatformUnavailable_Title",
            "Settings availability");
        AutomationProperties.SetName(this, titleFallback);
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
            return;

        control.Loaded -= OnLoaded;
        try
        {
            _isApplying = true;
            var data = await _settingsService.GetPageAsync(_pageKey);
            TitleBlock.Text = data.Title;
            DescriptionBlock.Text = data.Description;
            AutomationProperties.SetName(this, data.Title);

            AvailabilityCard.IsVisible = !data.IsAvailable || !string.IsNullOrWhiteSpace(data.UnavailableReason);
            StatusTitleBlock.Text = data.IsAvailable
                ? AvaloniaLocalization.GetString("Settings_Page_StatusTitle", "Settings ready")
                : AvaloniaLocalization.GetString("Settings_PlatformUnavailable_Title", "Unavailable in this host");
            StatusMessageBlock.Text = data.UnavailableReason
                ?? AvaloniaLocalization.GetString("Settings_Page_StatusMessage", "Changes are saved immediately.");

            OptionsPanel.Children.Clear();
            foreach (var option in data.Options)
                OptionsPanel.Children.Add(CreateOptionCard(option));

            if (data.Options.Count == 0)
            {
                OptionsPanel.Children.Add(CreateEmptyState(data.IsAvailable));
            }
        }
        catch (Exception ex)
        {
            AvailabilityCard.IsVisible = true;
            StatusTitleBlock.Text = AvaloniaLocalization.GetString("Settings_Page_LoadFailed", "Unable to load settings");
            StatusMessageBlock.Text = ex.Message;
        }
        finally
        {
            _isApplying = false;
        }
    }

    private Control CreateOptionCard(AvaloniaSettingOption option)
    {
        var title = new Controls.LocalizedTextBlock
        {
            Text = option.Title,
            FontSize = (double)Resources["FontSizeBody"]!,
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var description = new Controls.LocalizedTextBlock
        {
            Text = option.Description,
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        };
        var textStack = new StackPanel { Spacing = 3, MinWidth = 0 };
        textStack.Children.Add(title);
        textStack.Children.Add(description);

        Control editor = option.Editor switch
        {
            AvaloniaSettingEditor.Toggle => CreateToggle(option),
            AvaloniaSettingEditor.Selection => CreateSelection(option),
            AvaloniaSettingEditor.MultiSelection => CreateMultiSelection(option),
            AvaloniaSettingEditor.Text => CreateTextBox(option),
            AvaloniaSettingEditor.Action => CreateAction(option),
            _ => new TextBlock { Text = option.Title },
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 };
        grid.Children.Add(textStack);
        Grid.SetColumn(editor, 1);
        grid.Children.Add(editor);

        var card = new Border
        {
            Background = GetResource<IBrush>("CardBackgroundBrush"),
            BorderBrush = GetResource<IBrush>("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = (CornerRadius)Resources["CornerRadiusCard"]!,
            Padding = new Thickness(16),
            Child = grid,
        };
        if (!string.IsNullOrWhiteSpace(option.Warning))
            ToolTip.SetTip(card, option.Warning);
        AutomationProperties.SetName(card, option.Title);
        return card;
    }

    private CheckBox CreateToggle(AvaloniaSettingOption option)
    {
        var toggle = new CheckBox
        {
            IsChecked = option.BoolValue,
            IsEnabled = option.IsEnabled,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 48,
        };
        AutomationProperties.SetAutomationId(toggle, $"AvaloniaSettings_{_pageKey}_{option.Key}");
        AutomationProperties.SetName(toggle, option.Title);
        ToolTip.SetTip(toggle, option.Description);
        toggle.IsCheckedChanged += async (_, _) =>
        {
            if (_isApplying || toggle.IsChecked is not bool value)
                return;
            if (!await PersistAsync(() => _settingsService.SetToggleAsync(_pageKey, option.Key, value), toggle))
            {
                _isApplying = true;
                toggle.IsChecked = option.BoolValue;
                _isApplying = false;
            }
        };
        return toggle;
    }

    private ComboBox CreateSelection(AvaloniaSettingOption option)
    {
        var combo = new ComboBox
        {
            ItemsSource = option.Values ?? [],
            SelectedItem = option.SelectedValue,
            IsEnabled = option.IsEnabled,
            MinWidth = 180,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        AutomationProperties.SetAutomationId(combo, $"AvaloniaSettings_{_pageKey}_{option.Key}");
        AutomationProperties.SetName(combo, option.Title);
        ToolTip.SetTip(combo, option.Description);
        combo.SelectionChanged += async (_, _) =>
        {
            if (_isApplying || combo.SelectedItem is not string value)
                return;
            if (!await PersistAsync(() => _settingsService.SetSelectionAsync(_pageKey, option.Key, value), combo))
            {
                _isApplying = true;
                combo.SelectedItem = option.SelectedValue;
                _isApplying = false;
            }
        };
        return combo;
    }

    private TextBox CreateTextBox(AvaloniaSettingOption option)
    {
        var textBox = new TextBox
        {
            Text = option.TextValue ?? string.Empty,
            IsEnabled = option.IsEnabled,
            MinWidth = 240,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        AutomationProperties.SetAutomationId(textBox, $"AvaloniaSettings_{_pageKey}_{option.Key}");
        AutomationProperties.SetName(textBox, option.Title);
        ToolTip.SetTip(textBox, option.Description);
        textBox.LostFocus += async (_, _) =>
        {
            if (_isApplying)
                return;
            if (!await PersistAsync(() => _settingsService.SetTextAsync(_pageKey, option.Key, textBox.Text), textBox))
            {
                _isApplying = true;
                textBox.Text = option.TextValue ?? string.Empty;
                _isApplying = false;
            }
        };
        return textBox;
    }

    private Control CreateMultiSelection(AvaloniaSettingOption option)
    {
        var selected = new HashSet<string>(option.SelectedValues ?? [], StringComparer.Ordinal);
        if (selected.Count == 0 && !string.IsNullOrWhiteSpace(option.SelectedValue))
            selected.Add(option.SelectedValue);

        var checkBoxes = new List<CheckBox>();
        var panel = new StackPanel
        {
            Spacing = 4,
            MinWidth = 220,
            MaxWidth = 360,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        foreach (var value in option.Values ?? [])
        {
            var checkBox = new CheckBox
            {
                Content = value,
                IsChecked = selected.Contains(value),
                IsEnabled = option.IsEnabled,
                HorizontalContentAlignment = HorizontalAlignment.Left,
            };
            AutomationProperties.SetName(checkBox, value);
            ToolTip.SetTip(checkBox, option.Description);
            checkBox.IsCheckedChanged += async (_, _) =>
            {
                if (_isApplying || checkBox.IsChecked is not bool isChecked)
                    return;

                var previous = selected.ToArray();
                if (isChecked && value.Equals(option.Values?.FirstOrDefault(), StringComparison.Ordinal))
                {
                    selected.Clear();
                    selected.Add(value);
                    _isApplying = true;
                    foreach (var sibling in checkBoxes.Where(sibling => !ReferenceEquals(sibling, checkBox)))
                        sibling.IsChecked = false;
                    _isApplying = false;
                }
                else if (isChecked)
                {
                    selected.Remove(option.Values?.FirstOrDefault() ?? string.Empty);
                    selected.Add(value);
                }
                else
                    selected.Remove(value);

                if (!await PersistAsync(
                    () => _settingsService.SetMultiSelectionAsync(_pageKey, option.Key, selected.ToArray()),
                    checkBox))
                {
                    selected.Clear();
                    foreach (var previousValue in previous)
                        selected.Add(previousValue);
                    _isApplying = true;
                    foreach (var sibling in checkBoxes)
                        sibling.IsChecked = selected.Contains(sibling.Content?.ToString() ?? string.Empty);
                    _isApplying = false;
                }
            };
            checkBoxes.Add(checkBox);
            panel.Children.Add(checkBox);
        }

        AutomationProperties.SetName(panel, option.Title);
        ToolTip.SetTip(panel, option.Description);
        return panel;
    }

    private Button CreateAction(AvaloniaSettingOption option)
    {
        var button = new Button
        {
            Content = option.ActionText ?? option.Title,
            IsEnabled = option.IsEnabled,
            MinWidth = 180,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        AutomationProperties.SetAutomationId(button, $"AvaloniaSettings_{_pageKey}_{option.Key}");
        AutomationProperties.SetName(button, option.Title);
        ToolTip.SetTip(button, option.Warning ?? option.Description);
        button.Click += async (_, _) =>
        {
            if (_isApplying)
                return;
            await PersistAsync(
                () => _settingsService.InvokeActionAsync(_pageKey, option.Key),
                button);
        };
        return button;
    }

    private Control CreateEmptyState(bool isAvailable) => new Border
    {
        Background = GetResource<IBrush>("CardBackgroundBrush"),
        BorderBrush = GetResource<IBrush>("CardBorderBrush"),
        BorderThickness = new Thickness(1),
        CornerRadius = (CornerRadius)Resources["CornerRadiusCard"]!,
        Padding = new Thickness(16),
        Child = new Controls.LocalizedTextBlock
        {
            Text = isAvailable
                ? AvaloniaLocalization.GetString("Settings_Page_NoOptions", "No settings are available for this page.")
                : AvaloniaLocalization.GetString("Settings_Page_UnavailableDetail", "The controls remain visible when the adapter is available on this platform."),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        },
    };

    private async Task<bool> PersistAsync(Func<Task> action, Control control)
    {
        try
        {
            await action();
            return true;
        }
        catch (Exception ex)
        {
            ToolTip.SetTip(control, ex.Message);
            return false;
        }
    }

    private T GetResource<T>(object key) where T : class
    {
        if (this.TryFindResource(key, out var resource) && resource is T typedValue)
            return typedValue;

        if (typeof(T) == typeof(IBrush))
            return (T)(object)new SolidColorBrush(Colors.Transparent);

        throw new InvalidOperationException($"Missing Avalonia resource '{key}'.");
    }
}

public sealed class SettingsSmartKeysView() : SettingsCapabilityView(
    "SmartKeys",
    "Smart Keys",
    "Configure Fn-lock and Smart Key behavior.");

public sealed class SettingsUpdateView() : SettingsCapabilityView(
    "Update",
    "Update",
    "Choose how Universal Device Toolkit checks for new releases.");

public sealed class SettingsPowerView() : SettingsCapabilityView(
    "Power",
    "Power",
    "Configure power mode mapping and battery behavior.");

public sealed class SettingsIntegrationsView() : SettingsCapabilityView(
    "Integrations",
    "Integrations",
    "Connect Universal Device Toolkit to supported external tools and services.");

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages.Windows;

/// <summary>
/// Focused OSD editor that reuses the Application settings contract while
/// keeping the dense OSD controls out of the main settings page.
/// </summary>
public sealed class OsdSettingsWindow : Window
{
    private static readonly string[] GeneralKeys =
    [
        "ShowOsd",
        "OsdStyle",
        "OsdRefreshInterval",
        "OsdSnapThreshold",
        "OsdLockPosition",
        "OsdResetPosition",
    ];

    private static readonly string[] AppearanceKeys =
    [
        "OsdOpacity",
        "OsdCornerRadiusTop",
        "OsdCornerRadiusBottom",
        "OsdFontSize",
        "OsdBackgroundColor",
        "OsdCategoryColor",
        "OsdLabelColor",
        "OsdValueColor",
        "OsdWarningColor",
        "OsdCriticalColor",
        "OsdSeparatorColor",
    ];

    private static readonly string[] SensorItemKeys = ["OsdItems"];

    private static readonly string[] ThresholdKeys =
    [
        "OsdTempWarning",
        "OsdTempCritical",
        "OsdUsageWarning",
        "OsdUsageCritical",
        "OsdFpsCritical",
        "OsdLowFpsDelta",
    ];

    private readonly IAvaloniaSettingsService _settingsService;
    private readonly StackPanel _groupsPanel = new() { Spacing = 14 };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button _saveButton;
    private bool _isApplying;
    private bool _isLoaded;
    private bool _isSaving;

    public OsdSettingsWindow(IAvaloniaSettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        Title = Get("OsdWindow_Title", "On-screen display");
        Width = 620;
        Height = 720;
        MinWidth = 480;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        AutomationProperties.SetAutomationId(this, "AvaloniaOsdSettingsWindow");
        AutomationProperties.SetName(this, Title);
        _status.Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush");
        AutomationProperties.SetAutomationId(_status, "AvaloniaOsdSettingsStatusText");

        _saveButton = ActionButton(
            Get("Save", "Save"),
            "AvaloniaOsdSettingsSaveButton",
            SaveAndCloseAsync);
        _saveButton.IsEnabled = false;

        var closeButton = ActionButton(
            Get("Close", "Close"),
            "AvaloniaOsdSettingsCloseButton",
            () =>
            {
                Close(false);
                return Task.CompletedTask;
            });

        var content = new StackPanel
        {
            Spacing = 14,
            Margin = new Thickness(24),
            FlowDirection = LocalizationCatalog.IsRightToLeft(LocalizationRuntime.CurrentCulture)
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight,
        };
        content.Children.Add(new LocalizedTextBlock
        {
            Text = Get("OsdWindow_Title", "On-screen display"),
            FontSize = 22,
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        });
        content.Children.Add(new LocalizedTextBlock
        {
            Text = Get("SettingsPage_Osd_Message", "Configure the on-screen display and sensor thresholds."),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        });
        content.Children.Add(_groupsPanel);
        content.Children.Add(_status);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { closeButton, _saveButton },
        };
        content.Children.Add(buttons);

        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = content,
        };
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_isLoaded)
            return;

        _isLoaded = true;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task LoadAsync()
    {
        try
        {
            _isApplying = true;
            var page = await _settingsService.GetPageAsync("Application").ConfigureAwait(true);
            var options = page.Options
                .Where(option => option.IsVisible
                    && (option.Key.Equals("ShowOsd", StringComparison.Ordinal)
                        || option.Key.StartsWith("Osd", StringComparison.Ordinal)))
                .ToDictionary(option => option.Key, StringComparer.Ordinal);

            _groupsPanel.Children.Clear();
            var added = new HashSet<string>(StringComparer.Ordinal);
            AddGroup(
                Get("OsdSettings_Tabs_General", "General"),
                GeneralKeys,
                options,
                added);
            AddGroup(
                Get("OsdSettings_Tabs_Appearance", "Appearance"),
                AppearanceKeys,
                options,
                added);
            AddGroup(
                Get("OsdSettings_Tabs_SensorItems", "Sensor items"),
                SensorItemKeys,
                options,
                added);
            AddGroup(
                Get("OsdSettings_Tabs_Thresholds", "Thresholds"),
                ThresholdKeys,
                options,
                added);

            var remaining = options.Values
                .Where(option => !added.Contains(option.Key))
                .ToArray();
            if (remaining.Length > 0)
                AddGroup(Get("Settings", "Settings"), remaining.Select(option => option.Key).ToArray(), options, added);

            if (_groupsPanel.Children.Count == 0)
            {
                _groupsPanel.Children.Add(new LocalizedTextBlock
                {
                    Text = page.UnavailableReason
                        ?? Get("Settings_Page_NoOptions", "No OSD settings are available."),
                    Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
                    OverflowMode = LocalizedOverflowMode.Wrap,
                    MaxLines = 4,
                });
            }

            _status.Text = page.UnavailableReason
                ?? Get("Settings_Page_StatusMessage", "Changes are saved immediately. Select Save to close.");
            _status.Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush");
            _saveButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            _status.Foreground = GetResource<IBrush>("StatusWarningBrush");
            _saveButton.IsEnabled = false;
        }
        finally
        {
            _isApplying = false;
        }
    }

    private void AddGroup(
        string title,
        IReadOnlyList<string> keys,
        IReadOnlyDictionary<string, AvaloniaSettingOption> options,
        ISet<string> added)
    {
        var groupOptions = keys
            .Where(options.ContainsKey)
            .Select(key => options[key])
            .ToArray();
        if (groupOptions.Length == 0)
            return;

        foreach (var option in groupOptions)
            added.Add(option.Key);

        var groupContent = new StackPanel { Spacing = 10 };
        groupContent.Children.Add(new LocalizedTextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        });
        var rows = new StackPanel { Spacing = 0 };
        foreach (var option in groupOptions)
            rows.Children.Add(CreateOptionRow(option));
        groupContent.Children.Add(rows);

        var card = new Border
        {
            Background = GetResource<IBrush>("CardBackgroundBrush"),
            BorderBrush = GetResource<IBrush>("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetCornerRadius("CornerRadiusCard"),
            Padding = new Thickness(16),
            Child = groupContent,
        };
        _groupsPanel.Children.Add(card);
    }

    private Control CreateOptionRow(AvaloniaSettingOption option)
    {
        var title = new LocalizedTextBlock
        {
            Text = option.Title,
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var description = new LocalizedTextBlock
        {
            Text = option.Description,
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        };
        var text = new StackPanel { Spacing = 3, MinWidth = 0 };
        text.Children.Add(title);
        text.Children.Add(description);
        if (!string.IsNullOrWhiteSpace(option.Warning))
        {
            text.Children.Add(new LocalizedTextBlock
            {
                Text = option.Warning,
                Foreground = GetResource<IBrush>("StatusWarningBrush"),
                OverflowMode = LocalizedOverflowMode.Wrap,
                MaxLines = 3,
            });
        }

        var editor = CreateEditor(option);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 };
        grid.Children.Add(text);
        Grid.SetColumn(editor, 1);
        grid.Children.Add(editor);

        var row = new Border
        {
            Padding = new Thickness(0, 10),
            BorderBrush = GetResource<IBrush>("CardBorderBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid,
        };
        AutomationProperties.SetName(row, option.Title);
        return row;
    }

    private Control CreateEditor(AvaloniaSettingOption option) => option.Editor switch
    {
        AvaloniaSettingEditor.Toggle => CreateToggle(option),
        AvaloniaSettingEditor.Selection => CreateSelection(option),
        AvaloniaSettingEditor.MultiSelection => CreateMultiSelection(option),
        AvaloniaSettingEditor.Text => CreateTextBox(option),
        AvaloniaSettingEditor.Action => CreateAction(option),
        _ => new TextBlock { Text = option.Title },
    };

    private CheckBox CreateToggle(AvaloniaSettingOption option)
    {
        var toggle = new CheckBox
        {
            IsChecked = option.BoolValue,
            IsEnabled = option.IsEnabled,
            MinWidth = 48,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        SetAutomation(toggle, option);
        var lastSaved = option.BoolValue;
        toggle.IsCheckedChanged += async (_, _) =>
        {
            if (_isApplying || toggle.IsChecked is not bool value)
                return;

            if (await PersistAsync(
                () => _settingsService.SetToggleAsync("Application", option.Key, value)))
            {
                lastSaved = value;
            }
            else
            {
                _isApplying = true;
                toggle.IsChecked = lastSaved;
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
        SetAutomation(combo, option);
        var lastSaved = option.SelectedValue;
        combo.SelectionChanged += async (_, _) =>
        {
            if (_isApplying || combo.SelectedItem is not string value)
                return;

            if (await PersistAsync(
                () => _settingsService.SetSelectionAsync("Application", option.Key, value)))
            {
                lastSaved = value;
            }
            else
            {
                _isApplying = true;
                combo.SelectedItem = lastSaved;
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
            Width = 180,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        SetAutomation(textBox, option);
        var lastSaved = textBox.Text;
        textBox.LostFocus += async (_, _) =>
        {
            if (_isApplying || string.Equals(textBox.Text, lastSaved, StringComparison.Ordinal))
                return;

            var value = textBox.Text;
            if (await PersistAsync(
                () => _settingsService.SetTextAsync("Application", option.Key, value)))
            {
                lastSaved = value;
            }
            else
            {
                _isApplying = true;
                textBox.Text = lastSaved;
                _isApplying = false;
            }
        };
        return textBox;
    }

    private Control CreateMultiSelection(AvaloniaSettingOption option)
    {
        var selected = new HashSet<string>(option.SelectedValues ?? [], StringComparer.Ordinal);
        var checks = new List<CheckBox>();
        var panel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemWidth = 150,
            MaxWidth = 320,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        foreach (var value in option.Values ?? [])
        {
            var check = new CheckBox
            {
                Content = value,
                IsChecked = selected.Contains(value),
                IsEnabled = option.IsEnabled,
                Width = 150,
                Margin = new Thickness(0, 2, 4, 2),
                HorizontalContentAlignment = HorizontalAlignment.Left,
            };
            AutomationProperties.SetAutomationId(
                check,
                $"AvaloniaOsdSettings_{option.Key}_{checks.Count}");
            AutomationProperties.SetName(check, value);
            ToolTip.SetTip(check, option.Description);
            check.IsCheckedChanged += async (_, _) =>
            {
                if (_isApplying || check.IsChecked is not bool isChecked)
                    return;

                var previous = selected.ToArray();
                if (isChecked)
                    selected.Add(value);
                else
                    selected.Remove(value);

                if (!await PersistAsync(
                    () => _settingsService.SetMultiSelectionAsync(
                        "Application",
                        option.Key,
                        selected.ToArray())))
                {
                    selected.Clear();
                    selected.UnionWith(previous);
                    _isApplying = true;
                    foreach (var sibling in checks)
                        sibling.IsChecked = selected.Contains(sibling.Content?.ToString() ?? string.Empty);
                    _isApplying = false;
                }
            };
            checks.Add(check);
            panel.Children.Add(check);
        }

        AutomationProperties.SetAutomationId(panel, $"AvaloniaOsdSettings_{option.Key}");
        AutomationProperties.SetName(panel, option.Title);
        ToolTip.SetTip(panel, option.Description);
        return panel;
    }

    private Button CreateAction(AvaloniaSettingOption option)
    {
        var button = ActionButton(
            option.ActionText ?? option.Title,
            $"AvaloniaOsdSettings_{option.Key}",
            async () =>
            {
                await PersistAsync(
                    () => _settingsService.InvokeActionAsync("Application", option.Key));
            });
        button.IsEnabled = option.IsEnabled;
        ToolTip.SetTip(button, option.Description);
        return button;
    }

    private async Task<bool> PersistAsync(Func<Task> action)
    {
        if (_isApplying || _isSaving)
            return false;

        try
        {
            await action().ConfigureAwait(true);
            _saveButton.IsEnabled = true;
            _status.Text = Get("Settings_Page_SaveSuccess", "Setting saved.");
            _status.Foreground = GetResource<IBrush>("StatusSuccessBrush");
            return true;
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            _status.Foreground = GetResource<IBrush>("StatusWarningBrush");
            return false;
        }
    }

    private async Task SaveAndCloseAsync()
    {
        if (!_isLoaded || _isSaving)
            return;

        _isSaving = true;
        _saveButton.IsEnabled = false;
        try
        {
            _status.Text = Get("Settings_Page_SaveSuccess", "OSD settings saved.");
            Close(true);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private static Button ActionButton(string text, string automationId, Func<Task> action)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = 100,
            Padding = new Thickness(12, 7),
        };
        AutomationProperties.SetAutomationId(button, automationId);
        ToolTip.SetTip(button, text);
        button.Click += async (_, _) => await action().ConfigureAwait(true);
        return button;
    }

    private static void SetAutomation(Control control, AvaloniaSettingOption option)
    {
        AutomationProperties.SetAutomationId(control, $"AvaloniaOsdSettings_{option.Key}");
        AutomationProperties.SetName(control, option.Title);
        ToolTip.SetTip(control, option.Description);
    }

    private T GetResource<T>(object key) where T : class
    {
        if (this.TryFindResource(key, out var resource) && resource is T typedValue)
            return typedValue;

        if (typeof(T) == typeof(IBrush))
            return (T)(object)new SolidColorBrush(Colors.Transparent);

        throw new InvalidOperationException($"Missing Avalonia resource '{key}'.");
    }

    private CornerRadius GetCornerRadius(object key)
    {
        if (this.TryFindResource(key, out var resource) && resource is CornerRadius radius)
            return radius;

        return new CornerRadius(8);
    }

    private static string Get(string key, string fallback) =>
        AvaloniaLocalization.GetString(key, fallback);
}

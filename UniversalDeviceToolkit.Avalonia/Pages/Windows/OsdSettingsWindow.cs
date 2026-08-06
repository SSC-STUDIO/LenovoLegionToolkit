using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages.Windows;

/// <summary>
/// Focused editor for the on-screen display settings. The Application page
/// keeps the same options for discoverability, while this dialog mirrors the
/// WPF window workflow and commits a coherent batch on Save.
/// </summary>
public sealed class OsdSettingsWindow : Window
{
    private readonly IAvaloniaSettingsService _settingsService;
    private readonly StackPanel _optionsPanel = new() { Spacing = 10 };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button _saveButton;
    private readonly Dictionary<string, Control> _editors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<CheckBox>> _multiEditors = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<AvaloniaSettingOption> _options = [];
    private bool _loaded;
    private bool _saving;

    public OsdSettingsWindow(IAvaloniaSettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        Title = Get("OsdSettings_Window_Title", "On-screen display settings");
        Width = 620;
        MinWidth = 480;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        AutomationProperties.SetAutomationId(this, "AvaloniaOsdSettingsWindow");
        AutomationProperties.SetName(this, Title);

        _saveButton = ActionButton(Get("Save", "Save"), "AvaloniaOsdSettingsSaveButton", SaveAsync);
        _saveButton.IsEnabled = false;
        var cancelButton = ActionButton(Get("Cancel", "Cancel"), "AvaloniaOsdSettingsCancelButton", () =>
        {
            Close(false);
            return Task.CompletedTask;
        });

        var root = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(20),
            FlowDirection = LocalizationCatalog.IsRightToLeft(LocalizationRuntime.CurrentCulture)
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight,
        };
        root.Children.Add(new LocalizedTextBlock
        {
            Text = Title,
            FontSize = 20,
            FontWeight = FontWeight.SemiBold,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        });
        root.Children.Add(new LocalizedTextBlock
        {
            Text = Get("OsdSettings_Window_Description", "Configure the appearance, thresholds, and sensor items shown by the on-screen display."),
            Foreground = Brushes.Gray,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        });
        root.Children.Add(_optionsPanel);
        root.Children.Add(_status);
        root.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { cancelButton, _saveButton },
        });

        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = root,
        };
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_loaded)
            return;

        _loaded = true;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task LoadAsync()
    {
        try
        {
            var page = await _settingsService.GetPageAsync("Application").ConfigureAwait(true);
            _options = page.Options
                .Where(option => option.Key.StartsWith("Osd", StringComparison.OrdinalIgnoreCase))
                .Where(option => option.IsVisible)
                .ToArray();
            _optionsPanel.Children.Clear();
            _editors.Clear();
            _multiEditors.Clear();

            foreach (var option in _options)
            {
                _optionsPanel.Children.Add(CreateOptionCard(option));
                if (option.Key is "OsdItems")
                    _optionsPanel.Children.Add(SectionHeading(Get("OsdSettings_Tabs_SensorItems", "Sensor items")));
            }

            _status.Text = Get("Settings_Page_StatusMessage", "Changes are saved when you select Save.");
            _status.Foreground = Brushes.Gray;
            _saveButton.IsEnabled = _options.Count > 0;
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            _status.Foreground = Brushes.OrangeRed;
            _saveButton.IsEnabled = false;
        }
    }

    private Control CreateOptionCard(AvaloniaSettingOption option)
    {
        var title = new LocalizedTextBlock
        {
            Text = option.Title,
            FontWeight = FontWeight.Medium,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var description = new LocalizedTextBlock
        {
            Text = option.Description,
            Foreground = Brushes.Gray,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        };
        var copy = new StackPanel { Spacing = 3, MinWidth = 0 };
        copy.Children.Add(title);
        copy.Children.Add(description);

        Control editor = option.Editor switch
        {
            AvaloniaSettingEditor.Toggle => CreateToggle(option),
            AvaloniaSettingEditor.Selection => CreateSelection(option),
            AvaloniaSettingEditor.MultiSelection => CreateMultiSelection(option),
            AvaloniaSettingEditor.Text => CreateText(option),
            AvaloniaSettingEditor.Action => CreateAction(option),
            _ => new TextBlock { Text = option.Title },
        };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 };
        grid.Children.Add(copy);
        Grid.SetColumn(editor, 1);
        grid.Children.Add(editor);
        return new Border
        {
            Background = GetBrush("CardBackgroundBrush"),
            BorderBrush = GetBrush("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetResource("CornerRadiusCard", new CornerRadius(8)),
            Padding = new Thickness(14),
            Child = grid,
        };
    }

    private CheckBox CreateToggle(AvaloniaSettingOption option)
    {
        var editor = new CheckBox { IsChecked = option.BoolValue, IsEnabled = option.IsEnabled, MinWidth = 48 };
        RegisterEditor(option, editor);
        return editor;
    }

    private ComboBox CreateSelection(AvaloniaSettingOption option)
    {
        var editor = new ComboBox
        {
            ItemsSource = option.Values ?? [],
            SelectedItem = option.SelectedValue,
            IsEnabled = option.IsEnabled,
            MinWidth = 190,
        };
        RegisterEditor(option, editor);
        return editor;
    }

    private TextBox CreateText(AvaloniaSettingOption option)
    {
        var editor = new TextBox
        {
            Text = option.TextValue ?? string.Empty,
            IsEnabled = option.IsEnabled,
            MinWidth = 190,
        };
        RegisterEditor(option, editor);
        return editor;
    }

    private StackPanel CreateMultiSelection(AvaloniaSettingOption option)
    {
        var selected = (option.SelectedValues ?? []).ToHashSet(StringComparer.Ordinal);
        var checks = new List<CheckBox>();
        foreach (var value in option.Values ?? [])
        {
            var check = new CheckBox
            {
                Content = value,
                IsChecked = selected.Contains(value),
                IsEnabled = option.IsEnabled,
                HorizontalContentAlignment = HorizontalAlignment.Left,
            };
            checks.Add(check);
        }

        var panel = new StackPanel { Spacing = 4, MinWidth = 220, MaxWidth = 360 };
        foreach (var check in checks)
            panel.Children.Add(check);
        _multiEditors[option.Key] = checks;
        AutomationProperties.SetAutomationId(panel, $"AvaloniaOsdSettings_{option.Key}");
        return panel;
    }

    private Button CreateAction(AvaloniaSettingOption option)
    {
        var button = ActionButton(option.ActionText ?? option.Title, $"AvaloniaOsdSettings_{option.Key}", async () =>
        {
            await _settingsService.InvokeActionAsync("Application", option.Key).ConfigureAwait(true);
        });
        button.IsEnabled = option.IsEnabled;
        return button;
    }

    private async Task SaveAsync()
    {
        if (!_loaded || _saving)
            return;

        _saving = true;
        _saveButton.IsEnabled = false;
        try
        {
            foreach (var option in _options)
            {
                if (option.Editor == AvaloniaSettingEditor.Toggle
                    && _editors[option.Key] is CheckBox toggle
                    && toggle.IsChecked is bool boolValue)
                {
                    await _settingsService.SetToggleAsync("Application", option.Key, boolValue).ConfigureAwait(true);
                }
                else if (option.Editor == AvaloniaSettingEditor.Selection
                         && _editors[option.Key] is ComboBox combo
                         && combo.SelectedItem is string selected)
                {
                    await _settingsService.SetSelectionAsync("Application", option.Key, selected).ConfigureAwait(true);
                }
                else if (option.Editor == AvaloniaSettingEditor.Text
                         && _editors[option.Key] is TextBox text)
                {
                    await _settingsService.SetTextAsync("Application", option.Key, text.Text).ConfigureAwait(true);
                }
                else if (option.Editor == AvaloniaSettingEditor.MultiSelection
                         && _multiEditors.TryGetValue(option.Key, out var checks))
                {
                    var values = checks
                        .Where(check => check.IsChecked == true)
                        .Select(check => check.Content?.ToString() ?? string.Empty)
                        .Where(value => value.Length > 0)
                        .ToArray();
                    await _settingsService.SetMultiSelectionAsync("Application", option.Key, values).ConfigureAwait(true);
                }
            }

            _status.Text = Get("Settings_Page_SaveSuccess", "On-screen display settings saved.");
            _status.Foreground = Brushes.SeaGreen;
            Close(true);
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            _status.Foreground = Brushes.OrangeRed;
            _saveButton.IsEnabled = true;
        }
        finally
        {
            _saving = false;
        }
    }

    private void RegisterEditor(AvaloniaSettingOption option, Control editor)
    {
        _editors[option.Key] = editor;
        AutomationProperties.SetAutomationId(editor, $"AvaloniaOsdSettings_{option.Key}");
        AutomationProperties.SetName(editor, option.Title);
        ToolTip.SetTip(editor, option.Description);
    }

    private Button ActionButton(string text, string automationId, Func<Task> action)
    {
        var button = new Button { Content = text, MinWidth = 100, Padding = new Thickness(12, 7) };
        AutomationProperties.SetAutomationId(button, automationId);
        ToolTip.SetTip(button, text);
        button.Click += async (_, _) => await action().ConfigureAwait(true);
        return button;
    }

    private static TextBlock SectionHeading(string text) => new()
    {
        Text = text,
        FontSize = 16,
        FontWeight = FontWeight.SemiBold,
        Margin = new Thickness(0, 8, 0, 0),
    };

    private IBrush GetBrush(string key) =>
        this.TryFindResource(key, out var value) && value is IBrush brush
            ? brush
            : Brushes.Transparent;

    private T GetResource<T>(string key, T fallback) =>
        this.TryFindResource(key, out var value) && value is T resource ? resource : fallback;

    private static string Get(string key, string fallback) => AvaloniaLocalization.GetString(key, fallback);
}

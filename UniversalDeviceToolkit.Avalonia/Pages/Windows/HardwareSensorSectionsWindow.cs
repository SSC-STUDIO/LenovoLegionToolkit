using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages.Windows;

/// <summary>
/// Edits the dashboard sensor section visibility and order in one focused
/// dialog. The dialog uses the same Application-page setting contract as the
/// inline editors, so both surfaces remain in sync with the persisted store.
/// </summary>
public sealed class HardwareSensorSectionsWindow : Window
{
    private static readonly string[] DefaultSections = ["CPU", "Battery", "GPU"];

    private readonly IAvaloniaSettingsService _settingsService;
    private readonly StackPanel _visibleSectionsPanel = new() { Spacing = 6 };
    private readonly StackPanel _orderPanel = new() { Spacing = 6 };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button _saveButton;
    private readonly Dictionary<string, CheckBox> _sectionChecks = new(StringComparer.OrdinalIgnoreCase);
    private List<string> _order = [];
    private bool _isLoaded;
    private bool _isSaving;

    public HardwareSensorSectionsWindow(IAvaloniaSettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        Title = Get("SensorSections_Window_Title", "Hardware sensor sections");
        Width = 520;
        MinWidth = 420;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        AutomationProperties.SetAutomationId(this, "AvaloniaHardwareSensorSectionsWindow");
        AutomationProperties.SetName(this, Title);
        _status.Foreground = new SolidColorBrush(Colors.Gray);
        AutomationProperties.SetAutomationId(_status, "AvaloniaHardwareSensorSectionsStatusText");

        _saveButton = ActionButton(
            Get("Save", "Save"),
            "AvaloniaHardwareSensorSectionsSaveButton",
            SaveAsync);
        _saveButton.IsEnabled = false;
        var cancelButton = ActionButton(
            Get("Cancel", "Cancel"),
            "AvaloniaHardwareSensorSectionsCancelButton",
            () =>
            {
                Close(false);
                return Task.CompletedTask;
            });

        var content = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(20),
            FlowDirection = LocalizationCatalog.IsRightToLeft(LocalizationRuntime.CurrentCulture)
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight,
        };
        content.Children.Add(Heading(
            Get("SensorSections_Visible_Title", "Visible sensor sections")));
        content.Children.Add(Body(
            Get("SensorSections_Visible_Description", "Choose which CPU, battery, and GPU sections appear on the dashboard.")));
        content.Children.Add(Card(_visibleSectionsPanel));
        content.Children.Add(Heading(
            Get("SensorSections_Order_Title", "Sensor section order")));
        content.Children.Add(Body(
            Get("SensorSections_Order_Description", "Move sections up or down to choose their dashboard order.")));
        content.Children.Add(Card(_orderPanel));
        content.Children.Add(_status);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { cancelButton, _saveButton },
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
            var page = await _settingsService.GetPageAsync("Application").ConfigureAwait(true);
            var visibleOption = page.Options.FirstOrDefault(option => option.Key == "HardwareSectionsVisible");
            var orderOption = page.Options.FirstOrDefault(option => option.Key == "HardwareSectionsOrder");
            var sections = (visibleOption?.Values ?? DefaultSections)
                .Where(section => !string.IsNullOrWhiteSpace(section))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (sections.Length == 0)
                sections = [.. DefaultSections];

            var selected = (visibleOption?.SelectedValues ?? [])
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (selected.Count == 0)
                selected.UnionWith(sections);

            _order = ParseOrder(orderOption?.SelectedValue, orderOption?.Values, sections);
            BuildVisibleSections(sections, selected);
            BuildOrderList();
            _status.Text = Get("Settings_Page_StatusMessage", "Changes are saved when you select Save.");
            _status.Foreground = new SolidColorBrush(Colors.Gray);
            UpdateSaveState();
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            _status.Foreground = new SolidColorBrush(Colors.OrangeRed);
            _saveButton.IsEnabled = false;
        }
    }

    private void BuildVisibleSections(IEnumerable<string> sections, ISet<string> selected)
    {
        _visibleSectionsPanel.Children.Clear();
        _sectionChecks.Clear();
        foreach (var section in sections)
        {
            var check = new CheckBox
            {
                Content = section,
                IsChecked = selected.Contains(section),
                HorizontalContentAlignment = HorizontalAlignment.Left,
            };
            AutomationProperties.SetAutomationId(check, $"AvaloniaHardwareSensorSection_{section}");
            AutomationProperties.SetName(check, section);
            check.IsCheckedChanged += (_, _) => UpdateSaveState();
            _sectionChecks[section] = check;
            _visibleSectionsPanel.Children.Add(check);
        }
    }

    private void BuildOrderList()
    {
        _orderPanel.Children.Clear();
        for (var index = 0; index < _order.Count; index++)
        {
            var section = _order[index];
            var up = new Button
            {
                Content = Get("MoveUp", "Up"),
                IsEnabled = index > 0,
                Padding = new Thickness(10, 5),
            };
            var down = new Button
            {
                Content = Get("MoveDown", "Down"),
                IsEnabled = index < _order.Count - 1,
                Padding = new Thickness(10, 5),
            };
            AutomationProperties.SetAutomationId(up, $"AvaloniaHardwareSensorSectionMoveUp_{section}");
            AutomationProperties.SetAutomationId(down, $"AvaloniaHardwareSensorSectionMoveDown_{section}");
            ToolTip.SetTip(up, Get("MoveUp", "Move up"));
            ToolTip.SetTip(down, Get("MoveDown", "Move down"));
            var capturedIndex = index;
            up.Click += (_, _) => Move(capturedIndex, -1);
            down.Click += (_, _) => Move(capturedIndex, 1);

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
                ColumnSpacing = 8,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var label = new TextBlock
            {
                Text = section,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };
            Grid.SetColumn(up, 1);
            Grid.SetColumn(down, 2);
            row.Children.Add(label);
            row.Children.Add(up);
            row.Children.Add(down);
            _orderPanel.Children.Add(row);
        }
    }

    private void Move(int index, int delta)
    {
        var target = index + delta;
        if (index < 0 || target < 0 || target >= _order.Count)
            return;

        (_order[index], _order[target]) = (_order[target], _order[index]);
        BuildOrderList();
    }

    private async Task SaveAsync()
    {
        if (!_isLoaded || _isSaving)
            return;

        var visible = _sectionChecks
            .Where(pair => pair.Value.IsChecked == true)
            .Select(pair => pair.Key)
            .ToArray();
        if (visible.Length == 0)
        {
            _status.Text = Get("SensorSections_Visible_Required", "Select at least one sensor section.");
            _status.Foreground = new SolidColorBrush(Colors.OrangeRed);
            return;
        }

        _isSaving = true;
        _saveButton.IsEnabled = false;
        try
        {
            await _settingsService.SetMultiSelectionAsync(
                "Application",
                "HardwareSectionsVisible",
                visible).ConfigureAwait(true);
            await _settingsService.SetSelectionAsync(
                "Application",
                "HardwareSectionsOrder",
                string.Join(", ", _order)).ConfigureAwait(true);
            _status.Text = Get("Settings_Page_SaveSuccess", "Sensor sections saved.");
            _status.Foreground = new SolidColorBrush(Colors.SeaGreen);
            Close(true);
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            _status.Foreground = new SolidColorBrush(Colors.OrangeRed);
            _saveButton.IsEnabled = true;
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void UpdateSaveState()
    {
        _saveButton.IsEnabled = _isLoaded
            && !_isSaving
            && _sectionChecks.Values.Any(check => check.IsChecked == true);
    }

    private static List<string> ParseOrder(
        string? selectedValue,
        IReadOnlyList<string>? allowedValues,
        IReadOnlyList<string> sections)
    {
        var selected = selectedValue?
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(value => sections.Contains(value, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
        if (selected.Count != sections.Count)
            selected = [];

        if (selected.Count == 0 && allowedValues is not null)
        {
            var fallback = allowedValues.FirstOrDefault(value =>
                value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Length == sections.Count);
            if (fallback is not null)
                selected = fallback.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        return selected.Count == sections.Count ? selected : [.. sections];
    }

    private static Button ActionButton(string text, string automationId, Func<Task> action)
    {
        var button = new Button { Content = text, MinWidth = 100, Padding = new Thickness(12, 7) };
        AutomationProperties.SetAutomationId(button, automationId);
        ToolTip.SetTip(button, text);
        button.Click += async (_, _) => await action().ConfigureAwait(true);
        return button;
    }

    private static Border Card(Control child) => new()
    {
        Padding = new Thickness(12),
        BorderThickness = new Thickness(1),
        BorderBrush = Brushes.Gray,
        CornerRadius = new CornerRadius(8),
        Child = child,
    };

    private static TextBlock Heading(string text) => new()
    {
        Text = text,
        FontSize = 16,
        FontWeight = FontWeight.SemiBold,
        TextWrapping = TextWrapping.Wrap,
    };

    private static TextBlock Body(string text) => new()
    {
        Text = text,
        Foreground = Brushes.Gray,
        TextWrapping = TextWrapping.Wrap,
    };

    private static string Get(string key, string fallback) =>
        AvaloniaLocalization.GetString(key, fallback);
}

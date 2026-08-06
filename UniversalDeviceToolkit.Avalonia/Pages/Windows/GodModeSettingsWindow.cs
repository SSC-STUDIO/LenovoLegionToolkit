using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages.Windows;

/// <summary>
/// Avalonia editor for the host-neutral GodMode projection. Hardware-specific
/// validation remains in the shared GodMode controller; this window only edits
/// values reported by that controller and never performs an unsafe operation by
/// itself.
/// </summary>
public sealed class GodModeSettingsWindow : Window
{
    private readonly IPlatformServices _platformServices;
    private readonly ComboBox _presetComboBox;
    private readonly TextBox _presetName;
    private readonly StackPanel _valuesPanel;
    private readonly StackPanel _fanPanel;
    private readonly StackPanel _advancedPanel;
    private readonly TextBlock _status;
    private readonly Button _saveButton;
    private readonly Button _saveCloseButton;
    private readonly Dictionary<string, NumericUpDown> _valueEditors = new(StringComparer.Ordinal);
    private readonly List<NumericUpDown> _fanEditors = [];
    private NumericUpDown? _minOffsetEditor;
    private NumericUpDown? _maxOffsetEditor;
    private ToggleSwitch? _fanFullSpeedEditor;
    private GodModeSettingsState? _state;
    private GodModePresetState? _activePreset;
    private bool _isLoaded;
    private bool _isRefreshing;
    private bool _isSaving;

    public GodModeSettingsWindow(IPlatformServices platformServices)
    {
        _platformServices = platformServices ?? throw new ArgumentNullException(nameof(platformServices));
        Title = Get("GodModeSettingsWindow_Title", "GodMode settings");
        Width = 920;
        Height = 720;
        MinWidth = 820;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        AutomationProperties.SetAutomationId(this, "AvaloniaGodModeSettingsWindow");
        AutomationProperties.SetName(this, Title);

        _presetComboBox = new ComboBox { MinWidth = 260, HorizontalAlignment = HorizontalAlignment.Stretch };
        _presetComboBox.ItemTemplate = new FuncDataTemplate<GodModePresetState>(
            (preset, _) => new TextBlock { Text = preset?.Name ?? string.Empty });
        _presetComboBox.SelectionChanged += PresetComboBox_SelectionChanged;
        AutomationProperties.SetAutomationId(_presetComboBox, "GodModePresetComboBox");
        AutomationProperties.SetName(_presetComboBox, Get("GodModeSettingsWindow_ActivePreset_Title", "Active preset"));

        _presetName = new TextBox { MinWidth = 180, Watermark = Get("GodModeSettingsWindow_EditPreset_Message", "Preset name") };
        AutomationProperties.SetAutomationId(_presetName, "GodModePresetNameTextBox");

        _valuesPanel = new StackPanel { Spacing = 8 };
        _fanPanel = new StackPanel { Spacing = 8 };
        _advancedPanel = new StackPanel { Spacing = 8 };
        _status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetAutomationId(_status, "AvaloniaGodModeSettingsStatusText");

        _saveButton = ActionButton(Get("Save", "Save"), "GodModeSaveButton", () => SaveAsync(close: false));
        _saveCloseButton = ActionButton(Get("GodModeSettingsWindow_SaveAndClose", "Save and close"), "GodModeSaveAndCloseButton", () => SaveAsync(close: true));
        _saveButton.IsEnabled = false;
        _saveCloseButton.IsEnabled = false;

        var root = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(24),
            FlowDirection = LocalizationCatalog.IsRightToLeft(LocalizationRuntime.CurrentCulture)
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight,
        };
        root.Children.Add(new LocalizedTextBlock
        {
            Text = Title,
            FontSize = 22,
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        });
        root.Children.Add(CreateWarning("GodModeSettingsWindow_VantageWarning_Title", "Close Lenovo Vantage before applying GodMode settings."));
        root.Children.Add(CreateWarning("GodModeSettingsWindow_LegionZoneWarning_Title", "Close Legion Zone before applying GodMode settings."));
        root.Children.Add(CreateSection(
            Get("GodModeSettingsWindow_ActivePreset_Title", "Active preset"),
            CreatePresetRow()));
        root.Children.Add(_valuesPanel);
        root.Children.Add(_fanPanel);
        root.Children.Add(_advancedPanel);
        root.Children.Add(_status);

        var closeButton = ActionButton(Get("Cancel", "Cancel"), "GodModeCancelButton", () =>
        {
            Close(false);
            return Task.CompletedTask;
        });
        root.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { closeButton, _saveButton, _saveCloseButton },
        });

        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = root,
        };
        Loaded += OnLoaded;
    }

    private Control CreatePresetRow()
    {
        var renameButton = ActionButton(Get("Edit", "Rename"), "GodModeRenamePresetButton", RenamePresetAsync);
        var addButton = ActionButton(Get("Add", "Add"), "GodModeAddPresetButton", AddPresetAsync);
        var deleteButton = ActionButton(Get("Delete", "Delete"), "GodModeDeletePresetButton", DeletePresetAsync);
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto,Auto"), ColumnSpacing = 8 };
        row.Children.Add(_presetComboBox);
        Grid.SetColumn(_presetName, 1);
        row.Children.Add(_presetName);
        Grid.SetColumn(renameButton, 2);
        row.Children.Add(renameButton);
        Grid.SetColumn(addButton, 3);
        row.Children.Add(addButton);
        Grid.SetColumn(deleteButton, 4);
        row.Children.Add(deleteButton);
        return row;
    }

    private Control CreateWarning(string key, string fallback)
    {
        var text = new LocalizedTextBlock
        {
            Text = Get(key, fallback),
            Foreground = GetResource<IBrush>("StatusCriticalBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        };
        var border = new Border
        {
            Background = GetResource<IBrush>("StatusCriticalBackgroundBrush"),
            BorderBrush = GetResource<IBrush>("StatusCriticalBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetCornerRadius("CornerRadiusCard"),
            Padding = new Thickness(12, 8),
            Child = text,
            IsVisible = false,
        };
        border.Classes.Add(key.Contains("Vantage", StringComparison.Ordinal) ? "GodModeVantageWarning" : "GodModeLegionZoneWarning");
        return border;
    }

    private Control CreateSection(string title, Control content)
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new LocalizedTextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        });
        panel.Children.Add(content);
        return panel;
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
            _isRefreshing = true;
            var state = await _platformServices.GetGodModeSettingsAsync().ConfigureAwait(true);
            _state = state;
            _presetComboBox.ItemsSource = state.Presets;
            var selectedPreset = state.Presets.FirstOrDefault(
                preset => preset.Id == state.ActivePresetId)
                ?? state.Presets.FirstOrDefault();
            _presetComboBox.SelectedItem = selectedPreset;
            RenderPreset(selectedPreset);
            UpdateWarnings(state);
            _status.Text = state.ErrorMessage
                ?? Get("GodModeSettingsWindow_Status", "Adjust the active preset and select Save to apply it.");
            _status.Foreground = state.IsAvailable
                ? GetResource<IBrush>("TextFillColorSecondaryBrush")
                : GetResource<IBrush>("StatusCriticalBrush");
            _saveButton.IsEnabled = state.IsAvailable;
            _saveCloseButton.IsEnabled = state.IsAvailable;
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            _status.Foreground = GetResource<IBrush>("StatusCriticalBrush");
            _saveButton.IsEnabled = false;
            _saveCloseButton.IsEnabled = false;
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void UpdateWarnings(GodModeSettingsState state)
    {
        // Warning controls are discovered through their class marker so the
        // page stays compact when the host reports no external conflict.
        if (Content is not ScrollViewer scrollViewer
            || scrollViewer.Content is not Panel root)
            return;

        foreach (var child in root.Children.OfType<Control>())
        {
            if (child.Classes.Contains("GodModeVantageWarning"))
                child.IsVisible = state.NeedsVantageDisabled;
            if (child.Classes.Contains("GodModeLegionZoneWarning"))
                child.IsVisible = state.NeedsLegionZoneDisabled;
        }
    }

    private void RenderPreset(GodModePresetState? preset)
    {
        _activePreset = preset;
        _valueEditors.Clear();
        _fanEditors.Clear();
        _minOffsetEditor = null;
        _maxOffsetEditor = null;
        _fanFullSpeedEditor = null;
        _valuesPanel.Children.Clear();
        _fanPanel.Children.Clear();
        _advancedPanel.Children.Clear();
        if (preset is null)
            return;

        _presetName.Text = preset.Name;

        var cpuValues = preset.Values.Where(value => value.Key.StartsWith("CPU", StringComparison.Ordinal)
            || value.Key.Equals("APUsPPTPowerLimit", StringComparison.Ordinal)).ToArray();
        var gpuValues = preset.Values.Except(cpuValues).ToArray();
        if (cpuValues.Length > 0)
            AddValueGroup(_valuesPanel, Get("GodModeSettingsWindow_CPU_Title", "CPU"), cpuValues);
        if (gpuValues.Length > 0)
            AddValueGroup(_valuesPanel, Get("GodModeSettingsWindow_GPU_Title", "GPU"), gpuValues);

        if (preset.FanFullSpeed.HasValue || preset.FanCurveValues is not null)
        {
            _fanPanel.Children.Add(CreateHeading(Get("GodModeSettingsWindow_Fans_Title", "Fans")));
            if (preset.FanCurveValues is { Count: 10 } fanCurve)
            {
                _fanPanel.Children.Add(new LocalizedTextBlock
                {
                    Text = Get("GodModeSettingsWindow_Fans_Curve_Message", "Set the fan curve values for each point."),
                    Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
                    OverflowMode = LocalizedOverflowMode.Wrap,
                    MaxLines = 3,
                });
                var curveGrid = new WrapPanel { Orientation = Orientation.Horizontal };
                for (var index = 0; index < fanCurve.Count; index++)
                {
                    var editor = new NumericUpDown
                    {
                        Minimum = 0,
                        Maximum = ushort.MaxValue,
                        Increment = 1,
                        Value = fanCurve[index],
                        FormatString = "0",
                        Width = 120,
                        Margin = new Thickness(4),
                    };
                    AutomationProperties.SetAutomationId(editor, $"GodModeFanCurvePoint{index + 1}");
                    _fanEditors.Add(editor);
                    curveGrid.Children.Add(new StackPanel
                    {
                        Spacing = 2,
                        Children =
                        {
                            new TextBlock { Text = $"{index + 1}" },
                            editor,
                        },
                    });
                }
                _fanPanel.Children.Add(curveGrid);
            }

            if (preset.FanFullSpeed.HasValue)
            {
                _fanFullSpeedEditor = new ToggleSwitch
                {
                    IsChecked = preset.FanFullSpeed,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Content = Get("GodModeSettingsWindow_Fans_Max_Title", "Full-speed fan"),
                };
                AutomationProperties.SetAutomationId(_fanFullSpeedEditor, "GodModeFanFullSpeedToggle");
                _fanPanel.Children.Add(_fanFullSpeedEditor);
            }
        }

        if (preset.MinValueOffset.HasValue || preset.MaxValueOffset.HasValue)
        {
            _advancedPanel.Children.Add(CreateHeading(Get("GodModeSettingsWindow_Advanced_Title", "Advanced")));
            if (preset.MaxValueOffset.HasValue)
                _maxOffsetEditor = AddOffsetEditor(
                    _advancedPanel,
                    "GodModeSettingsWindow_Advanced_MaxOffset_Title",
                    "Maximum offset",
                    preset.MaxValueOffset.Value,
                    0,
                    100,
                    "GodModeMaxValueOffset");
            if (preset.MinValueOffset.HasValue)
                _minOffsetEditor = AddOffsetEditor(
                    _advancedPanel,
                    "GodModeSettingsWindow_Advanced_MinOffset_Title",
                    "Minimum offset",
                    preset.MinValueOffset.Value,
                    -100,
                    0,
                    "GodModeMinValueOffset");
        }
    }

    private void AddValueGroup(
        Panel parent,
        string title,
        IReadOnlyList<GodModeValueState> values)
    {
        parent.Children.Add(CreateHeading(title));
        foreach (var value in values)
        {
            var editor = new NumericUpDown
            {
                Minimum = value.Minimum,
                Maximum = value.Maximum,
                Increment = value.Step,
                Value = value.Value,
                FormatString = "0",
                Width = 120,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            AutomationProperties.SetAutomationId(editor, $"GodMode{value.Key}");
            AutomationProperties.SetName(editor, value.Title);
            _valueEditors[value.Key] = editor;

            var description = string.IsNullOrWhiteSpace(value.Description)
                ? value.Title
                : $"{value.Description} ({value.Unit})";
            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                ColumnSpacing = 12,
                Margin = new Thickness(0, 0, 0, 4),
            };
            row.Children.Add(new StackPanel
            {
                Spacing = 2,
                MinWidth = 0,
                Children =
                {
                    new LocalizedTextBlock
                    {
                        Text = value.Title,
                        OverflowMode = LocalizedOverflowMode.Wrap,
                        MaxLines = 2,
                        FontWeight = FontWeight.Medium,
                    },
                    new LocalizedTextBlock
                    {
                        Text = description,
                        Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
                        OverflowMode = LocalizedOverflowMode.Wrap,
                        MaxLines = 3,
                    },
                },
            });
            Grid.SetColumn(editor, 1);
            row.Children.Add(editor);
            parent.Children.Add(new Border
            {
                Background = GetResource<IBrush>("CardBackgroundBrush"),
                BorderBrush = GetResource<IBrush>("CardBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = GetCornerRadius("CornerRadiusCard"),
                Padding = new Thickness(12, 8),
                Child = row,
            });
        }
    }

    private NumericUpDown AddOffsetEditor(
        Panel parent,
        string key,
        string fallback,
        int value,
        int minimum,
        int maximum,
        string automationId)
    {
        var editor = new NumericUpDown
        {
            Minimum = minimum,
            Maximum = maximum,
            Increment = 1,
            Value = value,
            FormatString = "0",
            Width = 120,
        };
        AutomationProperties.SetAutomationId(editor, automationId);
        AutomationProperties.SetName(editor, Get(key, fallback));
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        row.Children.Add(new LocalizedTextBlock
        {
            Text = Get(key, fallback),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        });
        Grid.SetColumn(editor, 1);
        row.Children.Add(editor);
        parent.Children.Add(row);
        return editor;
    }

    private async void PresetComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing || _state is null || _presetComboBox.SelectedItem is not GodModePresetState selected)
            return;

        if (_activePreset?.Id == selected.Id)
            return;

        _isRefreshing = true;
        try
        {
            if (await _platformServices.SetGodModePresetAsync(selected.Id).ConfigureAwait(true))
                await LoadAsync().ConfigureAwait(true);
            else
                SetError(Get("GodModeSettingsWindow_PresetFailed", "The selected preset could not be applied."));
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private async Task RenamePresetAsync()
    {
        if (_activePreset is null || string.IsNullOrWhiteSpace(_presetName.Text))
            return;

        if (await _platformServices.RenameGodModePresetAsync(
                _activePreset.Id,
                _presetName.Text.Trim()).ConfigureAwait(true))
        {
            await LoadAsync().ConfigureAwait(true);
            SetSuccess(Get("GodModeSettingsWindow_PresetRenamed", "Preset renamed."));
        }
        else
        {
            SetError(Get("GodModeSettingsWindow_PresetFailed", "The selected preset could not be updated."));
        }
    }

    private async Task AddPresetAsync()
    {
        if (_state?.IsAvailable != true)
            return;

        var requestedName = _presetName.Text;
        await SaveAsync(close: false).ConfigureAwait(true);
        var name = string.IsNullOrWhiteSpace(requestedName)
            ? Get("GodModeSettingsWindow_DefaultPresetName", "Custom mode")
            : requestedName.Trim();
        if (await _platformServices.AddGodModePresetAsync(name).ConfigureAwait(true))
        {
            await LoadAsync().ConfigureAwait(true);
            SetSuccess(Get("GodModeSettingsWindow_PresetAdded", "Preset added."));
        }
        else
        {
            SetError(Get("GodModeSettingsWindow_PresetFailed", "The preset could not be added."));
        }
    }

    private async Task DeletePresetAsync()
    {
        if (_activePreset is null || _state?.Presets.Count <= 1)
            return;

        if (await _platformServices.DeleteGodModePresetAsync(_activePreset.Id).ConfigureAwait(true))
        {
            await LoadAsync().ConfigureAwait(true);
            SetSuccess(Get("GodModeSettingsWindow_PresetDeleted", "Preset deleted."));
        }
        else
        {
            SetError(Get("GodModeSettingsWindow_PresetFailed", "The preset could not be deleted."));
        }
    }

    private async Task SaveAsync(bool close)
    {
        if (_isSaving || _state is null || _activePreset is null || !_state.IsAvailable)
            return;

        _isSaving = true;
        _saveButton.IsEnabled = false;
        _saveCloseButton.IsEnabled = false;
        try
        {
            var values = _valueEditors.ToDictionary(
                pair => pair.Key,
                pair => ToInt(pair.Value.Value));
            var fanCurve = _fanEditors.Count == 10
                ? _fanEditors.Select(editor => (ushort)Math.Clamp(ToInt(editor.Value), 0, ushort.MaxValue)).ToArray()
                : null;
            var update = new GodModeSettingsUpdate(
                _activePreset.Id,
                values,
                _fanFullSpeedEditor?.IsChecked,
                _minOffsetEditor is null ? null : ToInt(_minOffsetEditor.Value),
                _maxOffsetEditor is null ? null : ToInt(_maxOffsetEditor.Value),
                fanCurve);
            if (await _platformServices.SaveGodModeSettingsAsync(update).ConfigureAwait(true))
            {
                SetSuccess(Get("GodModeSettingsWindow_ApplySuccess_Message", "Custom mode settings applied successfully."));
                await LoadAsync().ConfigureAwait(true);
                if (close)
                    Close(true);
            }
            else
            {
                SetError(Get("GodModeSettingsWindow_ApplyFailed", "GodMode settings could not be applied."));
            }
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            _isSaving = false;
            _saveButton.IsEnabled = _state?.IsAvailable == true;
            _saveCloseButton.IsEnabled = _state?.IsAvailable == true;
        }
    }

    private void SetSuccess(string message)
    {
        _status.Text = message;
        _status.Foreground = GetResource<IBrush>("StatusSuccessBrush");
    }

    private void SetError(string message)
    {
        _status.Text = message;
        _status.Foreground = GetResource<IBrush>("StatusCriticalBrush");
    }

    private static int ToInt(decimal? value) => value.HasValue ? (int)Math.Round(value.Value) : 0;

    private LocalizedTextBlock CreateHeading(string title) => new()
    {
        Text = title,
        FontSize = 16,
        FontWeight = FontWeight.Medium,
        Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
        OverflowMode = LocalizedOverflowMode.Wrap,
        MaxLines = 2,
        Margin = new Thickness(0, 8, 0, 2),
    };

    private static Button ActionButton(string text, string automationId, Func<Task> action)
    {
        var button = new Button { Content = text, MinWidth = 96 };
        AutomationProperties.SetAutomationId(button, automationId);
        AutomationProperties.SetName(button, text);
        button.Click += async (_, _) => await action().ConfigureAwait(true);
        return button;
    }

    private static string Get(string key, string fallback) =>
        AvaloniaLocalization.GetString(key, fallback);

    private static T GetResource<T>(string key)
        where T : class =>
        Application.Current?.TryGetResource(key, out var value) == true && value is T resource
            ? resource
            : (T)(object)new SolidColorBrush(Colors.Gray);

    private static CornerRadius GetCornerRadius(string key) =>
        Application.Current?.TryGetResource(key, out var value) == true && value is CornerRadius radius
            ? radius
            : new CornerRadius(8);
}

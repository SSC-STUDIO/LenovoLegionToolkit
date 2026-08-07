using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

/// <summary>
/// Avalonia macro workspace backed by the shared MacroController. Recording and
/// playback continue to use the existing feature-action bridge; sequence options
/// are persisted immediately to the same macro store used by WPF.
/// </summary>
public sealed class MacroPage : UserControl
{
    private readonly IPlatformServices _platformServices;
    private readonly StackPanel _slotsPanel = new() { Spacing = 8 };
    private readonly CheckBox _enabledToggle = new() { MinWidth = 48 };
    private readonly Button _stopButton = new() { MinWidth = 96 };
    private readonly LocalizedTextBlock _statusBlock = new();
    private bool _isRefreshing;
    private bool _isPreparingRecording;

    public MacroPage(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
        Content = BuildContent();
        Loaded += OnLoaded;
    }

    private Control BuildContent()
    {
        var title = CreateText(
            Get("MacroPage_Title", "Macro"),
            "FontSizeDisplaySection",
            "TextFillColorPrimaryBrush",
            FontWeight.Medium,
            LocalizedOverflowMode.Wrap,
            2);
        var description = CreateText(
            Get("MacroPage_Description", "Create and manage device macros."),
            "FontSizeBody",
            "TextFillColorSecondaryBrush",
            FontWeight.Normal,
            LocalizedOverflowMode.Wrap,
            3);

        AutomationProperties.SetName(this, title.Text ?? "Macro");
        var header = new StackPanel { Spacing = 4 };
        header.Children.Add(title);
        header.Children.Add(description);

        _enabledToggle.Content = Get("MacroPage_Enable", "Enable macro input");
        _enabledToggle.VerticalAlignment = VerticalAlignment.Center;
        _enabledToggle.IsCheckedChanged += EnabledToggle_IsCheckedChanged;
        AutomationProperties.SetAutomationId(_enabledToggle, "AvaloniaMacroEnabledToggle");
        AutomationProperties.SetName(_enabledToggle, Get("MacroPage_Enable", "Enable macro input"));

        var enabledCopy = new StackPanel { Spacing = 3 };
        enabledCopy.Children.Add(CreateText(
            Get("MacroPage_Enable", "Enable macro input"),
            "FontSizeBody",
            "TextFillColorPrimaryBrush",
            FontWeight.Medium,
            LocalizedOverflowMode.Wrap,
            2));
        enabledCopy.Children.Add(CreateText(
            Get("MacroPage_EnableDescription", "Allow the global macro hook to receive and play keyboard sequences."),
            "FontSizeCaption",
            "TextFillColorSecondaryBrush",
            FontWeight.Normal,
            LocalizedOverflowMode.Wrap,
            3));
        var enabledGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 };
        enabledGrid.Children.Add(enabledCopy);
        Grid.SetColumn(_enabledToggle, 1);
        enabledGrid.Children.Add(_enabledToggle);

        var enabledCard = new Border
        {
            Background = GetBrush("CardBackgroundBrush"),
            BorderBrush = GetBrush("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetResource("CornerRadiusCard", new CornerRadius(8)),
            Padding = new Thickness(16),
            Child = enabledGrid,
        };

        _stopButton.Content = Get("Stop", "Stop");
        _stopButton.IsEnabled = false;
        _stopButton.Click += StopButton_Click;
        AutomationProperties.SetAutomationId(_stopButton, "AvaloniaMacroStopRecordingButton");
        AutomationProperties.SetName(_stopButton, Get("Stop", "Stop"));
        _statusBlock.Foreground = GetBrush("TextFillColorSecondaryBrush");
        _statusBlock.OverflowMode = LocalizedOverflowMode.Wrap;
        _statusBlock.MaxLines = 3;

        var toolbar = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        toolbar.Children.Add(_statusBlock);
        Grid.SetColumn(_stopButton, 1);
        toolbar.Children.Add(_stopButton);

        var content = new StackPanel { Spacing = 16, Margin = new Thickness(32, 24, 32, 32), MaxWidth = 920 };
        content.Children.Add(header);
        content.Children.Add(enabledCard);
        content.Children.Add(toolbar);
        content.Children.Add(_slotsPanel);
        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content,
        };
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            _isRefreshing = true;
            var state = await _platformServices.GetMacroWorkspaceAsync();
            _enabledToggle.IsChecked = state.IsEnabled;
            _stopButton.IsEnabled = state.IsRecording;
            _statusBlock.Text = state.IsRecording
                ? Get("MacroPage_Recording", "Recording a macro. Stop recording to save the active slot.")
                : string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    Get("MacroPage_SlotSummary", "{0} macro slots are available."),
                    state.Slots.Count);

            _slotsPanel.Children.Clear();
            foreach (var slot in state.Slots)
                _slotsPanel.Children.Add(CreateSlotCard(slot, state.IsRecording));
        }
        catch (Exception ex)
        {
            _statusBlock.Text = ex.Message;
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private Control CreateSlotCard(MacroSlotState slot, bool isRecording)
    {
        var digit = slot.Key - 0x60;
        var title = CreateText(
            $"Numpad {digit}",
            "FontSizeBody",
            "TextFillColorPrimaryBrush",
            FontWeight.Medium,
            LocalizedOverflowMode.Wrap,
            2);
        var summary = CreateText(
            slot.EventCount == 0
                ? Get("MacroPage_EmptySlot", "No sequence is stored for this slot.")
                : string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    Get("MacroPage_EventSummary", "{0} recorded event(s)."),
                    slot.EventCount),
            "FontSizeCaption",
            "TextFillColorSecondaryBrush",
            FontWeight.Normal,
            LocalizedOverflowMode.Wrap,
            3);
        var copy = new StackPanel { Spacing = 3, MinWidth = 0 };
        copy.Children.Add(title);
        copy.Children.Add(summary);

        if (slot.Events is { Count: > 0 })
        {
            var eventList = new StackPanel
            {
                Spacing = 2,
                Margin = new Thickness(0, 5, 0, 0),
            };
            foreach (var macroEvent in slot.Events)
            {
                eventList.Children.Add(CreateText(
                    FormatEvent(macroEvent),
                    "FontSizeCaption",
                    "TextFillColorSecondaryBrush",
                    FontWeight.Normal,
                    LocalizedOverflowMode.Ellipsis,
                    1));
            }

            copy.Children.Add(eventList);
        }

        var play = new Button
        {
            Content = Get("Play", "Play"),
            IsEnabled = slot.EventCount > 0 && !isRecording,
            MinWidth = 72,
        };
        var record = new Button
        {
            Content = Get("Record", "Record"),
            IsEnabled = !isRecording,
            MinWidth = 84,
        };
        var clear = new Button
        {
            Content = Get("Clear", "Clear"),
            IsEnabled = slot.EventCount > 0 && !isRecording,
            MinWidth = 72,
        };

        var recordingOptions = new ComboBox
        {
            ItemsSource = new[]
            {
                Get("MacroSequenceControl_Keyboard", "Keyboard"),
                Get("MacroSequenceControl_KeyboardMouse", "Keyboard + mouse"),
                Get("MacroSequenceControl_KeyboardMouseMovement", "Keyboard + mouse movement"),
            },
            SelectedIndex = 0,
            IsEnabled = !isRecording,
            MinWidth = 180,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        ToolTip.SetTip(
            recordingOptions,
            Get("MacroSequenceControl_RecordingOptions", "Choose which input sources recording captures."));
        AutomationProperties.SetAutomationId(recordingOptions, $"AvaloniaMacro_{slot.Key:X}_RecordingOptions");
        AutomationProperties.SetName(
            recordingOptions,
            Get("MacroSequenceControl_RecordingOptions", "Recording options"));

        play.Click += async (_, _) => await RunActionAsync($"macro-key:{slot.Key:X}");
        record.Click += async (_, _) => await StartRecordingAsync(slot.Key, recordingOptions.SelectedIndex);
        clear.Click += async (_, _) => await ClearSequenceAsync(slot.Key);
        AutomationProperties.SetAutomationId(play, $"AvaloniaMacro_{slot.Key:X}_PlayButton");
        AutomationProperties.SetAutomationId(record, $"AvaloniaMacro_{slot.Key:X}_RecordButton");
        AutomationProperties.SetAutomationId(clear, $"AvaloniaMacro_{slot.Key:X}_ClearButton");
        ToolTip.SetTip(play, summary.Text);
        ToolTip.SetTip(record, Get("MacroPage_RecordDescription", "Capture keyboard input into this macro slot."));
        ToolTip.SetTip(clear, Get("MacroPage_ClearDescription", "Remove all recorded events from this macro slot."));

        var repeat = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 10,
            Increment = 1,
            Value = Math.Clamp(slot.RepeatCount, 1, 10),
            MinWidth = 72,
            FormatString = "0x",
            IsEnabled = slot.EventCount > 0 && !isRecording,
        };
        var ignoreDelays = new CheckBox
        {
            Content = Get("MacroPage_IgnoreDelays", "Ignore delays"),
            IsChecked = slot.IgnoreDelays,
            IsEnabled = slot.EventCount > 0 && !isRecording,
        };
        var interrupt = new CheckBox
        {
            Content = Get("MacroPage_InterruptOnOtherKey", "Interrupt on other key"),
            IsChecked = slot.InterruptOnOtherKey,
            IsEnabled = slot.EventCount > 0 && !isRecording,
        };
        repeat.ValueChanged += async (_, _) => await SaveOptionsAsync(slot.Key, repeat, ignoreDelays, interrupt);
        ignoreDelays.IsCheckedChanged += async (_, _) => await SaveOptionsAsync(slot.Key, repeat, ignoreDelays, interrupt);
        interrupt.IsCheckedChanged += async (_, _) => await SaveOptionsAsync(slot.Key, repeat, ignoreDelays, interrupt);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Children.Add(play);
        actions.Children.Add(record);
        actions.Children.Add(clear);
        actions.Children.Add(repeat);

        var options = new StackPanel { Spacing = 4 };
        options.Children.Add(ignoreDelays);
        options.Children.Add(interrupt);

        var right = new StackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        right.Children.Add(recordingOptions);
        right.Children.Add(actions);
        right.Children.Add(options);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 };
        grid.Children.Add(copy);
        Grid.SetColumn(right, 1);
        grid.Children.Add(right);

        var card = new Border
        {
            Background = GetBrush("CardBackgroundBrush"),
            BorderBrush = GetBrush("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetResource("CornerRadiusCard", new CornerRadius(8)),
            Padding = new Thickness(16),
            Child = grid,
        };
        AutomationProperties.SetName(card, title.Text ?? $"Numpad {digit}");
        return card;
    }

    private async void EnabledToggle_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_isRefreshing || _enabledToggle.IsChecked is not bool enabled)
            return;

        if (!await _platformServices.SetMacroEnabledAsync(enabled))
        {
            _isRefreshing = true;
            _enabledToggle.IsChecked = !enabled;
            _isRefreshing = false;
            _statusBlock.Text = Get("MacroPage_EnableError", "Unable to update macro input state.");
        }
    }

    private async void StopButton_Click(object? sender, RoutedEventArgs e) =>
        await RunActionAsync("macro-stop-recording");

    private async Task RunActionAsync(string actionKey)
    {
        if (!await _platformServices.SetFeatureActionAsync("Macro", actionKey, true))
        {
            _statusBlock.Text = Get("MacroPage_ActionError", "The macro action could not be completed.");
            return;
        }

        await RefreshAsync();
    }

    private async Task StartRecordingAsync(ulong key, int selectedMode)
    {
        if (_isPreparingRecording)
            return;

        var mode = selectedMode switch
        {
            1 => MacroRecordingMode.KeyboardMouse,
            2 => MacroRecordingMode.KeyboardMouseMovement,
            _ => MacroRecordingMode.Keyboard,
        };

        _isPreparingRecording = true;
        try
        {
            // The WPF host gives the user three seconds to move away from the record button
            // before mouse-movement capture starts. Preserve that behavior in the native page.
            if (mode == MacroRecordingMode.KeyboardMouseMovement)
            {
                _statusBlock.Text = Get("MacroRecordingWindow_Preparing_Title", "Preparing to record macro input...");
                await Task.Delay(TimeSpan.FromSeconds(3));
            }

            if (!await _platformServices.StartMacroRecordingAsync(key, mode))
            {
                _statusBlock.Text = Get("MacroPage_ActionError", "The macro action could not be completed.");
                return;
            }

            await RefreshAsync();
        }
        finally
        {
            _isPreparingRecording = false;
        }
    }

    private async Task ClearSequenceAsync(ulong key)
    {
        if (_isRefreshing || !await _platformServices.ClearMacroSequenceAsync(key))
        {
            _statusBlock.Text = Get("MacroPage_ClearError", "Unable to clear this macro slot.");
            return;
        }

        await RefreshAsync();
    }

    private static string FormatEvent(MacroEventItem macroEvent)
    {
        var location = macroEvent.X == 0 && macroEvent.Y == 0
            ? string.Empty
            : $" ({macroEvent.X}, {macroEvent.Y})";
        return $"{macroEvent.Source} {macroEvent.Direction} | {macroEvent.Key}{location} | +{macroEvent.Delay.TotalMilliseconds:0} ms";
    }

    private async Task SaveOptionsAsync(
        ulong key,
        NumericUpDown repeat,
        CheckBox ignoreDelays,
        CheckBox interrupt)
    {
        if (_isRefreshing || repeat.Value is not decimal repeatValue
            || ignoreDelays.IsChecked is not bool ignore
            || interrupt.IsChecked is not bool stop)
            return;

        var accepted = await _platformServices.SetMacroSequenceOptionsAsync(
            key,
            (int)Math.Round(repeatValue),
            ignore,
            stop);
        if (!accepted)
            _statusBlock.Text = Get("MacroPage_OptionsError", "Unable to save macro sequence options.");
    }

    private LocalizedTextBlock CreateText(
        string text,
        string fontResource,
        string brushResource,
        FontWeight weight,
        LocalizedOverflowMode overflowMode,
        int maxLines)
    {
        var block = new LocalizedTextBlock
        {
            Text = text,
            FontWeight = weight,
            Foreground = GetBrush(brushResource),
            OverflowMode = overflowMode,
            MaxLines = maxLines,
        };
        if (this.TryFindResource(fontResource, out var resource) && resource is double size)
            block.FontSize = size;
        return block;
    }

    private string Get(string key, string fallback) => AvaloniaLocalization.GetString(key, fallback);

    private IBrush GetBrush(string key) =>
        this.TryFindResource(key, out var value) && value is IBrush brush
            ? brush
            : new SolidColorBrush(Colors.Transparent);

    private T GetResource<T>(string key, T fallback) =>
        this.TryFindResource(key, out var value) && value is T resource ? resource : fallback;
}

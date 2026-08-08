using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Pages.Windows;
using UniversalDeviceToolkit.Avalonia.Services;
using SharedMacroEventSnapshot = UniversalDeviceToolkit.ViewModels.MacroEventSnapshot;
using SharedMacroSlotSnapshot = UniversalDeviceToolkit.ViewModels.MacroSlotSnapshot;
using SharedMacroViewModel = UniversalDeviceToolkit.ViewModels.MacroViewModel;

namespace UniversalDeviceToolkit.Avalonia.Pages;

/// <summary>
/// Avalonia macro workspace backed by the shared MacroController. The layout is
/// transplanted from the WPF MacroPage: a numpad on the left selects the active
/// slot, and the right side hosts the sequence editor for that slot. Recording and
/// playback continue to use the existing feature-action bridge; sequence options
/// are persisted immediately to the same macro store used by WPF.
/// </summary>
public sealed class MacroPage : UserControl
{
    private readonly SharedMacroViewModel _viewModel;
    private readonly CheckBox _enabledToggle = new() { MinWidth = 48 };
    private readonly Button _stopButton = new() { MinWidth = 96 };
    private readonly LocalizedTextBlock _statusBlock = new();
    private readonly DispatcherTimer _recordingRefreshTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(250),
    };

    private readonly List<Button> _numpadButtons = [];
    private readonly List<ushort> _numpadKeys =
    [
        0x67, 0x68, 0x69,
        0x64, 0x65, 0x66,
        0x61, 0x62, 0x63,
        0x60,
    ];

    private readonly LocalizedTextBlock _editorTitle = new();
    private readonly LocalizedTextBlock _editorSummary = new();
    private readonly NumericUpDown _repeat = new();
    private readonly CheckBox _ignoreDelays = new();
    private readonly CheckBox _interrupt = new();
    private readonly ComboBox _recordingOptions = new();
    private readonly Button _playButton = new() { MinWidth = 72 };
    private readonly Button _recordButton = new() { MinWidth = 84 };
    private readonly Button _clearButton = new() { MinWidth = 72 };
    private readonly StackPanel _eventList = new() { Spacing = 4 };
    private readonly Button _addEventButton = new() { MinWidth = 96 };

    private bool _isRefreshing;
    private bool _isPreparingRecording;
    private bool _isLoaded;
    private ulong _selectedSlotKey = 0x60;
    private readonly Dictionary<ulong, List<MacroEventItem>> _editedSequences = new();

    public MacroPage(IPlatformServices platformServices)
    {
        _viewModel = new(
            new PlatformMacroController(),
            new PlatformMacroWorkspace(platformServices));
        Content = BuildContent();
        _recordingRefreshTimer.Tick += RecordingRefreshTimer_Tick;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private Control BuildContent()
    {
        var title = CreateText(
            Get("MacroPage_Title", "Macro"),
            "FontSizePageTitle",
            "TextFillColorPrimaryBrush",
            FontWeight.Medium,
            LocalizedOverflowMode.Wrap,
            2);
        var description = CreateText(
            Get("MacroPage_Description", "Create and manage device macros."),
            "FontSizePageDescription",
            "TextFillColorSecondaryBrush",
            FontWeight.Normal,
            LocalizedOverflowMode.Wrap,
            3);
        title.Margin = new Thickness(0, 16, 0, 8);
        description.Margin = new Thickness(0, 0, 0, 24);

        AutomationProperties.SetName(this, title.Text ?? "Macro");
        var header = new StackPanel { Spacing = 0 };
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

        var workspace = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            ColumnSpacing = 24,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        workspace.Children.Add(BuildNumpad());
        var editorCard = BuildEditorCard();
        Grid.SetColumn(editorCard, 1);
        workspace.Children.Add(editorCard);

        var content = new StackPanel { Spacing = 16, Margin = new Thickness(0, 0, 16, 16) };
        content.Children.Add(header);
        content.Children.Add(enabledCard);
        content.Children.Add(toolbar);
        content.Children.Add(workspace);
        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content,
        };
    }

    private Control BuildNumpad()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("56,56,56"),
            RowDefinitions = new RowDefinitions("56,56,56,56"),
        };
        for (var index = 0; index < _numpadKeys.Count; index++)
        {
            var key = _numpadKeys[index];
            var digit = key - 0x60;
            var button = new Button
            {
                Margin = new Thickness(0, 0, 8, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Content = digit.ToString(),
                FontSize = 24,
                FontWeight = FontWeight.Medium,
                Tag = (ulong)key,
            };
            AutomationProperties.SetName(
                button,
                Get($"MacroPage_Number{digit}", digit.ToString()));
            if (index < 9)
            {
                Grid.SetRow(button, index / 3);
                Grid.SetColumn(button, index % 3);
            }
            else
            {
                Grid.SetRow(button, 3);
                Grid.SetColumn(button, 1);
            }

            button.Click += NumpadButton_Click;
            _numpadButtons.Add(button);
            grid.Children.Add(button);
        }

        var card = new Border
        {
            Background = GetBrush("CardBackgroundBrush"),
            BorderBrush = GetBrush("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetResource("CornerRadiusCard", new CornerRadius(8)),
            Padding = new Thickness(12),
            Child = grid,
        };
        AutomationProperties.SetName(card, Get("MacroPage_NumberPad_AutomationName", "Macro numpad"));
        return card;
    }

    private Control BuildEditorCard()
    {
        _editorTitle.FontWeight = FontWeight.Medium;
        _editorTitle.OverflowMode = LocalizedOverflowMode.Wrap;
        _editorTitle.MaxLines = 2;
        _editorSummary.Foreground = GetBrush("TextFillColorSecondaryBrush");
        _editorSummary.OverflowMode = LocalizedOverflowMode.Wrap;
        _editorSummary.MaxLines = 3;

        _repeat.Minimum = 1;
        _repeat.Maximum = 10;
        _repeat.Increment = 1;
        _repeat.MinWidth = 72;
        _repeat.FormatString = "0x";
        _ignoreDelays.Content = Get("MacroPage_IgnoreDelays", "Ignore delays");
        _interrupt.Content = Get("MacroPage_InterruptOnOtherKey", "Interrupt on other key");

        _recordingOptions.ItemsSource = new[]
        {
            Get("MacroSequenceControl_Keyboard", "Keyboard"),
            Get("MacroSequenceControl_KeyboardMouse", "Keyboard + mouse"),
            Get("MacroSequenceControl_KeyboardMouseMovement", "Keyboard + mouse movement"),
        };
        _recordingOptions.SelectedIndex = 0;
        _recordingOptions.MinWidth = 180;
        _recordingOptions.HorizontalAlignment = HorizontalAlignment.Right;
        ToolTip.SetTip(
            _recordingOptions,
            Get("MacroSequenceControl_RecordingOptions", "Choose which input sources recording captures."));
        AutomationProperties.SetName(
            _recordingOptions,
            Get("MacroSequenceControl_RecordingOptions", "Recording options"));

        _playButton.Content = Get("Play", "Play");
        _recordButton.Content = Get("Record", "Record");
        _clearButton.Content = Get("Clear", "Clear");
        _playButton.Click += async (_, _) => await RunActionAsync($"macro-key:{_selectedSlotKey:X}");
        _recordButton.Click += async (_, _) => await StartRecordingAsync(_selectedSlotKey, _recordingOptions.SelectedIndex);
        _clearButton.Click += async (_, _) => await ClearSequenceAsync(_selectedSlotKey);
        _repeat.ValueChanged += async (_, _) => await SaveOptionsAsync(_selectedSlotKey, _repeat, _ignoreDelays, _interrupt);
        _ignoreDelays.IsCheckedChanged += async (_, _) => await SaveOptionsAsync(_selectedSlotKey, _repeat, _ignoreDelays, _interrupt);
        _interrupt.IsCheckedChanged += async (_, _) => await SaveOptionsAsync(_selectedSlotKey, _repeat, _ignoreDelays, _interrupt);

        _addEventButton.Content = Get("MacroPage_AddEvent", "Add event");
        _addEventButton.HorizontalAlignment = HorizontalAlignment.Left;
        var keyItem = new MenuItem { Header = Get("MacroPage_AddKeyEvent", "Keyboard key") };
        var mouseItem = new MenuItem { Header = Get("MacroPage_AddMouseEvent", "Mouse button") };
        var delayItem = new MenuItem { Header = Get("MacroPage_AddDelayEvent", "Delay") };
        keyItem.Click += async (_, _) => await AddKeyEventAsync(_selectedSlotKey, _repeat, _ignoreDelays, _interrupt);
        mouseItem.Click += async (_, _) => await AddMouseEventAsync(_selectedSlotKey, _repeat, _ignoreDelays, _interrupt);
        delayItem.Click += async (_, _) => await AddDelayEventAsync(_selectedSlotKey, _repeat, _ignoreDelays, _interrupt);
        var addMenu = new MenuFlyout { Items = { keyItem, mouseItem, delayItem } };
        _addEventButton.Flyout = addMenu;
        AutomationProperties.SetAutomationId(_addEventButton, $"AvaloniaMacro_{_selectedSlotKey:X}_AddEventButton");
        AutomationProperties.SetName(_addEventButton, Get("MacroPage_AddEvent", "Add event"));

        var options = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        options.Children.Add(_ignoreDelays);
        options.Children.Add(_interrupt);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        actions.Children.Add(_playButton);
        actions.Children.Add(_recordButton);
        actions.Children.Add(_clearButton);
        actions.Children.Add(_repeat);

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(_editorTitle);
        panel.Children.Add(_editorSummary);
        panel.Children.Add(options);
        panel.Children.Add(_recordingOptions);
        panel.Children.Add(actions);
        panel.Children.Add(_eventList);
        panel.Children.Add(_addEventButton);

        var card = new Border
        {
            Background = GetBrush("CardBackgroundBrush"),
            BorderBrush = GetBrush("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetResource("CornerRadiusCard", new CornerRadius(8)),
            Padding = new Thickness(16),
            MinWidth = 420,
            Child = panel,
        };
        AutomationProperties.SetName(card, Get("MacroPage_SequenceEditor_AutomationName", "Macro sequence editor"));
        return card;
    }

    private void NumpadButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ulong key })
            return;

        SelectSlot(key);
    }

    private void SelectSlot(ulong key)
    {
        _selectedSlotKey = key;
        _viewModel.SelectKey(key);
        foreach (var button in _numpadButtons)
        {
            var isSelected = button.Tag is ulong buttonKey && buttonKey == key;
            button.Classes.Set("accent", isSelected);
        }

        AutomationProperties.SetAutomationId(_addEventButton, $"AvaloniaMacro_{key:X}_AddEventButton");
        RebuildEditor();
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        await RefreshAsync();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        _recordingRefreshTimer.Stop();
    }

    private async void RecordingRefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isLoaded || _isRefreshing)
            return;

        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (_isRefreshing)
            return;

        try
        {
            _isRefreshing = true;
            var sharedState = await _viewModel.LoadWorkspaceAsync();
            if (sharedState is null)
            {
                _statusBlock.Text = _viewModel.ErrorMessage
                    ?? Get("MacroPage_ActionError", "The macro action could not be completed.");
                return;
            }

            var state = PlatformMacroWorkspace.ToPlatform(sharedState);
            _enabledToggle.IsChecked = state.IsEnabled;
            _stopButton.IsEnabled = state.IsRecording;
            _statusBlock.Text = state.IsRecording
                ? Get("MacroPage_Recording", "Recording a macro. Stop recording to save the active slot.")
                : string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    Get("MacroPage_SlotSummary", "{0} macro slots are available."),
                    state.Slots.Count);

            if (!state.Slots.Any(slot => slot.Key == _selectedSlotKey))
            {
                var preferred = state.Slots.FirstOrDefault(slot => slot.EventCount > 0);
                SelectSlot(preferred?.Key ?? 0x60);
            }
            else
            {
                RebuildEditor();
            }

            if (state.IsRecording && _isLoaded)
                _recordingRefreshTimer.Start();
            else
                _recordingRefreshTimer.Stop();
        }
        catch (Exception ex)
        {
            _statusBlock.Text = ex.Message;
            _recordingRefreshTimer.Stop();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void RebuildEditor()
    {
        var slot = _viewModel.FindSlot(_selectedSlotKey);
        if (slot is null)
        {
            _editorTitle.Text = string.Empty;
            _editorSummary.Text = Get("MacroPage_EmptySlot", "No sequence is stored for this slot.");
            _repeat.Value = 1;
            _ignoreDelays.IsChecked = false;
            _interrupt.IsChecked = false;
            _playButton.IsEnabled = false;
            _recordButton.IsEnabled = true;
            _clearButton.IsEnabled = false;
            _eventList.Children.Clear();
            return;
        }

        var snapshot = slot.ToSnapshot();
        var digit = slot.Key - 0x60;
        _editorTitle.Text = $"Numpad {digit}";
        var eventCount = slot.Events.Count;
        _editorSummary.Text = eventCount == 0
            ? Get("MacroPage_EmptySlot", "No sequence is stored for this slot.")
            : string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                Get("MacroPage_EventSummary", "{0} recorded event(s)."),
                eventCount);

        _repeat.Value = Math.Clamp(snapshot.RepeatCount, 1, 10);
        _ignoreDelays.IsChecked = snapshot.IgnoreDelays;
        _interrupt.IsChecked = snapshot.InterruptOnOtherKey;

        var isRecording = _viewModel.IsRecording;
        _repeat.IsEnabled = slot.EventCount > 0 && !isRecording;
        _ignoreDelays.IsEnabled = slot.EventCount > 0 && !isRecording;
        _interrupt.IsEnabled = slot.EventCount > 0 && !isRecording;
        _recordingOptions.IsEnabled = !isRecording;
        _playButton.IsEnabled = slot.EventCount > 0 && !isRecording;
        _recordButton.IsEnabled = !isRecording;
        _clearButton.IsEnabled = slot.EventCount > 0 && !isRecording;

        _eventList.Children.Clear();
        for (var index = 0; index < slot.Events.Count; index++)
        {
            var eventSnapshot = slot.Events[index];
            var macroEvent = new MacroEventItem(
                eventSnapshot.Source,
                eventSnapshot.Direction,
                eventSnapshot.Key,
                eventSnapshot.X,
                eventSnapshot.Y,
                eventSnapshot.Delay);
            _eventList.Children.Add(CreateEventRow(
                slot.Key,
                index,
                macroEvent,
                _repeat,
                _ignoreDelays,
                _interrupt,
                isRecording));
        }
    }

    private async void EnabledToggle_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_isRefreshing || _enabledToggle.IsChecked is not bool enabled)
            return;

        if (!await HostOperation.TryExecuteAsync(() => _viewModel.SetEnabledAsync(enabled)))
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
        var accepted = actionKey.Equals("macro-stop-recording", StringComparison.OrdinalIgnoreCase)
            ? await HostOperation.TryExecuteAsync(() => _viewModel.StopRecordingAsync())
            : FeatureActionContract.TryParseMacroPlayKey(actionKey, out var key)
                && await HostOperation.TryExecuteAsync(() => _viewModel.PlayAsync(key));
        if (!accepted)
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
            // A fresh recording replaces the slot's host sequence, so any
            // in-page edits are discarded before the new events are captured.
            _editedSequences.Remove(key);

            // The WPF host gives the user three seconds to move away from the record button
            // before mouse-movement capture starts. Preserve that behavior in the native page.
            if (mode == MacroRecordingMode.KeyboardMouseMovement)
            {
                _statusBlock.Text = Get("MacroRecordingWindow_Preparing_Title", "Preparing to record macro input...");
                await Task.Delay(TimeSpan.FromSeconds(3));
            }

            if (!await HostOperation.TryExecuteAsync(
                    () => _viewModel.StartRecordingAsync(key, ToSharedRecordingMode(mode))))
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
        if (_isRefreshing || !await HostOperation.TryExecuteAsync(() => _viewModel.ClearSequenceAsync(key)))
        {
            _statusBlock.Text = Get("MacroPage_ClearError", "Unable to clear this macro slot.");
            return;
        }

        _editedSequences.Remove(key);
        await RefreshAsync();
    }

    private Control CreateEventRow(
        ulong slotKey,
        int index,
        MacroEventItem macroEvent,
        NumericUpDown repeat,
        CheckBox ignoreDelays,
        CheckBox interrupt,
        bool isRecording)
    {
        var summary = CreateText(
            MacroEventEditing.FormatEvent(macroEvent),
            "FontSizeCaption",
            "TextFillColorSecondaryBrush",
            FontWeight.Normal,
            LocalizedOverflowMode.Ellipsis,
            1);

        var delay = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 10000,
            Increment = 100,
            FormatString = "0",
            Value = Math.Clamp((decimal)Math.Round(macroEvent.Delay.TotalMilliseconds), 0m, 10000m),
            MinWidth = 84,
            VerticalAlignment = VerticalAlignment.Center,
            IsEnabled = !isRecording,
        };
        AutomationProperties.SetAutomationId(delay, $"AvaloniaMacro_{slotKey:X}_EventRow_{index}_DelayEditor");
        ToolTip.SetTip(delay, Get("MacroPage_DelayToolTip", "Delay before this event, in milliseconds."));

        var up = new Button { Content = "\u2191", MinWidth = 28, VerticalAlignment = VerticalAlignment.Center, IsEnabled = !isRecording };
        var down = new Button { Content = "\u2193", MinWidth = 28, VerticalAlignment = VerticalAlignment.Center, IsEnabled = !isRecording };
        var remove = new Button { Content = "\u2715", MinWidth = 28, VerticalAlignment = VerticalAlignment.Center, IsEnabled = !isRecording };
        AutomationProperties.SetAutomationId(up, $"AvaloniaMacro_{slotKey:X}_EventRow_{index}_MoveUpButton");
        AutomationProperties.SetAutomationId(down, $"AvaloniaMacro_{slotKey:X}_EventRow_{index}_MoveDownButton");
        AutomationProperties.SetAutomationId(remove, $"AvaloniaMacro_{slotKey:X}_EventRow_{index}_RemoveButton");
        ToolTip.SetTip(up, Get("MacroPage_MoveUp", "Move event up"));
        ToolTip.SetTip(down, Get("MacroPage_MoveDown", "Move event down"));
        ToolTip.SetTip(remove, Get("MacroPage_RemoveEvent", "Remove event"));

        delay.ValueChanged += async (_, _) =>
        {
            if (_isRefreshing || delay.Value is not decimal value)
                return;

            var list = EnsureEdited(slotKey);
            if (index >= list.Count)
                return;

            list[index] = MacroEventEditing.WithDelay(list[index], TimeSpan.FromMilliseconds((double)value));
            await SaveEditedSequenceAsync(slotKey, repeat, ignoreDelays, interrupt);
        };
        up.Click += async (_, _) =>
        {
            if (_isRefreshing || !MacroEventEditing.MoveEventUp(EnsureEdited(slotKey), index))
                return;

            await SaveEditedSequenceAsync(slotKey, repeat, ignoreDelays, interrupt);
        };
        down.Click += async (_, _) =>
        {
            if (_isRefreshing || !MacroEventEditing.MoveEventDown(EnsureEdited(slotKey), index))
                return;

            await SaveEditedSequenceAsync(slotKey, repeat, ignoreDelays, interrupt);
        };
        remove.Click += async (_, _) =>
        {
            if (_isRefreshing || !MacroEventEditing.RemoveEventAt(EnsureEdited(slotKey), index))
                return;

            await SaveEditedSequenceAsync(slotKey, repeat, ignoreDelays, interrupt);
        };

        Button? capture = null;
        if (MacroEventEditing.CanCapture(macroEvent))
        {
            capture = new Button
            {
                Content = Get("MacroPage_Capture", "Capture"),
                MinWidth = 72,
                VerticalAlignment = VerticalAlignment.Center,
                IsEnabled = !isRecording,
            };
            AutomationProperties.SetAutomationId(capture, $"AvaloniaMacro_{slotKey:X}_EventRow_{index}_CaptureButton");
            ToolTip.SetTip(capture, Get("MacroPage_CaptureDescription", "Capture the next key or mouse button for this event."));
            capture.Click += async (_, _) => await CaptureForEventAsync(slotKey, index, repeat, ignoreDelays, interrupt);
        }

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto"), ColumnSpacing = 6 };
        row.Children.Add(summary);
        if (capture is not null)
        {
            Grid.SetColumn(capture, 1);
            row.Children.Add(capture);
        }

        Grid.SetColumn(delay, capture is null ? 1 : 2);
        row.Children.Add(delay);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { up, down, remove },
        };
        Grid.SetColumn(buttons, 3);
        row.Children.Add(buttons);

        AutomationProperties.SetAutomationId(row, $"AvaloniaMacro_{slotKey:X}_EventRow_{index}");
        AutomationProperties.SetName(row, summary.Text);
        return row;
    }

    private Control CreateAddEventButton(
        ulong slotKey,
        NumericUpDown repeat,
        CheckBox ignoreDelays,
        CheckBox interrupt,
        bool isRecording)
    {
        var add = new Button
        {
            Content = Get("MacroPage_AddEvent", "Add event"),
            MinWidth = 96,
            HorizontalAlignment = HorizontalAlignment.Left,
            IsEnabled = !isRecording,
        };
        var keyItem = new MenuItem { Header = Get("MacroPage_AddKeyEvent", "Keyboard key") };
        var mouseItem = new MenuItem { Header = Get("MacroPage_AddMouseEvent", "Mouse button") };
        var delayItem = new MenuItem { Header = Get("MacroPage_AddDelayEvent", "Delay") };
        keyItem.Click += async (_, _) => await AddKeyEventAsync(slotKey, repeat, ignoreDelays, interrupt);
        mouseItem.Click += async (_, _) => await AddMouseEventAsync(slotKey, repeat, ignoreDelays, interrupt);
        delayItem.Click += async (_, _) => await AddDelayEventAsync(slotKey, repeat, ignoreDelays, interrupt);
        var menu = new MenuFlyout { Items = { keyItem, mouseItem, delayItem } };
        add.Flyout = menu;
        AutomationProperties.SetAutomationId(add, $"AvaloniaMacro_{slotKey:X}_AddEventButton");
        AutomationProperties.SetName(add, Get("MacroPage_AddEvent", "Add event"));
        return add;
    }

    private List<MacroEventItem> EnsureEdited(ulong key)
    {
        if (!_editedSequences.TryGetValue(key, out var events))
        {
            events = new List<MacroEventItem>();
            _editedSequences[key] = events;
        }

        return events;
    }

    private void SeedEditedSequence(MacroSlotState slot)
    {
        if (slot.Events is { Count: > 0 } && !_editedSequences.ContainsKey(slot.Key))
            _editedSequences[slot.Key] = [.. slot.Events];
    }

    private async Task AddKeyEventAsync(ulong slotKey, NumericUpDown repeat, CheckBox ignoreDelays, CheckBox interrupt)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var result = await MacroKeyCaptureWindow.CaptureAsync(owner);
        if (result is null || _isRefreshing)
            return;

        MacroEventEditing.AddEvents(EnsureEdited(slotKey), MacroEventEditing.CreatePress(result));
        await SaveEditedSequenceAsync(slotKey, repeat, ignoreDelays, interrupt);
    }

    private async Task AddMouseEventAsync(ulong slotKey, NumericUpDown repeat, CheckBox ignoreDelays, CheckBox interrupt)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var result = await MacroKeyCaptureWindow.CaptureAsync(owner);
        if (result is null || _isRefreshing)
            return;

        MacroEventEditing.AddEvents(EnsureEdited(slotKey), MacroEventEditing.CreatePress(result));
        await SaveEditedSequenceAsync(slotKey, repeat, ignoreDelays, interrupt);
    }

    private async Task AddDelayEventAsync(ulong slotKey, NumericUpDown repeat, CheckBox ignoreDelays, CheckBox interrupt)
    {
        if (_isRefreshing)
            return;

        MacroEventEditing.AddEvent(
            EnsureEdited(slotKey),
            MacroEventEditing.CreateDelayEvent(TimeSpan.FromMilliseconds(250)));
        await SaveEditedSequenceAsync(slotKey, repeat, ignoreDelays, interrupt);
    }

    private async Task CaptureForEventAsync(ulong slotKey, int index, NumericUpDown repeat, CheckBox ignoreDelays, CheckBox interrupt)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var result = await MacroKeyCaptureWindow.CaptureAsync(owner);
        if (result is null || _isRefreshing)
            return;

        var list = EnsureEdited(slotKey);
        if (index < 0 || index >= list.Count)
            return;

        if (!MacroEventEditing.ReplaceCapturedPress(list, index, result))
        {
            _statusBlock.Text = Get("MacroPage_CaptureError", "Unable to replace this macro event.");
            return;
        }

        await SaveEditedSequenceAsync(slotKey, repeat, ignoreDelays, interrupt);
    }

    private async Task SaveEditedSequenceAsync(ulong key, NumericUpDown repeat, CheckBox ignoreDelays, CheckBox interrupt) =>
        await SaveSequenceAsync(key, repeat, ignoreDelays, interrupt);

    private async Task SaveOptionsAsync(
        ulong key,
        NumericUpDown repeat,
        CheckBox ignoreDelays,
        CheckBox interrupt) =>
        await SaveSequenceAsync(key, repeat, ignoreDelays, interrupt);

    private async Task SaveSequenceAsync(ulong key, NumericUpDown repeat, CheckBox ignoreDelays, CheckBox interrupt)
    {
        if (_isRefreshing || repeat.Value is not decimal repeatValue
            || ignoreDelays.IsChecked is not bool ignore
            || interrupt.IsChecked is not bool stop)
            return;

        var repeatCount = (int)Math.Round(repeatValue);
        var hasEditedSequence = _editedSequences.TryGetValue(key, out var events);
        var saved = await HostOperation.TryExecuteAsync(() => ShouldPersistEditedSequence(hasEditedSequence)
            ? _viewModel.SaveSequenceAsync(new SharedMacroSlotSnapshot(
                key,
                repeatCount,
                ignore,
                stop,
                events!.Select(ToSharedEvent).ToArray()))
            : _viewModel.SetSequenceOptionsAsync(key, repeatCount, ignore, stop));
        if (!saved)
            _statusBlock.Text = Get("MacroPage_OptionsError", "Unable to save macro sequence options.");

        await RefreshAsync();
    }

    internal static bool ShouldPersistEditedSequence(bool hasEditedSequence) => hasEditedSequence;

    private static UniversalDeviceToolkit.ViewModels.MacroRecordingMode ToSharedRecordingMode(MacroRecordingMode mode) => mode switch
    {
        MacroRecordingMode.KeyboardMouse => UniversalDeviceToolkit.ViewModels.MacroRecordingMode.KeyboardMouse,
        MacroRecordingMode.KeyboardMouseMovement => UniversalDeviceToolkit.ViewModels.MacroRecordingMode.KeyboardMouseMovement,
        _ => UniversalDeviceToolkit.ViewModels.MacroRecordingMode.Keyboard,
    };

    private static SharedMacroEventSnapshot ToSharedEvent(MacroEventItem macroEvent) => new(
        macroEvent.Source,
        macroEvent.Direction,
        macroEvent.Key,
        macroEvent.X,
        macroEvent.Y,
        macroEvent.Delay);

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

/// <summary>
/// Host-neutral editing model for a macro event sequence. These helpers operate
/// on the <see cref="MacroEventItem"/> projection so the page and its tests can
/// add, remove, reorder and edit events without depending on the Windows macro
/// controller. The host model has no dedicated delay event; a delay-only item
/// is a keyboard item with no key code and an unknown direction.
/// </summary>
public static class MacroEventEditing
{
    public const string KeyboardSource = "Keyboard";
    public const string MouseSource = "Mouse";
    public const string DelayOnlyDirection = "Unknown";

    public static MacroEventItem CreateKeyboardEvent(uint keyCode, TimeSpan delay = default) =>
        new(KeyboardSource, "Down", keyCode, 0, 0, delay);

    public static MacroEventItem CreateMouseEvent(uint button, TimeSpan delay = default) =>
        new(MouseSource, "Down", button, 0, 0, delay);

    public static MacroEventItem CreateDelayEvent(TimeSpan delay) =>
        new(KeyboardSource, DelayOnlyDirection, 0, 0, 0, delay);

    public static MacroEventItem FromCapture(MacroKeyCaptureWindow.CaptureResult result, TimeSpan delay = default)
    {
        if (result is null)
            throw new ArgumentNullException(nameof(result));

        return new MacroEventItem(result.Source, result.Direction, result.Key, result.X, result.Y, delay);
    }

    public static IReadOnlyList<MacroEventItem> CreatePress(MacroKeyCaptureWindow.CaptureResult result)
    {
        var down = FromCapture(result);
        return [down, down with { Direction = "Up" }];
    }

    public static bool IsDelayOnlyEvent(MacroEventItem item) => item is not null
        && item.Source == KeyboardSource
        && item.Direction == DelayOnlyDirection
        && item.Key == 0;

    public static bool CanCapture(MacroEventItem item) => item is not null && !IsDelayOnlyEvent(item);

    public static bool AddEvent(IList<MacroEventItem> events, MacroEventItem item)
    {
        if (events is null || item is null)
            return false;

        events.Add(item);
        return true;
    }

    public static bool AddEvents(IList<MacroEventItem> events, IEnumerable<MacroEventItem> items)
    {
        if (events is null || items is null)
            return false;

        var additions = items.Where(item => item is not null).ToArray();
        if (additions.Length == 0)
            return false;

        foreach (var item in additions)
            events.Add(item);
        return true;
    }

    public static bool RemoveEventAt(IList<MacroEventItem> events, int index)
    {
        if (events is null || index < 0 || index >= events.Count)
            return false;

        events.RemoveAt(index);
        return true;
    }

    public static bool MoveEventUp(IList<MacroEventItem> events, int index)
    {
        if (events is null || index <= 0 || index >= events.Count)
            return false;

        (events[index], events[index - 1]) = (events[index - 1], events[index]);
        return true;
    }

    public static bool MoveEventDown(IList<MacroEventItem> events, int index)
    {
        if (events is null || index < 0 || index >= events.Count - 1)
            return false;

        (events[index], events[index + 1]) = (events[index + 1], events[index]);
        return true;
    }

    public static MacroEventItem WithDelay(MacroEventItem item, TimeSpan delay) => item with { Delay = delay };

    public static MacroEventItem WithCapturedInput(MacroEventItem item, MacroKeyCaptureWindow.CaptureResult result)
    {
        if (result is null)
            throw new ArgumentNullException(nameof(result));

        return item with
        {
            Source = result.Source,
            Direction = result.Direction,
            Key = result.Key,
            X = result.X,
            Y = result.Y,
        };
    }

    public static bool ReplaceCapturedPress(
        IList<MacroEventItem> events,
        int index,
        MacroKeyCaptureWindow.CaptureResult result)
    {
        if (events is null || result is null || index < 0 || index >= events.Count)
            return false;

        var downIndex = events[index].Direction == "Down" ? index : index - 1;
        var upIndex = downIndex + 1;
        if (downIndex < 0 || upIndex >= events.Count || !IsPressPair(events[downIndex], events[upIndex]))
            return false;

        events[downIndex] = WithCapturedInput(events[downIndex], result) with { Direction = "Down" };
        events[upIndex] = WithCapturedInput(events[upIndex], result) with { Direction = "Up" };
        return true;
    }

    private static bool IsPressPair(MacroEventItem down, MacroEventItem up) =>
        down.Direction == "Down"
        && up.Direction == "Up"
        && string.Equals(down.Source, up.Source, StringComparison.OrdinalIgnoreCase)
        && down.Key == up.Key;

    public static string FormatEvent(MacroEventItem macroEvent)
    {
        if (macroEvent is null)
            return string.Empty;

        if (IsDelayOnlyEvent(macroEvent))
            return $"+{macroEvent.Delay.TotalMilliseconds:0} ms delay";

        var location = macroEvent.X == 0 && macroEvent.Y == 0
            ? string.Empty
            : $" ({macroEvent.X}, {macroEvent.Y})";
        return $"{macroEvent.Source} {macroEvent.Direction} | {macroEvent.Key}{location} | +{macroEvent.Delay.TotalMilliseconds:0} ms";
    }
}

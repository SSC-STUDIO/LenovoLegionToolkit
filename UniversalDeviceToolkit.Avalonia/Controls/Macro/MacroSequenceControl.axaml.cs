using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Windows.Macro;
using UniversalDeviceToolkit.Lib.Macro;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Controls.Macro
{
public partial class MacroSequenceControl : global::Avalonia.Controls.UserControl
{
    private readonly MacroController _controller = IoCContainer.Resolve<MacroController>();

    private MacroRecordingWindow? _recordingWindow;
    private MacroIdentifier _macroIdentifier;
    private bool _isRefreshing;

    public MacroSequenceControl()
    {
        InitializeComponent();

        _controller.RecorderReceived += Controller_RecorderReceived;
        _controller.RecorderStopped += Controller_RecorderStopped;
        Unloaded += MacroSequenceControl_Unloaded;
    }

    private void MacroSequenceControl_Unloaded(object? sender, RoutedEventArgs e)
    {
        _controller.RecorderReceived -= Controller_RecorderReceived;
        _controller.RecorderStopped -= Controller_RecorderStopped;
    }

    public void Set(MacroIdentifier macroIdentifier)
    {
        _isRefreshing = true;

        _macroIdentifier = macroIdentifier;

        _controller.StopRecording();

        SetWaitCursor(false);

        var sequence = _controller.GetSequences().GetValueOrDefault(_macroIdentifier);
        var sequenceHasEvents = sequence.Events?.Length > 0;

        _repeatCard.IsEnabled = sequenceHasEvents;
        _ignoreDelaysCard.IsEnabled = sequenceHasEvents;
        _interruptOnOtherKeyCard.IsEnabled = sequenceHasEvents;

        _settingsComboBox.SetItems([MacroRecorderSettings.Keyboard, MacroRecorderSettings.Keyboard | MacroRecorderSettings.Mouse, MacroRecorderSettings.Keyboard | MacroRecorderSettings.Mouse | MacroRecorderSettings.Movement],
            MacroRecorderSettings.Keyboard,
            v => v switch
            {
                MacroRecorderSettings.Keyboard => Resource.MacroSequenceControl_Keyboard,
                MacroRecorderSettings.Keyboard | MacroRecorderSettings.Mouse => Resource.MacroSequenceControl_KeyboardMouse,
                MacroRecorderSettings.Keyboard | MacroRecorderSettings.Mouse | MacroRecorderSettings.Movement => Resource.MacroSequenceControl_KeyboardMouseMovement,
                _ => string.Empty
            });
        _repeatComboBox.SetItems(MacroController.AllowedRepeatCounts,
            Math.Clamp(sequence.RepeatCount, 1, 10),
            v => v == 1 ? Resource.MacroSequenceControl_DontRepeat : v.ToString());
        _ignoreDelaysToggle.IsChecked = sequence.IgnoreDelays;
        _interruptOnOtherKeyToggle.IsChecked = sequence.InterruptOnOtherKey;

        _recordButton.IsEnabled = true;
        _clearButton.IsVisible = sequenceHasEvents ? true : false;

        _macroEventsPanel.Children.Clear();
        foreach (var macroEvent in sequence.Events ?? [])
            CreateControl(macroEvent);

        _isRefreshing = false;
    }

    private void CreateControl(MacroEvent macroEvent)
    {
        if (macroEvent.Direction is MacroDirection.Move)
        {
            if (_macroEventsPanel.Children.OfType<AbstractMacroEventControl>().LastOrDefault() is MultiAbstractMacroEventControl last)
            {
                last.Set(macroEvent);
            }
            else
            {
                var macroEventControl = new MultiAbstractMacroEventControl();
                macroEventControl.Set(macroEvent);
                _macroEventsPanel.Children.Add(macroEventControl);
            }
        }
        else
        {
            var macroEventControl = new SingleAbstractMacroEventControl();
            macroEventControl.Set(macroEvent);
            _macroEventsPanel.Children.Add(macroEventControl);
        }
    }

    private void Controller_RecorderReceived(object? sender, MacroController.RecorderReceivedEventArgs e) => CreateControl(e.MacroEvent);

    private void Controller_RecorderStopped(object? sender, MacroController.RecorderStoppedEventArgs e)
    {
        _recordingWindow?.Close();
        _recordingWindow = null;

        if (e.Interrupted)
            Clear();
        else
            Save();
    }

    private void RepeatComboBox_SelectionChanged(object? sender, RoutedEventArgs e) => Save();

    private void IgnoreDelaysToggle_Click(object? sender, RoutedEventArgs e) => Save();

    private void InterruptOnOtherKeyToggle_Click(object? sender, RoutedEventArgs e) => Save();

    private void ClearButton_Click(object? sender, RoutedEventArgs e) => Clear();

    private async void RecordButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!_settingsComboBox.TryGetSelectedItem(out MacroRecorderSettings settings))
                return;

            await RecordAsync(settings);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(RecordButton_Click)}.", ex);
        }
    }

    private async Task RecordAsync(MacroRecorderSettings settings)
    {
        _macroEventsPanel.Children.Clear();
        _recordButton.IsEnabled = false;
        _clearButton.IsVisible = false;

        SetWaitCursor(true);

        if (settings.HasFlag(MacroRecorderSettings.Mouse) && settings.HasFlag(MacroRecorderSettings.Movement))
        {
            var preparingWindow = MacroRecordingWindow.CreatePreparing();
            ShowRecordingWindow(preparingWindow);

            await Task.Delay(TimeSpan.FromSeconds(3));

            preparingWindow.Close();
        }

        _recordingWindow = MacroRecordingWindow.CreateRecording();
        ShowRecordingWindow(_recordingWindow);

        _controller.StartRecording(settings);
    }

    // AVALONIA: Window.Owner is read-only; the owner is passed to Show instead.
    private void ShowRecordingWindow(MacroRecordingWindow window)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? UdtAppContext.MainWindow;
        if (owner is not null)
            window.Show(owner);
        else
            window.Show();
    }

    private void Clear()
    {
        _macroEventsPanel.Children.Clear();

        Save();
    }

    private void Save()
    {
        if (_isRefreshing)
            return;

        SetWaitCursor(false);

        var repeatCount = _repeatComboBox.TryGetSelectedItem(out int repeat) ? repeat : 1;
        var ignoreDelays = _ignoreDelaysToggle.IsChecked ?? false;
        var interruptOnOtherKey = _interruptOnOtherKeyToggle.IsChecked ?? false;
        var macroEvents = _macroEventsPanel.Children
            .OfType<AbstractMacroEventControl>()
            .SelectMany(c => c.GetEvents())
            .ToArray();

        var sequences = _controller.GetSequences();
        sequences[_macroIdentifier] = new MacroSequence
        {
            RepeatCount = repeatCount,
            IgnoreDelays = ignoreDelays,
            InterruptOnOtherKey = interruptOnOtherKey,
            Events = macroEvents
        };
        _controller.SetSequences(sequences);

        Set(_macroIdentifier);
    }

    // AVALONIA: WPF Mouse.OverrideCursor does not exist; the wait cursor is applied
    // to this control instead of globally (behavioral approximation).
    private void SetWaitCursor(bool isWaiting) => Cursor = isWaiting ? new Cursor(StandardCursorType.Wait) : null;
}
}

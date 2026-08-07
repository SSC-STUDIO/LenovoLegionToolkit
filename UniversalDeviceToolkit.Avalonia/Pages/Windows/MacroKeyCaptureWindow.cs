using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;

namespace UniversalDeviceToolkit.Avalonia.Pages.Windows;

/// <summary>
/// Small modal dialog that waits for the next keyboard or mouse input and
/// reports it as a host-neutral macro event projection. A 3-2-1 countdown
/// precedes the listening state so the user can release the button that
/// opened the dialog, mirroring the WPF recording pre-roll.
/// </summary>
public sealed class MacroKeyCaptureWindow : Window
{
    /// <summary>Host-neutral projection of one captured input.</summary>
    public sealed record CaptureResult(string Source, string Direction, uint Key, int X, int Y)
    {
        public static CaptureResult FromKeyboard(uint keyCode) => new("Keyboard", "Down", keyCode, 0, 0);

        public static CaptureResult FromMouseButton(uint button) => new("Mouse", "Down", button, 0, 0);
    }

    public const int CountdownSeconds = 3;

    private readonly TaskCompletionSource<CaptureResult?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly DispatcherTimer _countdownTimer;
    private readonly LocalizedTextBlock _statusBlock;
    private int _countdownRemaining = CountdownSeconds;
    private bool _isListening;

    private MacroKeyCaptureWindow()
    {
        Title = Get("MacroKeyCaptureWindow_Title", "Capture macro input");
        Width = 380;
        Height = 220;
        MinWidth = 340;
        MinHeight = 200;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Topmost = true;
        ShowInTaskbar = false;
        AutomationProperties.SetAutomationId(this, "AvaloniaMacroKeyCaptureWindow");
        AutomationProperties.SetName(this, Title);

        var title = new LocalizedTextBlock
        {
            Text = Title,
            FontSize = 20,
            FontWeight = FontWeight.Medium,
            Foreground = GetBrush("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        _statusBlock = new LocalizedTextBlock
        {
            Foreground = GetBrush("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        AutomationProperties.SetAutomationId(_statusBlock, "AvaloniaMacroKeyCaptureStatusText");

        var cancel = new Button
        {
            Content = Get("Cancel", "Cancel"),
            MinWidth = 96,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        cancel.Click += (_, _) => Complete(null);
        AutomationProperties.SetAutomationId(cancel, "AvaloniaMacroKeyCaptureCancelButton");
        AutomationProperties.SetName(cancel, Get("Cancel", "Cancel"));

        Content = new StackPanel
        {
            Spacing = 14,
            Margin = new Thickness(24),
            VerticalAlignment = VerticalAlignment.Center,
            Children = { title, _statusBlock, cancel },
        };

        _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _countdownTimer.Tick += CountdownTimer_Tick;

        KeyDown += OnKeyDown;
        PointerPressed += OnPointerPressed;
        Opened += (_, _) => StartCountdown();
        Closed += (_, _) => Complete(null);
    }

    /// <summary>
    /// Opens the dialog and completes when the user captures input or cancels.
    /// A cancelled capture returns null.
    /// </summary>
    public static Task<CaptureResult?> CaptureAsync(Window? owner)
    {
        var window = new MacroKeyCaptureWindow();
        if (owner is null)
            window.Show();
        else
            _ = window.ShowDialog(owner);
        return window._completion.Task;
    }

    /// <summary>
    /// Maps an Avalonia logical key to its Win32 virtual-key code. Keys without
    /// a mapping are ignored by the capture flow.
    /// </summary>
    public static bool TryGetKeyCode(Key key, out uint keyCode)
    {
        if (KeyCodeMapping.TryGetValue(key, out keyCode))
            return true;

        keyCode = 0;
        return false;
    }

    private void StartCountdown()
    {
        _countdownRemaining = CountdownSeconds;
        _isListening = false;
        UpdateCountdownText();
        _countdownTimer.Start();
    }

    private void CountdownTimer_Tick(object? sender, EventArgs e)
    {
        _countdownRemaining--;
        if (_countdownRemaining > 0)
        {
            UpdateCountdownText();
            return;
        }

        _countdownTimer.Stop();
        _isListening = true;
        _statusBlock.Text = Get(
            "MacroKeyCaptureWindow_Listening",
            "Listening... press a key or click a mouse button.");
    }

    private void UpdateCountdownText() => _statusBlock.Text = string.Format(
        CultureInfo.CurrentCulture,
        Get("MacroKeyCaptureWindow_Countdown", "Capturing in {0}..."),
        _countdownRemaining);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_isListening || e.Key is Key.None or Key.Return or Key.Space or Key.Escape)
            return;

        if (TryGetKeyCode(e.Key, out var keyCode))
            Complete(CaptureResult.FromKeyboard(keyCode));
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_isListening || e.Source is Button)
            return;

        var button = e.GetCurrentPoint(this).Properties.PointerUpdateKind switch
        {
            PointerUpdateKind.LeftButtonPressed => 1u,
            PointerUpdateKind.RightButtonPressed => 2u,
            PointerUpdateKind.MiddleButtonPressed => 3u,
            PointerUpdateKind.XButton1Pressed => 0x00010001u,
            PointerUpdateKind.XButton2Pressed => 0x00020001u,
            _ => 0u,
        };
        if (button != 0)
            Complete(CaptureResult.FromMouseButton(button));
    }

    private void Complete(CaptureResult? result)
    {
        if (!_completion.TrySetResult(result))
            return;

        _countdownTimer.Stop();
        Close();
    }

    private static string Get(string key, string fallback) => AvaloniaLocalization.GetString(key, fallback);

    private IBrush GetBrush(string key) =>
        this.TryFindResource(key, out var value) && value is IBrush brush
            ? brush
            : new SolidColorBrush(Colors.Transparent);

    private static readonly Dictionary<Key, uint> KeyCodeMapping = new()
    {
        [Key.Back] = 0x08,
        [Key.Tab] = 0x09,
        [Key.Clear] = 0x0C,
        [Key.Return] = 0x0D,
        [Key.Pause] = 0x13,
        [Key.CapsLock] = 0x14,
        [Key.Escape] = 0x1B,
        [Key.Space] = 0x20,
        [Key.PageUp] = 0x21,
        [Key.PageDown] = 0x22,
        [Key.End] = 0x23,
        [Key.Home] = 0x24,
        [Key.Left] = 0x25,
        [Key.Up] = 0x26,
        [Key.Right] = 0x27,
        [Key.Down] = 0x28,
        [Key.Snapshot] = 0x2C,
        [Key.Insert] = 0x2D,
        [Key.Delete] = 0x2E,
        [Key.D0] = 0x30,
        [Key.D1] = 0x31,
        [Key.D2] = 0x32,
        [Key.D3] = 0x33,
        [Key.D4] = 0x34,
        [Key.D5] = 0x35,
        [Key.D6] = 0x36,
        [Key.D7] = 0x37,
        [Key.D8] = 0x38,
        [Key.D9] = 0x39,
        [Key.A] = 0x41,
        [Key.B] = 0x42,
        [Key.C] = 0x43,
        [Key.D] = 0x44,
        [Key.E] = 0x45,
        [Key.F] = 0x46,
        [Key.G] = 0x47,
        [Key.H] = 0x48,
        [Key.I] = 0x49,
        [Key.J] = 0x4A,
        [Key.K] = 0x4B,
        [Key.L] = 0x4C,
        [Key.M] = 0x4D,
        [Key.N] = 0x4E,
        [Key.O] = 0x4F,
        [Key.P] = 0x50,
        [Key.Q] = 0x51,
        [Key.R] = 0x52,
        [Key.S] = 0x53,
        [Key.T] = 0x54,
        [Key.U] = 0x55,
        [Key.V] = 0x56,
        [Key.W] = 0x57,
        [Key.X] = 0x58,
        [Key.Y] = 0x59,
        [Key.Z] = 0x5A,
        [Key.LWin] = 0x5B,
        [Key.RWin] = 0x5C,
        [Key.Apps] = 0x5D,
        [Key.NumPad0] = 0x60,
        [Key.NumPad1] = 0x61,
        [Key.NumPad2] = 0x62,
        [Key.NumPad3] = 0x63,
        [Key.NumPad4] = 0x64,
        [Key.NumPad5] = 0x65,
        [Key.NumPad6] = 0x66,
        [Key.NumPad7] = 0x67,
        [Key.NumPad8] = 0x68,
        [Key.NumPad9] = 0x69,
        [Key.Multiply] = 0x6A,
        [Key.Add] = 0x6B,
        [Key.Separator] = 0x6C,
        [Key.Subtract] = 0x6D,
        [Key.Decimal] = 0x6E,
        [Key.Divide] = 0x6F,
        [Key.F1] = 0x70,
        [Key.F2] = 0x71,
        [Key.F3] = 0x72,
        [Key.F4] = 0x73,
        [Key.F5] = 0x74,
        [Key.F6] = 0x75,
        [Key.F7] = 0x76,
        [Key.F8] = 0x77,
        [Key.F9] = 0x78,
        [Key.F10] = 0x79,
        [Key.F11] = 0x7A,
        [Key.F12] = 0x7B,
        [Key.F13] = 0x7C,
        [Key.F14] = 0x7D,
        [Key.F15] = 0x7E,
        [Key.F16] = 0x7F,
        [Key.F17] = 0x80,
        [Key.F18] = 0x81,
        [Key.F19] = 0x82,
        [Key.F20] = 0x83,
        [Key.F21] = 0x84,
        [Key.F22] = 0x85,
        [Key.F23] = 0x86,
        [Key.F24] = 0x87,
        [Key.NumLock] = 0x90,
        [Key.Scroll] = 0x91,
        [Key.LeftShift] = 0xA0,
        [Key.RightShift] = 0xA1,
        [Key.LeftCtrl] = 0xA2,
        [Key.RightCtrl] = 0xA3,
        [Key.LeftAlt] = 0xA4,
        [Key.RightAlt] = 0xA5,
        [Key.BrowserBack] = 0xA6,
        [Key.BrowserForward] = 0xA7,
        [Key.BrowserRefresh] = 0xA8,
        [Key.BrowserStop] = 0xA9,
        [Key.BrowserSearch] = 0xAA,
        [Key.BrowserFavorites] = 0xAB,
        [Key.BrowserHome] = 0xAC,
        [Key.VolumeMute] = 0xAD,
        [Key.VolumeDown] = 0xAE,
        [Key.VolumeUp] = 0xAF,
        [Key.MediaNextTrack] = 0xB0,
        [Key.MediaPreviousTrack] = 0xB1,
        [Key.MediaStop] = 0xB2,
        [Key.MediaPlayPause] = 0xB3,
        [Key.LaunchMail] = 0xB4,
        [Key.SelectMedia] = 0xB5,
        [Key.LaunchApplication1] = 0xB6,
        [Key.LaunchApplication2] = 0xB7,
        [Key.OemSemicolon] = 0xBA,
        [Key.OemPlus] = 0xBB,
        [Key.OemComma] = 0xBC,
        [Key.OemMinus] = 0xBD,
        [Key.OemPeriod] = 0xBE,
        [Key.OemQuestion] = 0xBF,
        [Key.OemTilde] = 0xC0,
        [Key.OemOpenBrackets] = 0xDB,
        [Key.OemPipe] = 0xDC,
        [Key.OemCloseBrackets] = 0xDD,
        [Key.OemQuotes] = 0xDE,
        [Key.OemBackslash] = 0xE2,
    };
}

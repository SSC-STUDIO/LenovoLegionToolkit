#if WINDOWS

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Startup;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Messaging;
using UniversalDeviceToolkit.Lib.Messaging.Messages;
using UniversalDeviceToolkit.Lib.Notifications;
using UniversalDeviceToolkit.Lib.Settings;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Native Avalonia presentation bridge for the shared notification buses. The
/// feature layer continues to publish messages without taking a UI dependency,
/// while this host applies the same user notification preferences as WPF.
/// </summary>
internal sealed class AvaloniaNotificationManager : IDisposable
{
    private const double ToastWidth = 380;
    private const double ToastHeight = 124;
    private const int ToastMargin = 20;

    private readonly ApplicationSettings _settings;
    private readonly Func<MainWindow?> _mainWindowProvider;
    private readonly IAppNotificationService _appNotifications;
    private readonly List<ToastRequest> _pending = [];
    private readonly Dictionary<Guid, ToastRequest> _tracked = [];
    private readonly Dictionary<Guid, List<AvaloniaToastWindow>> _visible = [];
    private bool _showing;
    private bool _disposed;

    public AvaloniaNotificationManager(
        ApplicationSettings settings,
        Func<MainWindow?> mainWindowProvider,
        IAppNotificationService appNotifications)
    {
        _settings = settings;
        _mainWindowProvider = mainWindowProvider;
        _appNotifications = appNotifications;

        MessagingCenter.Subscribe<NotificationMessage>(this, OnNotificationReceived);
        _appNotifications.Changed += OnAppNotificationChanged;
    }

    private void OnNotificationReceived(NotificationMessage notification) =>
        Dispatch(() => EnqueueLegacyNotification(notification));

    private void OnAppNotificationChanged(object? sender, AppNotificationChangedEventArgs args) =>
        Dispatch(() => ApplyAppNotificationChanged(args));

    private static void Dispatch(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(action, DispatcherPriority.Normal);
    }

    private void EnqueueLegacyNotification(NotificationMessage notification)
    {
        if (_disposed || _settings.Store.DontShowNotifications || !IsNotificationTypeEnabled(notification.Type))
            return;

        var request = CreateLegacyRequest(notification);
        if (request is null)
            return;

        EnqueueByPriority(request);
        ShowNext();
    }

    private void ApplyAppNotificationChanged(AppNotificationChangedEventArgs args)
    {
        if (_disposed || args.Notification.Id == Guid.Empty)
            return;

        if (args.IsDismiss)
        {
            Dismiss(args.Notification.Id);
            return;
        }

        if (_tracked.TryGetValue(args.Notification.Id, out var existing))
        {
            existing.Apply(args.Notification, args.MergeCount, ResolveAppDuration(args.Notification));
            if (_visible.TryGetValue(args.Notification.Id, out var windows))
            {
                foreach (var window in windows.ToArray())
                    window.Update(existing);
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(args.Notification.Title) || ShouldSuppress(args.Notification.Severity))
            return;

        var request = new ToastRequest(
            args.Notification.Id,
            args.Notification.Title,
            args.Notification.Message,
            args.Notification.Severity,
            ResolveAppIcon(args.Notification.Severity),
            ResolveAppDuration(args.Notification),
            args.Notification.IsPersistent,
            args.MergeCount,
            args.Notification.ProgressPercent,
            null);
        _tracked.Add(request.Id!.Value, request);
        EnqueueByPriority(request);
        TryPlaySound();
        ShowNext();
    }

    private void Dismiss(Guid id)
    {
        _pending.RemoveAll(request => request.Id == id);
        _tracked.Remove(id);

        if (!_visible.Remove(id, out var windows))
            return;

        foreach (var window in windows.ToArray())
            window.Close();
    }

    private void ShowNext()
    {
        if (_disposed || _showing || _pending.Count == 0)
            return;

        var owner = _mainWindowProvider();
        if (owner is null)
            return;

        var request = _pending[0];
        _pending.RemoveAt(0);
        var screens = ResolveScreens(owner).ToArray();
        if (screens.Length == 0)
        {
            RemoveTrackedRequest(request);
            ShowNext();
            return;
        }

        if (ShouldSuppressForFullscreen(_settings.Store.NotificationAlwaysOnTop, IsAnyApplicationFullscreen(owner)))
        {
            RemoveTrackedRequest(request);
            ShowNext();
            return;
        }

        _showing = true;
        var windows = new List<AvaloniaToastWindow>(screens.Length);
        var remaining = screens.Length;
        foreach (var screen in screens)
        {
            var toast = new AvaloniaToastWindow(request, _settings.Store.NotificationAlwaysOnTop);
            PositionToast(toast, screen, _settings.Store.NotificationPosition);
            toast.Closed += (_, _) =>
            {
                remaining--;
                if (remaining > 0)
                    return;

                if (request.Id is { } id)
                    _visible.Remove(id);
                RemoveTrackedRequest(request);
                _showing = false;
                ShowNext();
            };
            windows.Add(toast);

            // A hidden owner should not suppress system notifications. Showing
            // without ownership lets a tray-resident process still notify users.
            if (owner.IsVisible)
                toast.Show(owner);
            else
                toast.Show();
        }

        if (request.Id is { } requestId)
            _visible[requestId] = windows;
    }

    private IEnumerable<Screen> ResolveScreens(MainWindow owner)
    {
        var screens = owner.Screens;
        if (screens is null)
            return [];

        if (_settings.Store.NotificationOnAllScreens)
            return screens.All;

        var screen = screens.ScreenFromWindow(owner) ?? screens.Primary;
        return screen is null ? [] : [screen];
    }

    private static void PositionToast(AvaloniaToastWindow toast, Screen screen, NotificationPosition position)
    {
        var workArea = screen.WorkingArea;
        var width = (int)Math.Ceiling(ToastWidth * screen.Scaling);
        var height = (int)Math.Ceiling(ToastHeight * screen.Scaling);
        var margin = (int)Math.Ceiling(ToastMargin * screen.Scaling);
        var x = workArea.X + margin;
        var y = workArea.Y + margin;

        switch (position)
        {
            case NotificationPosition.BottomRight:
                x = workArea.Right - width - margin;
                y = workArea.Bottom - height - margin;
                break;
            case NotificationPosition.BottomCenter:
                x = workArea.X + (workArea.Width - width) / 2;
                y = workArea.Bottom - height - margin;
                break;
            case NotificationPosition.BottomLeft:
                y = workArea.Bottom - height - margin;
                break;
            case NotificationPosition.CenterLeft:
                y = workArea.Y + (workArea.Height - height) / 2;
                break;
            case NotificationPosition.TopCenter:
                x = workArea.X + (workArea.Width - width) / 2;
                break;
            case NotificationPosition.TopRight:
                x = workArea.Right - width - margin;
                break;
            case NotificationPosition.CenterRight:
                x = workArea.Right - width - margin;
                y = workArea.Y + (workArea.Height - height) / 2;
                break;
            case NotificationPosition.Center:
                x = workArea.X + (workArea.Width - width) / 2;
                y = workArea.Y + (workArea.Height - height) / 2;
                break;
            case NotificationPosition.TopLeft:
            default:
                break;
        }

        toast.Position = new PixelPoint(x, y);
    }

    private void RemoveTrackedRequest(ToastRequest request)
    {
        if (request.Id is { } id)
            _tracked.Remove(id);
    }

    private static int PriorityValue(AppNotificationSeverity severity) => severity switch
    {
        AppNotificationSeverity.Error => 0,
        AppNotificationSeverity.Warning => 1,
        _ => 2,
    };

    // Mirrors the WPF min-heap ordering: higher severity notifications are
    // dequeued before lower ones while equal severities keep arrival order.
    private void EnqueueByPriority(ToastRequest request)
    {
        var priority = PriorityValue(request.Severity);
        var index = _pending.Count;
        while (index > 0 && PriorityValue(_pending[index - 1].Severity) > priority)
            index--;
        _pending.Insert(index, request);
    }

    internal static bool ShouldSuppressForFullscreen(bool notificationAlwaysOnTop, bool isAnyApplicationFullscreen) =>
        !notificationAlwaysOnTop && isAnyApplicationFullscreen;

    private static bool IsAnyApplicationFullscreen(MainWindow owner)
    {
        try
        {
            var desktopWindow = GetDesktopWindow();
            var shellWindow = GetShellWindow();
            var foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero
                || foregroundWindow == desktopWindow
                || foregroundWindow == shellWindow)
                return false;

            if (!GetWindowRect(foregroundWindow, out var bounds))
                return false;

            var screens = owner.Screens?.All;
            if (screens is null || screens.Count == 0)
                return false;

            // Exclusive fullscreen covers the full monitor bounds (not the working
            // area); work-area maximization must not count as fullscreen.
            if (!screens.Any(screen =>
                    bounds.Left == screen.Bounds.X
                    && bounds.Top == screen.Bounds.Y
                    && bounds.Right == screen.Bounds.Right
                    && bounds.Bottom == screen.Bounds.Bottom))
                return false;

            if (GetWindowThreadProcessId(foregroundWindow, out var processId) != 0 && processId != 0)
            {
                try
                {
                    using var process = System.Diagnostics.Process.GetProcessById((int)processId);
                    return !string.Equals(process.ProcessName, "explorer", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return true;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool ShouldSuppress(AppNotificationSeverity severity) =>
        _settings.Store.DontShowNotifications
        || (severity == AppNotificationSeverity.Success && !_settings.Store.Notifications.SuccessNotifications);

    private bool IsNotificationTypeEnabled(NotificationType type)
    {
        var (key, legacyEnabled) = ResolveNotificationCategory(type);
        var policy = NotificationTypePolicyStore.GetOrDefault(
            _settings.Store.Notifications.TypePolicies,
            key,
            legacyEnabled);
        return policy.Enabled && legacyEnabled;
    }

    private (string Key, bool LegacyEnabled) ResolveNotificationCategory(NotificationType type)
    {
        var notifications = _settings.Store.Notifications;
        return type switch
        {
            NotificationType.ACAdapterConnected or NotificationType.ACAdapterConnectedLowWattage or NotificationType.ACAdapterDisconnected
                => ("ACAdapter", notifications.ACAdapter),
            NotificationType.AutomationNotification
                => ("AutomationNotification", notifications.AutomationNotification),
            NotificationType.CapsLockOn or NotificationType.CapsLockOff or NotificationType.NumLockOn or NotificationType.NumLockOff
                => ("CapsNumLock", notifications.CapsNumLock),
            NotificationType.CameraOn or NotificationType.CameraOff
                => ("CameraLock", notifications.CameraLock),
            NotificationType.FnLockOn or NotificationType.FnLockOff
                => ("FnLock", notifications.FnLock),
            NotificationType.MicrophoneOn or NotificationType.MicrophoneOff
                => ("Microphone", notifications.Microphone),
            NotificationType.PanelLogoLightingOn or NotificationType.PanelLogoLightingOff
                or NotificationType.PortLightingOn or NotificationType.PortLightingOff
                or NotificationType.RGBKeyboardBacklightOff or NotificationType.RGBKeyboardBacklightChanged
                or NotificationType.SpectrumBacklightChanged or NotificationType.SpectrumBacklightOff
                or NotificationType.SpectrumBacklightPresetChanged
                or NotificationType.WhiteKeyboardBacklightOff or NotificationType.WhiteKeyboardBacklightChanged
                => ("KeyboardBacklight", notifications.KeyboardBacklight),
            NotificationType.PowerModeQuiet or NotificationType.PowerModeBalance or NotificationType.PowerModePerformance
                or NotificationType.PowerModeExtreme or NotificationType.PowerModeGodMode
                or NotificationType.ITSModeAuto or NotificationType.ITSModeCool
                or NotificationType.ITSModePerformance or NotificationType.ITSModeGeek
                => ("PowerMode", notifications.PowerMode),
            NotificationType.RefreshRate => ("RefreshRate", notifications.RefreshRate),
            NotificationType.SmartKeyDoublePress or NotificationType.SmartKeySinglePress
                => ("SmartKey", notifications.SmartKey),
            NotificationType.TouchpadOn or NotificationType.TouchpadOff
                => ("TouchpadLock", notifications.TouchpadLock),
            NotificationType.UpdateAvailable => ("UpdateAvailable", notifications.UpdateAvailable),
            _ => ("Unknown", false),
        };
    }

    private ToastRequest? CreateLegacyRequest(NotificationMessage notification)
    {
        var message = notification.Type switch
        {
            NotificationType.ACAdapterConnected => Get("Notification_ACAdapterConnected", "AC adapter connected"),
            NotificationType.ACAdapterConnectedLowWattage => Get("Notification_ACAdapterConnectedLowWattage", "Low-wattage AC adapter connected"),
            NotificationType.ACAdapterDisconnected => Get("Notification_ACAdapterDisconnected", "AC adapter disconnected"),
            NotificationType.AutomationNotification => FormatArguments(notification.Args),
            NotificationType.CapsLockOn => Get("Notification_CapsLockOn", "Caps Lock on"),
            NotificationType.CapsLockOff => Get("Notification_CapsLockOff", "Caps Lock off"),
            NotificationType.CameraOn => Get("Notification_CameraOn", "Camera enabled"),
            NotificationType.CameraOff => Get("Notification_CameraOff", "Camera disabled"),
            NotificationType.FnLockOn => Get("Notification_FnLockOn", "Fn Lock on"),
            NotificationType.FnLockOff => Get("Notification_FnLockOff", "Fn Lock off"),
            NotificationType.MicrophoneOn => Get("Notification_MicrophoneOn", "Microphone enabled"),
            NotificationType.MicrophoneOff => Get("Notification_MicrophoneOff", "Microphone disabled"),
            NotificationType.NumLockOn => Get("Notification_NumLockOn", "Num Lock on"),
            NotificationType.NumLockOff => Get("Notification_NumLockOff", "Num Lock off"),
            NotificationType.PanelLogoLightingOn => Get("Notification_PanelLogoLightingOn", "Panel logo lighting enabled"),
            NotificationType.PanelLogoLightingOff => Get("Notification_PanelLogoLightingOff", "Panel logo lighting disabled"),
            NotificationType.PortLightingOn => Get("Notification_PortLightingOn", "Port lighting enabled"),
            NotificationType.PortLightingOff => Get("Notification_PortLightingOff", "Port lighting disabled"),
            NotificationType.PowerModeQuiet or NotificationType.PowerModeBalance or NotificationType.PowerModePerformance
                or NotificationType.PowerModeExtreme or NotificationType.PowerModeGodMode
                or NotificationType.ITSModeAuto or NotificationType.ITSModeCool
                or NotificationType.ITSModePerformance or NotificationType.ITSModeGeek
                or NotificationType.RefreshRate or NotificationType.RGBKeyboardBacklightOff
                or NotificationType.RGBKeyboardBacklightChanged or NotificationType.SmartKeyDoublePress
                or NotificationType.SmartKeySinglePress or NotificationType.TouchpadOn or NotificationType.TouchpadOff
                or NotificationType.WhiteKeyboardBacklightOff or NotificationType.WhiteKeyboardBacklightChanged
                => FormatArguments(notification.Args),
            NotificationType.SpectrumBacklightChanged => string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                Get("Notification_SpectrumKeyboardBacklight_Brightness", "Keyboard backlight brightness: {0}"),
                notification.Args),
            NotificationType.SpectrumBacklightOff => string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                Get("Notification_SpectrumKeyboardBacklight_Backlight", "Keyboard backlight: {0}"),
                notification.Args),
            NotificationType.SpectrumBacklightPresetChanged => string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                Get("Notification_SpectrumKeyboardBacklight_Profile", "Keyboard profile: {0}"),
                notification.Args),
            NotificationType.UpdateAvailable => string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                Get("Notification_UpdateAvailable", "Update available: {0}"),
                notification.Args),
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(message))
            return null;

        var severity = notification.Type == NotificationType.ACAdapterConnectedLowWattage
            ? AppNotificationSeverity.Warning
            : AppNotificationSeverity.Info;
        return new ToastRequest(
            null,
            Get("Window_Title", "Universal Device Toolkit"),
            message,
            severity,
            ResolveLegacyIcon(notification.Type),
            ResolveLegacyDuration(),
            false,
            1,
            null,
            notification.Type == NotificationType.UpdateAvailable ? ShowUpdatePage : null);
    }

    private TimeSpan ResolveLegacyDuration() => _settings.Store.NotificationDuration switch
    {
        NotificationDuration.Short => TimeSpan.FromMilliseconds(500),
        NotificationDuration.Long => TimeSpan.FromMilliseconds(2500),
        _ => TimeSpan.FromMilliseconds(1000),
    };

    private TimeSpan? ResolveAppDuration(AppNotificationRequest request)
    {
        if (request.IsPersistent)
            return null;
        if (request.Duration is { } explicitDuration && explicitDuration > TimeSpan.Zero)
            return explicitDuration;

        return _settings.Store.NotificationDuration switch
        {
            NotificationDuration.Short => TimeSpan.FromSeconds(3),
            NotificationDuration.Long => TimeSpan.FromSeconds(request.Severity == AppNotificationSeverity.Success ? 8 : 10),
            _ => TimeSpan.FromSeconds(5),
        };
    }

    private void ShowUpdatePage()
    {
        var owner = _mainWindowProvider();
        if (owner is null)
            return;

        owner.RestoreFromTray();
        if (AvaloniaUpdateCheckCoordinator.Current is { } coordinator)
        {
            _ = coordinator.ShowUpdateAsync(owner);
            return;
        }

        owner.Navigate(MainNavigation.About);
    }

    private static string ResolveLegacyIcon(NotificationType type) => type switch
    {
        NotificationType.ACAdapterConnected or NotificationType.ACAdapterConnectedLowWattage or NotificationType.ACAdapterDisconnected => "BatteryCharge24",
        NotificationType.AutomationNotification => "Rocket24",
        NotificationType.CapsLockOn or NotificationType.CapsLockOff => "KeyboardShiftUppercase24",
        NotificationType.NumLockOn or NotificationType.NumLockOff => "Keyboard12324",
        NotificationType.CameraOn or NotificationType.CameraOff => "Camera24",
        NotificationType.FnLockOn or NotificationType.FnLockOff => "Keyboard24",
        NotificationType.MicrophoneOn or NotificationType.MicrophoneOff => "Mic24",
        NotificationType.RefreshRate => "DesktopPulse24",
        NotificationType.SmartKeyDoublePress => "StarEmphasis24",
        NotificationType.SmartKeySinglePress => "Star24",
        NotificationType.TouchpadOn or NotificationType.TouchpadOff => "Tablet24",
        NotificationType.UpdateAvailable => "ArrowSync24",
        NotificationType.PowerModeQuiet or NotificationType.PowerModeBalance or NotificationType.PowerModePerformance
            or NotificationType.PowerModeExtreme or NotificationType.PowerModeGodMode
            or NotificationType.ITSModeAuto or NotificationType.ITSModeCool
            or NotificationType.ITSModePerformance or NotificationType.ITSModeGeek => "Gauge24",
        _ => "Lightbulb24",
    };

    private static string ResolveAppIcon(AppNotificationSeverity severity) => severity switch
    {
        AppNotificationSeverity.Success => "CheckmarkCircle24",
        AppNotificationSeverity.Warning => "Warning24",
        AppNotificationSeverity.Error => "ErrorCircle24",
        _ => "Info24",
    };

    private static string FormatArguments(IReadOnlyList<object> args) => args.Count switch
    {
        0 => string.Empty,
        1 => Convert.ToString(args[0], System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty,
        _ => string.Join(", ", args.Select(value => Convert.ToString(value, System.Globalization.CultureInfo.CurrentCulture))),
    };

    private static string Get(string key, string fallback) => AvaloniaLocalization.GetString(key, fallback);

    private void TryPlaySound()
    {
        if (!_settings.Store.Notifications.NotificationSound)
            return;

        try
        {
            System.Media.SystemSounds.Asterisk.Play();
        }
        catch
        {
            // Sound playback is best effort and must never hide the notification itself.
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        MessagingCenter.Unsubscribe<NotificationMessage>(this);
        _appNotifications.Changed -= OnAppNotificationChanged;
        foreach (var windows in _visible.Values)
        {
            foreach (var window in windows.ToArray())
                window.Close();
        }

        _visible.Clear();
        _pending.Clear();
        _tracked.Clear();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private sealed class ToastRequest(
        Guid? id,
        string title,
        string? message,
        AppNotificationSeverity severity,
        string iconIdentifier,
        TimeSpan? duration,
        bool isPersistent,
        int mergeCount,
        double? progressPercent,
        Action? clickAction)
    {
        public Guid? Id { get; } = id;
        public string Title { get; private set; } = title;
        public string? Message { get; private set; } = message;
        public AppNotificationSeverity Severity { get; private set; } = severity;
        public string IconIdentifier { get; private set; } = iconIdentifier;
        public TimeSpan? Duration { get; private set; } = duration;
        public bool IsPersistent { get; private set; } = isPersistent;
        public int MergeCount { get; private set; } = mergeCount;
        public double? ProgressPercent { get; private set; } = progressPercent;
        public Action? ClickAction { get; } = clickAction;

        public void Apply(AppNotificationRequest notification, int mergeCount, TimeSpan? duration)
        {
            if (!string.IsNullOrWhiteSpace(notification.Title))
                Title = notification.Title;
            if (notification.Message is not null)
                Message = notification.Message;
            if (notification.ProgressPercent is not null)
                ProgressPercent = notification.ProgressPercent;
            Severity = notification.Severity;
            IconIdentifier = ResolveAppIcon(notification.Severity);
            Duration = duration;
            IsPersistent = notification.IsPersistent;
            MergeCount = Math.Max(1, mergeCount);
        }
    }

    private sealed class AvaloniaToastWindow : Window
    {
        private readonly ToastRequest _request;
        private readonly LocalizedTextBlock _title = new();
        private readonly LocalizedTextBlock _message = new();
        private readonly ProgressBar _progress = new();
        private readonly DispatcherTimer _timer = new();

        public AvaloniaToastWindow(ToastRequest request, bool topmost)
        {
            _request = request;
            Width = ToastWidth;
            Height = ToastHeight;
            MinWidth = ToastWidth;
            MaxWidth = ToastWidth;
            CanResize = false;
            ShowInTaskbar = false;
            SystemDecorations = SystemDecorations.None;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Topmost = topmost;
            Background = Brushes.Transparent;
            AutomationProperties.SetAutomationId(this, "AvaloniaNotificationToast");
            AutomationProperties.SetName(this, request.Title);

            _timer.Tick += (_, _) => Close();
            Closed += (_, _) => _timer.Stop();
            Content = BuildContent();
            Update(request);
        }

        private Control BuildContent()
        {
            var icon = new NavigationIcon { FontSize = 22, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            icon.Bind(NavigationIcon.IconIdentifierProperty, new global::Avalonia.Data.Binding(nameof(ToastRequest.IconIdentifier)) { Source = _request });
            var iconHost = new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(34, 0, 120, 212)),
                Child = icon,
                VerticalAlignment = VerticalAlignment.Top,
            };

            _title.FontWeight = FontWeight.Medium;
            _title.OverflowMode = UniversalDeviceToolkit.Abstractions.Localization.LocalizedOverflowMode.Wrap;
            _title.MaxLines = 2;
            _message.OverflowMode = UniversalDeviceToolkit.Abstractions.Localization.LocalizedOverflowMode.Wrap;
            _message.MaxLines = 3;
            _message.Foreground = new SolidColorBrush(Colors.Gray);
            _progress.Minimum = 0;
            _progress.Maximum = 100;
            _progress.Height = 5;
            _progress.IsVisible = false;

            var copy = new StackPanel { Spacing = 3, MinWidth = 0 };
            copy.Children.Add(_title);
            copy.Children.Add(_message);
            copy.Children.Add(_progress);

            var close = new Button
            {
                Content = new NavigationIcon { IconIdentifier = "Dismiss24", FontSize = 16 },
                MinWidth = 30,
                MinHeight = 30,
                Padding = new Thickness(4),
                VerticalAlignment = VerticalAlignment.Top,
            };
            close.PointerPressed += (_, args) => args.Handled = true;
            close.Click += (_, _) => Close();
            ToolTip.SetTip(close, Get("AppNotificationHost_Close", "Close"));
            AutomationProperties.SetName(close, Get("AppNotificationHost_Close", "Close"));

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 10 };
            grid.Children.Add(iconHost);
            Grid.SetColumn(copy, 1);
            grid.Children.Add(copy);
            Grid.SetColumn(close, 2);
            grid.Children.Add(close);

            var border = new Border
            {
                Padding = new Thickness(14, 12),
                BorderThickness = new Thickness(1),
                Child = grid,
            };
            border.Classes.Add("notificationToast");
            border.PointerPressed += (_, _) => _request.ClickAction?.Invoke();
            return border;
        }

        public void Update(ToastRequest request)
        {
            _title.Text = request.MergeCount > 1 ? $"{request.Title} ({request.MergeCount})" : request.Title;
            _message.Text = request.Message ?? string.Empty;
            _message.IsVisible = !string.IsNullOrWhiteSpace(request.Message);
            _progress.Value = request.ProgressPercent ?? 0;
            _progress.IsVisible = request.ProgressPercent is not null;
            AutomationProperties.SetName(this, _message.IsVisible
                ? $"{_title.Text}. {_message.Text}"
                : _title.Text ?? string.Empty);

            _timer.Stop();
            if (!request.IsPersistent && request.Duration is { } duration && duration > TimeSpan.Zero)
            {
                _timer.Interval = duration;
                _timer.Start();
            }
        }
    }
}

#endif

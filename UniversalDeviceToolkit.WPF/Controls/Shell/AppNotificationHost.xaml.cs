using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using UniversalDeviceToolkit.Lib.Notifications;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Shell;

public partial class AppNotificationHost : UserControl
{
    public const int MaxPinnedVisible = 3;
    public static readonly TimeSpan SuccessAutoClose = TimeSpan.FromSeconds(5);

    private readonly ObservableCollection<NotificationItemViewModel> _notifications = [];
    private IAppNotificationService? _service;
    private bool _attached;

    public AppNotificationHost()
    {
        InitializeComponent();
        _items.ItemsSource = _notifications;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TryAttachService();
        // If MainWindow loaded before IoC finished, retry once on next idle tick.
        if (!_attached)
        {
            _ = Dispatcher.BeginInvoke(new Action(TryAttachService), DispatcherPriority.ApplicationIdle);
        }
    }

    private void TryAttachService()
    {
        if (_attached)
            return;

        try
        {
            _service = IoCContainer.TryResolve<IAppNotificationService>()
                       ?? IoCContainer.Resolve<IAppNotificationService>();
            _service.Changed += Service_Changed;
            _attached = true;
        }
        catch (Exception ex)
        {
            // Design-time / early boot — leave unattached.
            Log.Instance.TraceOnce(
                "notification-host-attach",
                "AppNotificationHost could not attach to notification service (early boot/design-time).",
                ex);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_service is not null && _attached)
        {
            _service.Changed -= Service_Changed;
            _attached = false;
        }

        foreach (var item in _notifications.ToList())
            item.Dispose();
        _notifications.Clear();
    }

    private void Service_Changed(object? sender, AppNotificationChangedEventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.BeginInvoke(new Action(() => Service_Changed(sender, e)), DispatcherPriority.Normal);
            return;
        }

        if (e.IsDismiss)
        {
            RemoveById(e.Notification.Id);
            return;
        }

        if (ShouldSuppress(e.Notification.Severity))
            return;

        var existing = _notifications.FirstOrDefault(i => i.Id == e.Notification.Id);
        if (existing is not null)
        {
            existing.ApplyMerge(e.MergeCount, e.Notification);
            existing.ResetAutoCloseTimer(ResolveDuration(e.Notification));
            return;
        }

        var duration = ResolveDuration(e.Notification);
        var vm = new NotificationItemViewModel(e.Notification, e.MergeCount, duration, OnItemExpired);
        _notifications.Add(vm);
        TryPlaySound();
        ScrollToBottom();
        TrimIfNeeded();
    }

    private static bool ShouldSuppress(AppNotificationSeverity severity)
    {
        try
        {
            var settings = IoCContainer.Resolve<ApplicationSettings>();
            if (settings.Store.DontShowNotifications)
                return true;
            if (severity == AppNotificationSeverity.Success && !settings.Store.Notifications.SuccessNotifications)
                return true;
            return false;
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "notification-suppress-settings",
                "Failed to read notification suppression settings; showing notification.",
                ex);
            return false;
        }
    }

    private static TimeSpan ResolveDuration(AppNotificationRequest request)
    {
        if (request.IsPersistent)
            return Timeout.InfiniteTimeSpan;

        if (request.Duration is { } explicitDuration && explicitDuration > TimeSpan.Zero)
            return explicitDuration;

        try
        {
            var settings = IoCContainer.Resolve<ApplicationSettings>();
            var baseDuration = settings.Store.NotificationDuration switch
            {
                NotificationDuration.Short => TimeSpan.FromSeconds(3),
                NotificationDuration.Long => TimeSpan.FromSeconds(10),
                _ => TimeSpan.FromSeconds(5)
            };

            // Success defaults to 5s per product contract; duration setting scales around it.
            if (request.Severity == AppNotificationSeverity.Success)
                return settings.Store.NotificationDuration switch
                {
                    NotificationDuration.Short => TimeSpan.FromSeconds(3),
                    NotificationDuration.Long => TimeSpan.FromSeconds(8),
                    _ => SuccessAutoClose
                };

            return baseDuration;
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "notification-duration-settings",
                "Failed to read notification duration settings; using defaults.",
                ex);
            return request.Severity == AppNotificationSeverity.Success
                ? SuccessAutoClose
                : TimeSpan.FromSeconds(5);
        }
    }

    private static void TryPlaySound()
    {
        try
        {
            var settings = IoCContainer.Resolve<ApplicationSettings>();
            if (!settings.Store.Notifications.NotificationSound)
                return;
            System.Media.SystemSounds.Asterisk.Play();
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "notification-sound",
                "Notification sound playback failed (best-effort).",
                ex);
        }
    }

    private void OnItemExpired(Guid id) => RemoveById(id);

    private void RemoveById(Guid id)
    {
        var match = _notifications.FirstOrDefault(i => i.Id == id);
        if (match is null)
            return;
        match.Dispose();
        _notifications.Remove(match);
    }

    private void TrimIfNeeded()
    {
        // Keep all items in a scrollable stack; hard-cap memory at 30.
        while (_notifications.Count > 30)
        {
            var oldest = _notifications[0];
            oldest.Dispose();
            _notifications.RemoveAt(0);
        }
    }

    private void ScrollToBottom()
    {
        _scrollViewer.UpdateLayout();
        _scrollViewer.ScrollToEnd();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Guid id })
            return;

        // Close only — never invoke a primary action.
        _service?.Dismiss(id);
        RemoveById(id);
        e.Handled = true;
    }

    private void Toast_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: NotificationItemViewModel vm })
            vm.PauseTimer();
    }

    private void Toast_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: NotificationItemViewModel vm })
            vm.ResumeTimer();
    }

    private sealed class NotificationItemViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly Action<Guid> _onExpired;
        private DispatcherTimer? _timer;
        private TimeSpan _remaining;
        private DateTime _deadlineUtc;
        private bool _paused;
        private int _mergeCount;
        private string _title;
        private string? _message;
        private double _progressPercent;
        private bool _hasProgress;

        public NotificationItemViewModel(
            AppNotificationRequest request,
            int mergeCount,
            TimeSpan duration,
            Action<Guid> onExpired)
        {
            Id = request.Id;
            Severity = request.Severity;
            IsPersistent = request.IsPersistent || duration == Timeout.InfiniteTimeSpan;
            _title = request.Title;
            _message = request.Message;
            _mergeCount = Math.Max(1, mergeCount);
            _progressPercent = request.ProgressPercent ?? 0;
            _hasProgress = request.ProgressPercent.HasValue;
            _onExpired = onExpired;
            _remaining = duration <= TimeSpan.Zero || duration == Timeout.InfiniteTimeSpan
                ? TimeSpan.Zero
                : duration;

            if (!IsPersistent && _remaining > TimeSpan.Zero)
                StartTimer(_remaining);
        }

        public Guid Id { get; }
        public AppNotificationSeverity Severity { get; }
        public bool IsPersistent { get; }

        public string DisplayTitle =>
            _mergeCount > 1 ? $"{_title} ×{_mergeCount}" : _title;

        public string? Message => _message;

        public double ProgressPercent => _progressPercent;

        public Visibility ProgressVisibility =>
            _hasProgress ? Visibility.Visible : Visibility.Collapsed;

        public Visibility MessageVisibility =>
            string.IsNullOrWhiteSpace(_message) ? Visibility.Collapsed : Visibility.Visible;

        public string AutomationId => $"AppNotification_{Id:N}";
        public string CloseAutomationId => $"AppNotificationClose_{Id:N}";

        public string AccessibilityName =>
            string.IsNullOrWhiteSpace(_message) ? DisplayTitle : $"{DisplayTitle}. {_message}";

        public SymbolRegular IconSymbol => Severity switch
        {
            AppNotificationSeverity.Success => SymbolRegular.Checkmark24,
            AppNotificationSeverity.Warning => SymbolRegular.Warning24,
            AppNotificationSeverity.Error => SymbolRegular.ErrorCircle24,
            _ => SymbolRegular.Info24
        };

        public Brush IconBrush
        {
            get
            {
                var key = Severity switch
                {
                    AppNotificationSeverity.Success => "StatusSuccessBrush",
                    AppNotificationSeverity.Warning => "StatusWarningBrush",
                    AppNotificationSeverity.Error => "StatusCriticalBrush",
                    _ => "StatusInfoBrush"
                };
                return Application.Current?.TryFindResource(key) as Brush
                       ?? new SolidColorBrush(Severity switch
                       {
                           AppNotificationSeverity.Success => Color.FromRgb(0x2E, 0xB8, 0x71),
                           AppNotificationSeverity.Warning => Color.FromRgb(0xE6, 0xA2, 0x3C),
                           AppNotificationSeverity.Error => Color.FromRgb(0xE8, 0x4A, 0x5F),
                           _ => Color.FromRgb(0x3E, 0x8A, 0xE0)
                       });
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void ApplyMerge(int mergeCount, AppNotificationRequest request)
        {
            _mergeCount = Math.Max(_mergeCount, mergeCount);
            if (!string.IsNullOrWhiteSpace(request.Title))
                _title = request.Title;
            if (!string.IsNullOrWhiteSpace(request.Message))
                _message = request.Message;
            if (request.ProgressPercent.HasValue)
            {
                _progressPercent = request.ProgressPercent.Value;
                _hasProgress = true;
                OnPropertyChanged(nameof(ProgressPercent));
                OnPropertyChanged(nameof(ProgressVisibility));
            }
            OnPropertyChanged(nameof(DisplayTitle));
            OnPropertyChanged(nameof(Message));
            OnPropertyChanged(nameof(MessageVisibility));
            OnPropertyChanged(nameof(AccessibilityName));
        }

        public void PauseTimer()
        {
            if (_paused || IsPersistent || _timer is null)
                return;
            _paused = true;
            _remaining = _deadlineUtc - DateTime.UtcNow;
            if (_remaining < TimeSpan.Zero)
                _remaining = TimeSpan.Zero;
            _timer.Stop();
        }

        public void ResumeTimer()
        {
            if (!_paused || IsPersistent)
                return;
            _paused = false;
            if (_remaining <= TimeSpan.Zero)
            {
                _onExpired(Id);
                return;
            }

            StartTimer(_remaining);
        }

        public void ResetAutoCloseTimer(TimeSpan duration)
        {
            if (IsPersistent)
                return;
            _paused = false;
            StartTimer(duration <= TimeSpan.Zero ? SuccessAutoClose : duration);
        }

        private void StartTimer(TimeSpan duration)
        {
            if (_timer is not null)
            {
                _timer.Stop();
                _timer.Tick -= OnTimerTick;
                _timer = null;
            }

            _remaining = duration;
            _deadlineUtc = DateTime.UtcNow + duration;
            _timer = new DispatcherTimer { Interval = duration };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            _timer?.Stop();
            if (!_paused)
                _onExpired(Id);
        }

        public void Dispose()
        {
            if (_timer is not null)
            {
                _timer.Stop();
                _timer.Tick -= OnTimerTick;
                _timer = null;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

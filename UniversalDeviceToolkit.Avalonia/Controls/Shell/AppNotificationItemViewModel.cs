using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Lib.Notifications;

namespace UniversalDeviceToolkit.Avalonia.Controls.Shell;

// TODO(Phase 4c): full portable extraction into UniversalDeviceToolkit.ViewModels
// is blocked by (a) notification model types living in UniversalDeviceToolkit.Lib
// (AppNotificationSeverity/AppNotificationRequest/AppNotificationChangedEventArgs),
// (b) WPF-only presentation surface (DispatcherTimer, Visibility, Brush/SymbolRegular,
// Application resource lookups). Candidates for the portable split: merge-counting,
// auto-close deadline math, progress state. Revisit after models move to
// UniversalDeviceToolkit.Lib.Abstractions (same-namespace relocation, see Plugins.Abstractions
// dual-ABI precedent).
public sealed class NotificationItemViewModel : INotifyPropertyChanged, IDisposable
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

    public bool ProgressVisibility =>
        _hasProgress ? true : false;

    public bool MessageVisibility =>
        string.IsNullOrWhiteSpace(_message) ? false : true;

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
            return (Application.Current?.TryFindResource(key, out var value) == true ? value as Brush : null)
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
        StartTimer(duration <= TimeSpan.Zero ? AppNotificationHost.SuccessAutoClose : duration);
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

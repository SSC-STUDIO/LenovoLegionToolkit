using System;
using System.Collections.Generic;
using System.Threading;

namespace LenovoLegionToolkit.Lib.Notifications;

/// <summary>
/// Thread-safe notification bus with short-window merge for repeated successes.
/// </summary>
public sealed class AppNotificationService : IAppNotificationService
{
    public static readonly TimeSpan DefaultSuccessDuration = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan DefaultInfoDuration = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan DefaultWarningDuration = TimeSpan.FromSeconds(8);
    public static readonly TimeSpan DefaultErrorDuration = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MergeWindow = TimeSpan.FromSeconds(4);

    private readonly object _gate = new();
    private readonly Dictionary<string, (Guid Id, DateTimeOffset At, int Count)> _recentMerges = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<AppNotificationChangedEventArgs>? Changed;

    public Guid Show(AppNotificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.", nameof(request));

        var duration = request.Duration ?? ResolveDefaultDuration(request.Severity, request.IsPersistent);
        var mergeCount = 1;
        Guid id;

        lock (_gate)
        {
            PruneMergesLocked();

            if (!string.IsNullOrWhiteSpace(request.MergeKey)
                && _recentMerges.TryGetValue(request.MergeKey, out var existing)
                && DateTimeOffset.UtcNow - existing.At <= MergeWindow)
            {
                // ValueTuple is a copy — write a new tuple back into the dictionary.
                mergeCount = existing.Count + 1;
                id = existing.Id;
                _recentMerges[request.MergeKey] = (id, DateTimeOffset.UtcNow, mergeCount);
            }
            else
            {
                id = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id;
                if (!string.IsNullOrWhiteSpace(request.MergeKey))
                    _recentMerges[request.MergeKey!] = (id, DateTimeOffset.UtcNow, 1);
            }
        }

        var published = new AppNotificationRequest
        {
            Id = id,
            Title = request.Title.Trim(),
            Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
            Severity = request.Severity,
            Duration = duration,
            IsPersistent = request.IsPersistent,
            MergeKey = request.MergeKey,
            CreatedAt = DateTimeOffset.UtcNow
        };

        Volatile.Read(ref Changed)?.Invoke(this, new AppNotificationChangedEventArgs
        {
            Notification = published,
            IsDismiss = false,
            MergeCount = mergeCount
        });

        return id;
    }

    public void Dismiss(Guid id)
    {
        if (id == Guid.Empty)
            return;

        Volatile.Read(ref Changed)?.Invoke(this, new AppNotificationChangedEventArgs
        {
            Notification = new AppNotificationRequest { Id = id, Title = string.Empty },
            IsDismiss = true
        });
    }

    public Guid ShowSuccess(string title, string? message = null, string? mergeKey = null) =>
        Show(new AppNotificationRequest
        {
            Title = title,
            Message = message,
            Severity = AppNotificationSeverity.Success,
            Duration = DefaultSuccessDuration,
            MergeKey = mergeKey
        });

    public Guid ShowInfo(string title, string? message = null, string? mergeKey = null) =>
        Show(new AppNotificationRequest
        {
            Title = title,
            Message = message,
            Severity = AppNotificationSeverity.Info,
            Duration = DefaultInfoDuration,
            MergeKey = mergeKey
        });

    public Guid ShowWarning(string title, string? message = null, string? mergeKey = null) =>
        Show(new AppNotificationRequest
        {
            Title = title,
            Message = message,
            Severity = AppNotificationSeverity.Warning,
            Duration = DefaultWarningDuration,
            MergeKey = mergeKey
        });

    public Guid ShowError(string title, string? message = null, string? mergeKey = null) =>
        Show(new AppNotificationRequest
        {
            Title = title,
            Message = message,
            Severity = AppNotificationSeverity.Error,
            Duration = DefaultErrorDuration,
            MergeKey = mergeKey
        });

    private static TimeSpan? ResolveDefaultDuration(AppNotificationSeverity severity, bool persistent)
    {
        if (persistent)
            return null;

        return severity switch
        {
            AppNotificationSeverity.Success => DefaultSuccessDuration,
            AppNotificationSeverity.Info => DefaultInfoDuration,
            AppNotificationSeverity.Warning => DefaultWarningDuration,
            AppNotificationSeverity.Error => DefaultErrorDuration,
            _ => DefaultInfoDuration
        };
    }

    private void PruneMergesLocked()
    {
        var cutoff = DateTimeOffset.UtcNow - MergeWindow;
        List<string>? stale = null;
        foreach (var pair in _recentMerges)
        {
            if (pair.Value.At < cutoff)
                (stale ??= []).Add(pair.Key);
        }

        if (stale is null)
            return;

        foreach (var key in stale)
            _recentMerges.Remove(key);
    }
}

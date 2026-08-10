using System;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Notifications;

namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Convenience helpers for persistent progress toasts: create once, update in place,
/// dismiss on completion. Safe no-ops when the notification service is unavailable.
/// </summary>
public static class ProgressToastHelper
{
    public static Guid Start(string title, string? message = null)
    {
        var service = IoCContainer.TryResolve<IAppNotificationService>();
        if (service is null)
            return Guid.Empty;

        return service.Show(new AppNotificationRequest
        {
            Title = title,
            Message = message,
            Severity = AppNotificationSeverity.Info,
            IsPersistent = true,
            ProgressPercent = 0
        });
    }

    public static void Update(Guid id, double percent, string? message = null)
    {
        if (id == Guid.Empty)
            return;

        IoCContainer.TryResolve<IAppNotificationService>()?.UpdateProgress(id, percent, message);
    }

    public static void Complete(Guid id)
    {
        if (id == Guid.Empty)
            return;

        IoCContainer.TryResolve<IAppNotificationService>()?.Dismiss(id);
    }
}

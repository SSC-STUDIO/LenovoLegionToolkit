using System;

namespace LenovoLegionToolkit.Lib.Notifications;

/// <summary>
/// Application-level toast notifications. Business layers publish events only —
/// they must not touch MainWindow, Snackbar, or visual controls.
/// </summary>
public interface IAppNotificationService
{
    event EventHandler<AppNotificationChangedEventArgs>? Changed;

    Guid Show(AppNotificationRequest request);

    void Dismiss(Guid id);

    Guid ShowSuccess(string title, string? message = null, string? mergeKey = null);

    Guid ShowInfo(string title, string? message = null, string? mergeKey = null);

    Guid ShowWarning(string title, string? message = null, string? mergeKey = null);

    Guid ShowError(string title, string? message = null, string? mergeKey = null);
}

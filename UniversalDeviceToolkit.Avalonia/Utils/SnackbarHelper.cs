using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Windows;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Notifications;

namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Compatibility façade over <see cref="IAppNotificationService"/>.
/// Routes all success/info/warn/error toasts to the bottom-right multi-toast host.
/// Falls back to legacy single Snackbar only when the service is unavailable.
/// </summary>
public static class SnackbarHelper
{
    public static async Task ShowAsync(string title, string? message = null, SnackbarType type = SnackbarType.Success)
    {
        if (TryPublish(title, message, type))
            return;

        // Design-time / early boot fallback — single snackbar path.
        await ShowLegacySnackbarAsync(title, message, type);
    }

    public static void Show(string title, string? message = null, SnackbarType type = SnackbarType.Success)
    {
        if (TryPublish(title, message, type))
            return;

        _ = ShowLegacySnackbarAsync(title, message, type);
    }

    private static bool TryPublish(string title, string? message, SnackbarType type)
    {
        try
        {
            var service = IoCContainer.TryResolve<IAppNotificationService>();
            if (service is null)
                return false;

            // Only merge identical success copy within the service window (avoids toast storms
            // for repeated identical operations). Errors/warnings always stay distinct.
            var mergeKey = type == SnackbarType.Success
                ? $"success:{title}\u001f{message}"
                : null;

            switch (type)
            {
                case SnackbarType.Success:
                    service.ShowSuccess(title, message, mergeKey);
                    break;
                case SnackbarType.Warning:
                    service.ShowWarning(title, message);
                    break;
                case SnackbarType.Error:
                    service.ShowError(title, message);
                    break;
                default:
                    service.ShowInfo(title, message);
                    break;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task ShowLegacySnackbarAsync(string title, string? message, SnackbarType type)
    {
        if (Application.Current is null)
            return;

        var mainWindow = await Dispatcher.UIThread.InvokeAsync(() =>
            UdtAppContext.MainWindow as MainWindow).GetTask();

        var snackBar = mainWindow?.Snackbar;
        if (snackBar is null)
            return;

        var timeout = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            snackBar.Appearance = type switch
            {
                SnackbarType.Warning => ControlAppearance.Caution,
                SnackbarType.Error => ControlAppearance.Danger,
                SnackbarType.Info => ControlAppearance.Info,
                SnackbarType.Success => ControlAppearance.Success,
                _ => ControlAppearance.Secondary
            };
            snackBar.Icon = type switch
            {
                SnackbarType.Warning => new SymbolIcon { Symbol = SymbolRegular.Warning24 },
                SnackbarType.Error => new SymbolIcon { Symbol = SymbolRegular.ErrorCircle24 },
                SnackbarType.Info => new SymbolIcon { Symbol = SymbolRegular.Info24 },
                _ => new SymbolIcon { Symbol = SymbolRegular.Checkmark24 }
            };
            snackBar.Timeout = TimeSpan.FromMilliseconds(type switch
            {
                SnackbarType.Success => 5000,
                SnackbarType.Info => 5000,
                _ => 8000
            });
            snackBar.IsCloseButtonEnabled = type is SnackbarType.Error or SnackbarType.Warning;
            snackBar.Title = title;
            snackBar.Content = message;
            return snackBar.Timeout;
        }).GetTask();

        await Dispatcher.UIThread.InvokeAsync(() => snackBar.ShowAsync());
        await Task.Delay(timeout + TimeSpan.FromMilliseconds(300));
    }
}

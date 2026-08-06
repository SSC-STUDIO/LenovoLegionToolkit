#if WINDOWS

using Avalonia.Controls;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Supplies the shared plugin SDK with Avalonia host capabilities. The SDK
/// discovers <see cref="PluginHostContext.Current"/> through the shared Lib
/// assembly, so plugins do not need to reference this host directly.
/// </summary>
public sealed class AvaloniaPluginHostContext(Func<MainWindow?> mainWindowProvider) : IPluginHostContext
{
    public PluginHostMode Mode => PluginHostMode.RealRuntime;

    public bool AllowSystemActions => true;

    public object? OwnerWindow => mainWindowProvider();

    public bool OpenPluginSettings(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        return InvokeOnUiThread(() =>
        {
            var mainWindow = mainWindowProvider();
            if (mainWindow is null)
                return false;

            mainWindow.Navigate(MainNavigation.CreatePluginSettingsRoute(pluginId.Trim()));
            mainWindow.Show();
            mainWindow.Activate();
            return true;
        }, false, $"Failed to open Avalonia plugin settings for '{pluginId}'.");
    }

    public bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null)
    {
        if (dialogOrContent is null)
            return null;

        return InvokeOnUiThread(() => ShowDialogCore(dialogOrContent, title), null,
            $"Failed to show Avalonia plugin dialog '{dialogOrContent.GetType().FullName}'.");
    }

    private bool? ShowDialogCore(object dialogOrContent, string? title)
    {
        var owner = mainWindowProvider();

        if (dialogOrContent is Window dialogWindow)
        {
            if (owner is not null && !ReferenceEquals(owner, dialogWindow))
            {
                _ = dialogWindow.ShowDialog(owner);
            }
            else
            {
                dialogWindow.Show();
            }

            return true;
        }

        if (dialogOrContent is not Control content)
            return null;

        var hostWindow = new Window
        {
            Title = string.IsNullOrWhiteSpace(title) ? "Plugin Dialog" : title,
            Content = content,
            Width = 640,
            Height = 480,
            MinWidth = 480,
            MinHeight = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        if (owner is not null)
        {
            _ = hostWindow.ShowDialog(owner);
        }
        else
        {
            hostWindow.Show();
        }

        return true;
    }

    private static T InvokeOnUiThread<T>(Func<T> callback, T fallback, string errorMessage)
    {
        try
        {
            return Dispatcher.UIThread.CheckAccess()
                ? callback()
                : Dispatcher.UIThread.Invoke(callback);
        }
        catch (Exception ex)
        {
            Log.Instance.Trace(errorMessage, ex);
            return fallback;
        }
    }
}

#endif

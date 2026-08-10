using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Windows.Settings;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Utils;

internal sealed class MainAppPluginHostContext : IPluginHostContext
{
    private readonly Func<Window?> _ownerWindowProvider;

    public MainAppPluginHostContext(Func<Window?> ownerWindowProvider)
    {
        _ownerWindowProvider = ownerWindowProvider ?? throw new ArgumentNullException(nameof(ownerWindowProvider));
    }

    public PluginHostMode Mode => PluginHostMode.RealRuntime;
    public bool AllowSystemActions => true;
    public object? OwnerWindow => _ownerWindowProvider();

    public bool OpenPluginSettings(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        return ExecuteOnUiThread(() =>
        {
            var window = new PluginSettingsWindow(pluginId);
            PrepareWindow(window);
            ShowDialogBlocking(window);
            return true;
        }, false, $"Failed to open plugin settings for '{pluginId}'.");
    }

    public bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null)
    {
        if (dialogOrContent is null)
            return null;

        return ExecuteOnUiThread(() =>
        {
            if (dialogOrContent is Window dialogWindow)
            {
                PrepareWindow(dialogWindow);
                return ShowDialogBlocking(dialogWindow);
            }

            if (dialogOrContent is not Control content)
                return null;

            var hostWindow = new Window
            {
                Title = string.IsNullOrWhiteSpace(title) ? "Plugin Dialog" : title,
                Content = content,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                MinWidth = 480,
                MinHeight = 320
            };

            PrepareWindow(hostWindow);
            ShowDialogBlocking(hostWindow);
            return true;
        }, null, $"Failed to show plugin dialog '{dialogOrContent.GetType().FullName}'.");
    }

    private Window? ResolveOwner(Window window)
    {
        var ownerWindow = _ownerWindowProvider();
        return ownerWindow is not null && !ReferenceEquals(ownerWindow, window) && ownerWindow.IsVisible
            ? ownerWindow
            : null;
    }

    private void PrepareWindow(Window window)
    {
        if (window.WindowStartupLocation == WindowStartupLocation.Manual)
            window.WindowStartupLocation = ResolveOwner(window) is null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner;
    }

    // AVALONIA: IPluginHostContext.ShowDialog is synchronous, but Avalonia's
    // Window.ShowDialog is async and requires a visible owner window. Block the
    // UI thread on the dialog task (same pattern as Utils/NativeMessageBox.Show)
    // and fall back to Show() when no visible owner is available.
    private bool? ShowDialogBlocking(Window window)
    {
        var owner = ResolveOwner(window);
        if (owner is not null)
            return window.ShowDialog<bool?>(owner).GetAwaiter().GetResult();

        var completion = new TaskCompletionSource<bool?>(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Closed += (_, _) => completion.TrySetResult(window.Tag as bool? ?? true);
        window.Show();
        return completion.Task.GetAwaiter().GetResult();
    }

    private static T ExecuteOnUiThread<T>(Func<T> callback, T fallback, string errorMessage)
    {
        try
        {
            var dispatcher = Dispatcher.UIThread;
            return dispatcher is null
                ? fallback
                : dispatcher.CheckAccess() ? callback() : dispatcher.Invoke(callback);
        }
        catch (Exception ex)
        {
            Log.Instance.Trace(errorMessage, ex);
            return fallback;
        }
    }
}

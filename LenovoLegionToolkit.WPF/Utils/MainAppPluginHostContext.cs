using System;
using System.Windows;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Windows.Settings;

namespace LenovoLegionToolkit.WPF.Utils;

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
            window.ShowDialog();
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
                return dialogWindow.ShowDialog();
            }

            if (dialogOrContent is not UIElement content)
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
            hostWindow.ShowDialog();
            return true;
        }, null, $"Failed to show plugin dialog '{dialogOrContent.GetType().FullName}'.");
    }

    private void PrepareWindow(Window window)
    {
        if (window.Owner is null &&
            _ownerWindowProvider() is Window ownerWindow &&
            !ReferenceEquals(ownerWindow, window))
        {
            window.Owner = ownerWindow;
        }

        if (window.WindowStartupLocation == WindowStartupLocation.Manual)
            window.WindowStartupLocation = window.Owner is null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner;
    }

    private static T ExecuteOnUiThread<T>(Func<T> callback, T fallback, string errorMessage)
    {
        try
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null)
                return fallback;

            return dispatcher.CheckAccess() ? callback() : dispatcher.Invoke(callback);
        }
        catch (Exception ex)
        {
            Log.Instance.Trace(errorMessage, ex);
            return fallback;
        }
    }
}

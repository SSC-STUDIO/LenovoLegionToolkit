using System;
using System.Windows;
using UniversalDeviceToolkit.Lib.Plugins;

namespace PluginWorkbench;

internal sealed class PluginWorkbenchHostContext : IPluginHostContext
{
    private readonly Func<PluginHostMode> _modeProvider;
    private readonly Func<bool> _allowSystemActionsProvider;
    private readonly Func<Window?> _ownerWindowProvider;
    private readonly Func<string, bool> _openPluginSettings;

    public PluginWorkbenchHostContext(
        Func<PluginHostMode> modeProvider,
        Func<bool> allowSystemActionsProvider,
        Func<Window?> ownerWindowProvider,
        Func<string, bool> openPluginSettings)
    {
        _modeProvider = modeProvider;
        _allowSystemActionsProvider = allowSystemActionsProvider;
        _ownerWindowProvider = ownerWindowProvider;
        _openPluginSettings = openPluginSettings;
    }

    public PluginHostMode Mode => _modeProvider();
    public bool AllowSystemActions => _allowSystemActionsProvider();
    public object? OwnerWindow => _ownerWindowProvider();

    public bool OpenPluginSettings(string pluginId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        return _openPluginSettings(pluginId);
    }

    public bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null)
    {
        ArgumentNullException.ThrowIfNull(dialogOrContent);

        var ownerWindow = _ownerWindowProvider();

        if (dialogOrContent is Window dialogWindow)
        {
            if (ownerWindow is not null && !ReferenceEquals(ownerWindow, dialogWindow))
            {
                dialogWindow.Owner = ownerWindow;
            }

            dialogWindow.ShowDialog();
            return true;
        }

        if (dialogOrContent is UIElement content)
        {
            var hostWindow = new HostedPluginContentWindow(content, string.IsNullOrWhiteSpace(title) ? "Plugin Dialog" : title);
            if (ownerWindow is not null)
            {
                hostWindow.Owner = ownerWindow;
            }

            hostWindow.ShowDialog();
            return true;
        }

        return false;
    }
}

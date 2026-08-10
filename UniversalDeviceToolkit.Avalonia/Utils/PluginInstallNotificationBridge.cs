using System;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Resources;

namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Mirrors <see cref="PluginInstallCoordinator"/> download progress into a persistent
/// progress toast, so users see plugin downloads even after navigating away from the
/// extensions page. The page's own success/failure snackbars remain the completion signal.
/// </summary>
public sealed class PluginInstallNotificationBridge : IDisposable
{
    private readonly PluginInstallCoordinator _coordinator;
    private Guid _activeToastId;
    private string? _toastPluginId;

    public PluginInstallNotificationBridge(PluginInstallCoordinator coordinator)
    {
        _coordinator = coordinator;
        _coordinator.Changed += OnCoordinatorChanged;
        Sync();
    }

    private void OnCoordinatorChanged(object? sender, EventArgs e)
    {
        var dispatcher = Dispatcher.UIThread;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Post(Sync);
            return;
        }

        Sync();
    }

    private void Sync()
    {
        if (_coordinator.IsActive)
        {
            var pluginId = _coordinator.PluginId ?? string.Empty;
            if (_activeToastId == Guid.Empty ||
                !string.Equals(_toastPluginId, pluginId, StringComparison.OrdinalIgnoreCase))
            {
                ProgressToastHelper.Complete(_activeToastId);
                _activeToastId = ProgressToastHelper.Start(
                    string.IsNullOrWhiteSpace(_coordinator.PluginDisplayName)
                        ? Resource.PluginExtensionsPage_Downloading
                        : _coordinator.PluginDisplayName!);
                _toastPluginId = pluginId;
            }

            ProgressToastHelper.Update(_activeToastId, _coordinator.Progress, _coordinator.StatusText);
            return;
        }

        if (_activeToastId != Guid.Empty)
        {
            ProgressToastHelper.Complete(_activeToastId);
            _activeToastId = Guid.Empty;
            _toastPluginId = null;
        }
    }

    public void Dispose()
    {
        _coordinator.Changed -= OnCoordinatorChanged;
        if (_activeToastId != Guid.Empty)
        {
            ProgressToastHelper.Complete(_activeToastId);
            _activeToastId = Guid.Empty;
        }
    }
}

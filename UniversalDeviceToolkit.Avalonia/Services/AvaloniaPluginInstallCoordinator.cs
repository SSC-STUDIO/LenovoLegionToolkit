using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Avalonia.Localization;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Serializes online plugin install, update and uninstall work so only one
/// plugin operation runs at a time. Requests are deduplicated by plugin id and
/// progress is surfaced through the Changed event for the plugin store UI.
/// The page passes host-neutral installer callbacks, so this coordinator stays
/// usable on every TFM the Avalonia host builds for.
/// </summary>
public sealed class AvaloniaPluginInstallCoordinator
{
    /// <summary>Process-wide singleton consumed by the plugin store UI.</summary>
    public static AvaloniaPluginInstallCoordinator Current { get; } = new();

    private readonly object _sync = new();
    private readonly Queue<QueuedRequest> _queue = new();
    private readonly HashSet<string> _queuedIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Batch> _pendingBatches = new();
    private Task? _processor;
    private string? _currentPluginId;
    private string? _statusText;
    private bool _isActive;

    public bool IsActive
    {
        get
        {
            lock (_sync)
                return _isActive || _queue.Count > 0;
        }
    }

    public string? CurrentPluginId
    {
        get
        {
            lock (_sync)
                return _currentPluginId;
        }
    }

    /// <summary>
    /// Install progress in percent, or null while the operation runs without a
    /// progress source (indeterminate). The host installer callbacks used by the
    /// page do not report byte-level progress, so the store UI shows an
    /// indeterminate bar while a plugin is queued or active.
    /// </summary>
    public double? Progress
    {
        get
        {
            lock (_sync)
                return null;
        }
    }

    public string? StatusText
    {
        get
        {
            lock (_sync)
                return _statusText;
        }
    }

    public event Action? Changed;

    public Task InstallAsync(IEnumerable<string> pluginIds, Func<string, Task> installer) =>
        EnqueueAsync(pluginIds, installer, "install");

    public Task UpdateAsync(IEnumerable<string> pluginIds, Func<string, Task> installer) =>
        EnqueueAsync(pluginIds, installer, "update");

    public Task UninstallAsync(IEnumerable<string> pluginIds, Func<string, Task> installer) =>
        EnqueueAsync(pluginIds, installer, "uninstall");

    /// <summary>
    /// Whether the plugin id is currently queued or being processed. The store
    /// UI uses this to show per-plugin progress and to keep its action buttons
    /// disabled while the operation is pending.
    /// </summary>
    public bool IsQueuedOrActive(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        lock (_sync)
            return _queuedIds.Contains(pluginId)
                || string.Equals(_currentPluginId, pluginId, StringComparison.OrdinalIgnoreCase);
    }

    private Task EnqueueAsync(
        IEnumerable<string> pluginIds,
        Func<string, Task> installer,
        string operation)
    {
        if (pluginIds is null || installer is null)
            return Task.CompletedTask;

        Batch? batch = null;
        lock (_sync)
        {
            foreach (var pluginId in pluginIds)
            {
                if (string.IsNullOrWhiteSpace(pluginId) || !_queuedIds.Add(pluginId))
                    continue;

                batch ??= new Batch();
                batch.Remaining++;
                _queue.Enqueue(new QueuedRequest(pluginId, installer, operation, batch));
            }

            if (batch is null)
                return Task.CompletedTask;

            _pendingBatches.Add(batch);
            EnsureProcessorStarted();
        }

        RaiseChanged();
        return batch.Completion.Task;
    }

    private void EnsureProcessorStarted()
    {
        if (_processor is { IsCompleted: false })
            return;

        _processor = ProcessQueueAsync();
    }

    private async Task ProcessQueueAsync()
    {
        while (true)
        {
            QueuedRequest request;
            lock (_sync)
            {
                if (_queue.Count == 0)
                {
                    _processor = null;
                    return;
                }

                request = _queue.Dequeue();
                _currentPluginId = request.PluginId;
                _isActive = true;
                _statusText = FormatStatus(request.Operation, request.PluginId);
            }

            RaiseChanged();

            try
            {
                await request.Installer(request.PluginId).ConfigureAwait(false);
            }
            catch
            {
                // A failed plugin operation must not stop the remaining queue.
            }
            finally
            {
                CompleteRequest(request);
            }
        }
    }

    private void CompleteRequest(QueuedRequest request)
    {
        lock (_sync)
        {
            _queuedIds.Remove(request.PluginId);
            request.Batch.Remaining--;
            if (_queue.Count == 0)
            {
                _isActive = false;
                _currentPluginId = null;
                _statusText = null;
            }

            if (request.Batch.Remaining == 0)
            {
                _pendingBatches.Remove(request.Batch);
                request.Batch.Completion.TrySetResult();
            }
        }

        RaiseChanged();
    }

    private static string FormatStatus(string operation, string pluginId) => operation switch
    {
        "install" => string.Format(
            CultureInfo.CurrentCulture,
            AvaloniaLocalization.GetString(
                "PluginExtensionsPage_InstallingProgress",
                "Installing {0}..."),
            pluginId),
        "update" => string.Format(
            CultureInfo.CurrentCulture,
            AvaloniaLocalization.GetString(
                "PluginExtensionsPage_UpdatingProgress",
                "Updating {0}..."),
            pluginId),
        "uninstall" => string.Format(
            CultureInfo.CurrentCulture,
            AvaloniaLocalization.GetString(
                "PluginExtensionsPage_UninstallingProgress",
                "Uninstalling {0}..."),
            pluginId),
        _ => pluginId,
    };

    private void RaiseChanged() => Changed?.Invoke();

    private sealed class Batch
    {
        public int Remaining;

        public TaskCompletionSource Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class QueuedRequest(
        string pluginId,
        Func<string, Task> installer,
        string operation,
        Batch batch)
    {
        public string PluginId { get; } = pluginId;

        public Func<string, Task> Installer { get; } = installer;

        public string Operation { get; } = operation;

        public Batch Batch { get; } = batch;
    }
}

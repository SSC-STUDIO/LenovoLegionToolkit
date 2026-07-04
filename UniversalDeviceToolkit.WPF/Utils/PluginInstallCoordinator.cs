using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Plugins;
using UniversalDeviceToolkit.WPF.Resources;

namespace UniversalDeviceToolkit.WPF.Utils;

/// <summary>
/// Serializes online plugin installs and keeps progress visible across page navigation.
/// Additional install requests are queued; only one download runs at a time.
/// </summary>
public sealed class PluginInstallCoordinator(
    PluginRepositoryService pluginRepositoryService,
    IPluginManager pluginManager)
{
    private readonly object _sync = new();
    private readonly Queue<InstallRequest> _queue = new();
    private readonly HashSet<string> _queuedPluginIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Task<bool>> _tasksByPluginId = new(StringComparer.OrdinalIgnoreCase);

    private Task? _processTask;
    private string _pluginId = string.Empty;
    private double _progress;
    private string _statusText = string.Empty;
    private bool _progressHandlerAttached;

    public bool IsActive { get; private set; }

    public bool HasPendingWork
    {
        get
        {
            lock (_sync)
                return IsActive || _queue.Count > 0;
        }
    }

    public string? PluginId
    {
        get
        {
            lock (_sync)
                return string.IsNullOrWhiteSpace(_pluginId) ? null : _pluginId;
        }
    }

    public double Progress
    {
        get
        {
            lock (_sync)
                return _progress;
        }
    }

    public string StatusText
    {
        get
        {
            lock (_sync)
                return _statusText;
        }
    }

    public event EventHandler? Changed;

    public bool IsQueued(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        lock (_sync)
            return _queuedPluginIds.Contains(pluginId);
    }

    public Task<bool> InstallAsync(PluginManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        lock (_sync)
        {
            if (_tasksByPluginId.TryGetValue(manifest.Id, out var existing) && !existing.IsCompleted)
                return existing;
        }

        var request = new InstallRequest(manifest, cancellationToken);
        lock (_sync)
        {
            _queue.Enqueue(request);
            _queuedPluginIds.Add(manifest.Id);
            _tasksByPluginId[manifest.Id] = request.Completion.Task;
        }

        RaiseChanged();
        EnsureProcessorStarted();
        return request.Completion.Task;
    }

    private void EnsureProcessorStarted()
    {
        lock (_sync)
        {
            if (_processTask is { IsCompleted: false })
                return;

            _processTask = ProcessQueueAsync();
        }
    }

    private async Task ProcessQueueAsync()
    {
        while (true)
        {
            InstallRequest request;
            lock (_sync)
            {
                if (_queue.Count == 0)
                {
                    _processTask = null;
                    return;
                }

                request = _queue.Dequeue();
                _queuedPluginIds.Remove(request.Manifest.Id);
            }

            var success = false;
            try
            {
                success = await RunInstallAsync(request.Manifest, request.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                request.Completion.TrySetCanceled(request.CancellationToken);
            }
            catch (Exception ex)
            {
                request.Completion.TrySetException(ex);
            }
            finally
            {
                lock (_sync)
                    _tasksByPluginId.Remove(request.Manifest.Id);
            }

            if (!request.Completion.Task.IsCompleted)
                request.Completion.TrySetResult(success);

            RaiseChanged();
        }
    }

    private async Task<bool> RunInstallAsync(PluginManifest manifest, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _pluginId = manifest.Id;
            _progress = 0;
            _statusText = Resource.PluginExtensionsPage_PreparingDownload;
        }

        IsActive = true;
        RaiseChanged();

        try
        {
            if (pluginManager.IsInstalled(manifest.Id))
                pluginManager.StopPlugin(manifest.Id);

            AttachProgressHandler();
            return await pluginRepositoryService.DownloadAndInstallPluginAsync(manifest);
        }
        finally
        {
            DetachProgressHandler();

            lock (_sync)
            {
                IsActive = false;
                _pluginId = string.Empty;
                _progress = 0;
                _statusText = string.Empty;
            }

            RaiseChanged();
        }
    }

    private void AttachProgressHandler()
    {
        if (_progressHandlerAttached)
            return;

        pluginRepositoryService.DownloadProgressChanged += OnDownloadProgressChanged;
        _progressHandlerAttached = true;
    }

    private void DetachProgressHandler()
    {
        if (!_progressHandlerAttached)
            return;

        pluginRepositoryService.DownloadProgressChanged -= OnDownloadProgressChanged;
        _progressHandlerAttached = false;
    }

    private void OnDownloadProgressChanged(object? sender, PluginDownloadProgress progress)
    {
        lock (_sync)
        {
            if (string.IsNullOrWhiteSpace(_pluginId) ||
                !string.Equals(progress.PluginId, _pluginId, StringComparison.OrdinalIgnoreCase))
                return;

            _progress = progress.ProgressPercentage;
            _statusText = FormatStatusText(progress);
        }

        RaiseChanged();
    }

    private static string FormatStatusText(PluginDownloadProgress progress)
    {
        if (progress.IsCompleted)
            return Resource.PluginExtensionsPage_DownloadCompleted;

        if (progress.TotalBytes > 0)
        {
            var downloadedMb = progress.BytesDownloaded / 1024.0 / 1024.0;
            var totalMb = progress.TotalBytes / 1024.0 / 1024.0;
            return string.Format(
                CultureInfo.CurrentCulture,
                Resource.PluginExtensionsPage_DownloadingWithProgress,
                downloadedMb,
                totalMb,
                progress.ProgressPercentage);
        }

        return Resource.PluginExtensionsPage_Downloading;
    }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

    private sealed class InstallRequest(PluginManifest manifest, CancellationToken cancellationToken)
    {
        public PluginManifest Manifest { get; } = manifest;

        public CancellationToken CancellationToken { get; } = cancellationToken;

        public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

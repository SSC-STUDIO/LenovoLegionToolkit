using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Avalonia.Localization;

namespace UniversalDeviceToolkit.Avalonia.Services;

public enum PluginOperationStatus
{
    Succeeded,
    Failed,
    Canceled,
}

public sealed record PluginOperationResult(string PluginId, PluginOperationStatus Status, string? Error = null)
{
    public bool Succeeded => Status == PluginOperationStatus.Succeeded;
}

public sealed record PluginOperationBatchResult(IReadOnlyList<PluginOperationResult> Operations)
{
    public bool Succeeded => Operations.Count > 0 && Operations.All(operation => operation.Succeeded);

    public bool HasFailures => Operations.Any(operation => operation.Status == PluginOperationStatus.Failed);

    public bool HasCanceled => Operations.Any(operation => operation.Status == PluginOperationStatus.Canceled);

    public string? ErrorMessage => Operations.FirstOrDefault(operation => !operation.Succeeded)?.Error;
}

/// <summary>
/// Serializes online plugin lifecycle work and preserves the result of every
/// request. A failed or canceled request never stops later requests, and the
/// same plugin can be retried after the request has completed.
/// </summary>
public sealed class AvaloniaPluginInstallCoordinator
{
    public static AvaloniaPluginInstallCoordinator Current { get; } = new();

    private readonly object _sync = new();
    private readonly Queue<QueuedRequest> _queue = new();
    private readonly Dictionary<string, Task<PluginOperationResult>> _operationsByPluginId = new(StringComparer.OrdinalIgnoreCase);
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

    public double? Progress => null;

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
        InstallAsync(pluginIds, async (pluginId, _) =>
        {
            await installer(pluginId).ConfigureAwait(false);
            return true;
        });

    public Task UpdateAsync(IEnumerable<string> pluginIds, Func<string, Task> installer) =>
        UpdateAsync(pluginIds, async (pluginId, _) =>
        {
            await installer(pluginId).ConfigureAwait(false);
            return true;
        });

    public Task UninstallAsync(IEnumerable<string> pluginIds, Func<string, Task> installer) =>
        UninstallAsync(pluginIds, async (pluginId, _) =>
        {
            await installer(pluginId).ConfigureAwait(false);
            return true;
        });

    public Task<PluginOperationBatchResult> InstallAsync(IEnumerable<string> pluginIds, Func<string, Task<bool>> installer) =>
        InstallAsync(pluginIds, (pluginId, _) => installer(pluginId));

    public Task<PluginOperationBatchResult> UpdateAsync(IEnumerable<string> pluginIds, Func<string, Task<bool>> installer) =>
        UpdateAsync(pluginIds, (pluginId, _) => installer(pluginId));

    public Task<PluginOperationBatchResult> UninstallAsync(IEnumerable<string> pluginIds, Func<string, Task<bool>> installer) =>
        UninstallAsync(pluginIds, (pluginId, _) => installer(pluginId));

    public Task<PluginOperationBatchResult> InstallAsync(
        IEnumerable<string> pluginIds,
        Func<string, CancellationToken, Task<bool>> installer,
        CancellationToken cancellationToken = default) =>
        EnqueueAsync(pluginIds, installer, "install", cancellationToken);

    public Task<PluginOperationBatchResult> UpdateAsync(
        IEnumerable<string> pluginIds,
        Func<string, CancellationToken, Task<bool>> installer,
        CancellationToken cancellationToken = default) =>
        EnqueueAsync(pluginIds, installer, "update", cancellationToken);

    public Task<PluginOperationBatchResult> UninstallAsync(
        IEnumerable<string> pluginIds,
        Func<string, CancellationToken, Task<bool>> installer,
        CancellationToken cancellationToken = default) =>
        EnqueueAsync(pluginIds, installer, "uninstall", cancellationToken);

    public bool IsQueuedOrActive(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        lock (_sync)
            return _operationsByPluginId.ContainsKey(pluginId);
    }

    private Task<PluginOperationBatchResult> EnqueueAsync(
        IEnumerable<string> pluginIds,
        Func<string, CancellationToken, Task<bool>> installer,
        string operation,
        CancellationToken cancellationToken)
    {
        if (pluginIds is null || installer is null)
            return Task.FromResult(new PluginOperationBatchResult(Array.Empty<PluginOperationResult>()));

        var operations = new List<Task<PluginOperationResult>>();
        lock (_sync)
        {
            foreach (var rawPluginId in pluginIds)
            {
                var pluginId = rawPluginId?.Trim();
                if (string.IsNullOrWhiteSpace(pluginId))
                    continue;

                if (!_operationsByPluginId.TryGetValue(pluginId, out var resultTask))
                {
                    var request = new QueuedRequest(pluginId, installer, operation, cancellationToken);
                    resultTask = request.Completion.Task;
                    _operationsByPluginId.Add(pluginId, resultTask);
                    _queue.Enqueue(request);
                }

                operations.Add(resultTask);
            }

            if (_queue.Count > 0)
                EnsureProcessorStarted();
        }

        RaiseChanged();
        return CompleteBatchAsync(operations);
    }

    private static async Task<PluginOperationBatchResult> CompleteBatchAsync(
        IReadOnlyCollection<Task<PluginOperationResult>> operations)
    {
        if (operations.Count == 0)
            return new PluginOperationBatchResult(Array.Empty<PluginOperationResult>());

        return new PluginOperationBatchResult(await Task.WhenAll(operations).ConfigureAwait(false));
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
            var result = await ProcessRequestAsync(request).ConfigureAwait(false);
            request.Completion.TrySetResult(result);

            lock (_sync)
            {
                _operationsByPluginId.Remove(request.PluginId);
                if (_queue.Count == 0)
                {
                    _isActive = false;
                    _currentPluginId = null;
                    _statusText = null;
                }
            }

            RaiseChanged();
        }
    }

    private static async Task<PluginOperationResult> ProcessRequestAsync(QueuedRequest request)
    {
        try
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            var succeeded = await request.Installer(request.PluginId, request.CancellationToken).ConfigureAwait(false);
            return succeeded
                ? new PluginOperationResult(request.PluginId, PluginOperationStatus.Succeeded)
                : new PluginOperationResult(request.PluginId, PluginOperationStatus.Failed, "The plugin operation did not complete.");
        }
        catch (OperationCanceledException) when (request.CancellationToken.IsCancellationRequested)
        {
            return new PluginOperationResult(request.PluginId, PluginOperationStatus.Canceled, "The plugin operation was canceled.");
        }
        catch (Exception ex)
        {
            return new PluginOperationResult(request.PluginId, PluginOperationStatus.Failed, ex.Message);
        }
    }

    private static string FormatStatus(string operation, string pluginId) => operation switch
    {
        "install" => string.Format(CultureInfo.CurrentCulture, AvaloniaLocalization.GetString("PluginExtensionsPage_InstallingProgress", "Installing {0}..."), pluginId),
        "update" => string.Format(CultureInfo.CurrentCulture, AvaloniaLocalization.GetString("PluginExtensionsPage_UpdatingProgress", "Updating {0}..."), pluginId),
        "uninstall" => string.Format(CultureInfo.CurrentCulture, AvaloniaLocalization.GetString("PluginExtensionsPage_UninstallingProgress", "Uninstalling {0}..."), pluginId),
        _ => pluginId,
    };

    private void RaiseChanged() => Changed?.Invoke();

    private sealed class QueuedRequest(
        string pluginId,
        Func<string, CancellationToken, Task<bool>> installer,
        string operation,
        CancellationToken cancellationToken)
    {
        public string PluginId { get; } = pluginId;
        public Func<string, CancellationToken, Task<bool>> Installer { get; } = installer;
        public string Operation { get; } = operation;
        public CancellationToken CancellationToken { get; } = cancellationToken;
        public TaskCompletionSource<PluginOperationResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

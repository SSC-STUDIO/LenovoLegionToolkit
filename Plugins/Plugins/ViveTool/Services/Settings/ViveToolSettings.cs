using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using NeoSmart.AsyncLock;
using LenovoLegionToolkit.Plugins.Shared;

namespace LenovoLegionToolkit.Plugins.ViveTool.Services.Settings;

/// <summary>
/// ViVeTool plugin settings manager
/// </summary>
public class ViveToolSettings
{
    private static readonly string SettingsFilePath = Path.Combine(
        Folders.AppData,
        "ViveTool",
        "settings.json");

    private readonly AsyncLock _dataLock = new();
    private SettingsData _data = new();
    private readonly object _saveLock = new();
    private CancellationTokenSource? _saveCancellationTokenSource;
    private bool _isLoading;

    public string? ViveToolPath
    {
        get
        {
            using (_dataLock.Lock())
            {
                return _data.ViveToolPath;
            }
        }
        set
        {
            bool shouldSave;
            using (_dataLock.Lock())
            {
                _data.ViveToolPath = value;
                shouldSave = !_isLoading;
            }
            if (shouldSave)
            {
                _ = SaveAsyncDelayed();
            }
        }
    }

    private async Task SaveAsyncDelayed()
    {
        CancellationTokenSource cts;
        lock (_saveLock)
        {
            // Cancel and dispose previous save operation
            if (_saveCancellationTokenSource is not null)
            {
                _saveCancellationTokenSource.Cancel();
                _saveCancellationTokenSource.Dispose();
            }
            _saveCancellationTokenSource = new CancellationTokenSource();
            cts = _saveCancellationTokenSource;
        }

        try
        {
            // Wait a bit to batch multiple property changes
            await Task.Delay(500, cts.Token);

            // Save if not cancelled
            if (!cts.Token.IsCancellationRequested)
            {
                await SaveAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when a new save is triggered
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"Error in delayed save: {ex.Message}", ex);
        }
        finally
        {
            // Clear and dispose our CTS reference now that the delay has completed
            lock (_saveLock)
            {
                if (ReferenceEquals(_saveCancellationTokenSource, cts))
                {
                    _saveCancellationTokenSource.Dispose();
                    _saveCancellationTokenSource = null;
                }
            }
        }
    }

    public async Task LoadAsync()
    {
        string? viveToolPathBeforeLoad = null;

        try
        {
            using (await _dataLock.LockAsync().ConfigureAwait(false))
            {
                _isLoading = true;
                viveToolPathBeforeLoad = _data.ViveToolPath;
            }

            SettingsData? loadedData = null;
            if (File.Exists(SettingsFilePath))
            {
                var json = await File.ReadAllTextAsync(SettingsFilePath);
                loadedData = JsonSerializer.Deserialize<SettingsData>(json);
            }

            using (await _dataLock.LockAsync().ConfigureAwait(false))
            {
                // Preserve user mutations that happened while the settings file was loading.
                var viveToolPathChangedDuringLoad = !string.Equals(_data.ViveToolPath, viveToolPathBeforeLoad, StringComparison.Ordinal);
                if (!viveToolPathChangedDuringLoad)
                {
                    _data = loadedData ?? new SettingsData();
                }

                _isLoading = false;
            }
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"Error loading settings: {ex.Message}", ex);

            using (await _dataLock.LockAsync().ConfigureAwait(false))
            {
                var viveToolPathChangedDuringLoad = !string.Equals(_data.ViveToolPath, viveToolPathBeforeLoad, StringComparison.Ordinal);
                if (!viveToolPathChangedDuringLoad)
                {
                    _data = new SettingsData();
                }

                _isLoading = false;
            }
        }
    }

    public async Task SaveAsync()
    {
        lock (_saveLock)
        {
            _saveCancellationTokenSource?.Cancel();
            _saveCancellationTokenSource?.Dispose();
            _saveCancellationTokenSource = null;
        }

        SettingsData dataToSave;
        using (await _dataLock.LockAsync().ConfigureAwait(false))
        {
            dataToSave = new SettingsData
            {
                ViveToolPath = _data.ViveToolPath
            };
        }

        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(dataToSave, new JsonSerializerOptions { WriteIndented = true });
            var tempPath = SettingsFilePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, SettingsFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            // Clean up orphaned temp file if File.Move failed (target locked, I/O error, etc.)
            var tempPath = SettingsFilePath + ".tmp";
            try { if (File.Exists(tempPath)) File.Delete(tempPath); }
            catch (Exception cleanupEx) { PluginLog.Trace($"Failed to clean up temp file: {cleanupEx.Message}", cleanupEx); }
            PluginLog.Trace($"Error saving settings: {ex.Message}", ex);
        }
    }

    private class SettingsData
    {
        public string? ViveToolPath { get; set; }
    }
}

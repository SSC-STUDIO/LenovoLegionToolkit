using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using NeoSmart.AsyncLock;

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
                return _data.ViveToolPath;
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
                _ = SaveAsyncDelayed();
        }
    }

    private async Task SaveAsyncDelayed()
    {
        CancellationTokenSource cts;
        lock (_saveLock)
        {
            // Cancel previous save operation
            _saveCancellationTokenSource?.Cancel();
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
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in delayed save: {ex.Message}", ex);
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            using (await _dataLock.LockAsync().ConfigureAwait(false))
            {
                _isLoading = true;
            }

            SettingsData? loadedData = null;
            if (File.Exists(SettingsFilePath))
            {
                var json = await File.ReadAllTextAsync(SettingsFilePath);
                loadedData = JsonSerializer.Deserialize<SettingsData>(json);
            }

            using (await _dataLock.LockAsync().ConfigureAwait(false))
            {
                _data = loadedData ?? new SettingsData();
                _isLoading = false;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading settings: {ex.Message}", ex);
            
            using (await _dataLock.LockAsync().ConfigureAwait(false))
            {
                _data = new SettingsData();
                _isLoading = false;
            }
        }
    }

    public async Task SaveAsync()
    {
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
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error saving settings: {ex.Message}", ex);
        }
    }

    private class SettingsData
    {
        public string? ViveToolPath { get; set; }
    }
}

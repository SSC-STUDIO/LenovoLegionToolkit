using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Settings;

/// <summary>
/// Network acceleration settings manager
/// </summary>
public class NetworkAccelerationSettings
{
    private static readonly string SettingsFilePath = Path.Combine(
        Folders.AppData,
        "NetworkAcceleration",
        "settings.json");

    private SettingsData _data = new();
    private readonly object _saveLock = new();
    private CancellationTokenSource? _saveCancellationTokenSource;
    private bool _isLoading;

    public bool IsServiceEnabled
    {
        get => _data.IsServiceEnabled;
        set
        {
            _data.IsServiceEnabled = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool IsDnsOptimizationEnabled
    {
        get => _data.IsDnsOptimizationEnabled;
        set
        {
            _data.IsDnsOptimizationEnabled = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool IsRequestInterceptionEnabled
    {
        get => _data.IsRequestInterceptionEnabled;
        set
        {
            _data.IsRequestInterceptionEnabled = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool IsGithubAccelerationEnabled
    {
        get => _data.IsGithubAccelerationEnabled;
        set
        {
            _data.IsGithubAccelerationEnabled = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public string ProxyAddress
    {
        get => _data.ProxyAddress ?? string.Empty;
        set
        {
            _data.ProxyAddress = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    private async Task SaveAsyncDelayed()
    {
        lock (_saveLock)
        {
            // Cancel previous save operation
            _saveCancellationTokenSource?.Cancel();
            _saveCancellationTokenSource = new CancellationTokenSource();
        }

        try
        {
            // Wait a bit to batch multiple property changes
            await Task.Delay(500, _saveCancellationTokenSource.Token);
            
            // Save if not cancelled
            if (!_saveCancellationTokenSource.Token.IsCancellationRequested)
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
            _isLoading = true;
            if (File.Exists(SettingsFilePath))
            {
                var json = await File.ReadAllTextAsync(SettingsFilePath);
                _data = JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading settings: {ex.Message}", ex);
            _data = new SettingsData();
        }
        finally
        {
            _isLoading = false;
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error saving settings: {ex.Message}", ex);
        }
    }



    public int ProxyPort
    {
        get => _data.ProxyPort;
        set
        {
            _data.ProxyPort = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public int ConnectionTimeout
    {
        get => _data.ConnectionTimeout;
        set
        {
            _data.ConnectionTimeout = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool IsSteamAccelerationEnabled
    {
        get => _data.IsSteamAccelerationEnabled;
        set
        {
            _data.IsSteamAccelerationEnabled = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool IsDiscordAccelerationEnabled
    {
        get => _data.IsDiscordAccelerationEnabled;
        set
        {
            _data.IsDiscordAccelerationEnabled = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool IsNpmAccelerationEnabled
    {
        get => _data.IsNpmAccelerationEnabled;
        set
        {
            _data.IsNpmAccelerationEnabled = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool IsPypiAccelerationEnabled
    {
        get => _data.IsPypiAccelerationEnabled;
        set
        {
            _data.IsPypiAccelerationEnabled = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    private class SettingsData
    {
        public bool IsServiceEnabled { get; set; }
        public bool IsDnsOptimizationEnabled { get; set; }
        public bool IsRequestInterceptionEnabled { get; set; }
        public bool IsGithubAccelerationEnabled { get; set; }
        public string? ProxyAddress { get; set; }
        public int ProxyPort { get; set; } = 8888;
        public int ConnectionTimeout { get; set; } = 30;
        public bool IsSteamAccelerationEnabled { get; set; }
        public bool IsDiscordAccelerationEnabled { get; set; }
        public bool IsNpmAccelerationEnabled { get; set; }
        public bool IsPypiAccelerationEnabled { get; set; }
    }
}


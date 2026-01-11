using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using NeoSmart.AsyncLock;

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

    private readonly AsyncLock _dataLock = new();
    private SettingsData _data = new();
    private readonly object _saveLock = new();
    private CancellationTokenSource? _saveCancellationTokenSource;
    private bool _isLoading;

    public bool IsServiceEnabled
    {
        get
        {
            using (_dataLock.Lock())
                return _data.IsServiceEnabled;
        }
        set
        {
            bool shouldSave;
            using (_dataLock.Lock())
            {
                _data.IsServiceEnabled = value;
                shouldSave = !_isLoading;
            }
            if (shouldSave)
                _ = SaveAsyncDelayed();
        }
    }

    public bool IsDnsOptimizationEnabled
    {
        get
        {
            using (_dataLock.Lock())
                return _data.IsDnsOptimizationEnabled;
        }
        set
        {
            bool shouldSave;
            using (_dataLock.Lock())
            {
                _data.IsDnsOptimizationEnabled = value;
                shouldSave = !_isLoading;
            }
            if (shouldSave)
                _ = SaveAsyncDelayed();
        }
    }

    public bool IsRequestInterceptionEnabled
    {
        get
        {
            using (_dataLock.Lock())
                return _data.IsRequestInterceptionEnabled;
        }
        set
        {
            bool shouldSave;
            using (_dataLock.Lock())
            {
                _data.IsRequestInterceptionEnabled = value;
                shouldSave = !_isLoading;
            }
            if (shouldSave)
                _ = SaveAsyncDelayed();
        }
    }

    public bool IsGithubAccelerationEnabled
    {
        get
        {
            using (_dataLock.Lock())
                return _data.IsGithubAccelerationEnabled;
        }
        set
        {
            bool shouldSave;
            using (_dataLock.Lock())
            {
                _data.IsGithubAccelerationEnabled = value;
                shouldSave = !_isLoading;
            }
            if (shouldSave)
                _ = SaveAsyncDelayed();
        }
    }

    public string ProxyAddress
    {
        get
        {
            using (_dataLock.Lock())
                return _data.ProxyAddress ?? string.Empty;
        }
        set
        {
            bool shouldSave;
            using (_dataLock.Lock())
            {
                _data.ProxyAddress = value;
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
                IsServiceEnabled = _data.IsServiceEnabled,
                IsDnsOptimizationEnabled = _data.IsDnsOptimizationEnabled,
                IsRequestInterceptionEnabled = _data.IsRequestInterceptionEnabled,
                IsGithubAccelerationEnabled = _data.IsGithubAccelerationEnabled,
                ProxyAddress = _data.ProxyAddress,
                AutoStartService = _data.AutoStartService,
                ProxyPort = _data.ProxyPort,
                ConnectionTimeout = _data.ConnectionTimeout,
                IsSteamAccelerationEnabled = _data.IsSteamAccelerationEnabled,
                IsDiscordAccelerationEnabled = _data.IsDiscordAccelerationEnabled,
                IsNpmAccelerationEnabled = _data.IsNpmAccelerationEnabled,
                IsPypiAccelerationEnabled = _data.IsPypiAccelerationEnabled
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

    public bool AutoStartService
    {
        get
        {
            using (_dataLock.Lock())
                return _data.AutoStartService;
        }
        set
        {
            bool shouldSave;
            using (_dataLock.Lock())
            {
                _data.AutoStartService = value;
                shouldSave = !_isLoading;
            }
            if (shouldSave)
                _ = SaveAsyncDelayed();
        }
    }

    public int ProxyPort
    {
        get
        {
            using (_dataLock.Lock())
                return _data.ProxyPort;
        }
        set
        {
            bool shouldSave;
            using (_dataLock.Lock())
            {
                _data.ProxyPort = value;
                shouldSave = !_isLoading;
            }
            if (shouldSave)
                _ = SaveAsyncDelayed();
        }
    }

    public int ConnectionTimeout
    {
        get
        {
            using (_dataLock.Lock())
                return _data.ConnectionTimeout;
        }
        set
        {
            bool shouldSave;
            using (_dataLock.Lock())
            {
                _data.ConnectionTimeout = value;
                shouldSave = !_isLoading;
            }
            if (shouldSave)
                _ = SaveAsyncDelayed();
        }
    }

    public bool IsSteamAccelerationEnabled
    {
        get
        {
            using (_dataLock.Lock())
                return _data.IsSteamAccelerationEnabled;
        }
        set
        {
            bool shouldSave;
            using (_dataLock.Lock())
            {
                _data.IsSteamAccelerationEnabled = value;
                shouldSave = !_isLoading;
            }
            if (shouldSave)
                _ = SaveAsyncDelayed();
        }
    }

    public bool IsDiscordAccelerationEnabled
    {
        get
        {
            using (_dataLock.Lock())
                return _data.IsDiscordAccelerationEnabled;
        }
        set
        {
            bool shouldSave;
            using (_dataLock.Lock())
            {
                _data.IsDiscordAccelerationEnabled = value;
                shouldSave = !_isLoading;
            }
            if (shouldSave)
                _ = SaveAsyncDelayed();
        }
    }

    public bool IsNpmAccelerationEnabled
    {
        get
        {
            using (_dataLock.Lock())
                return _data.IsNpmAccelerationEnabled;
        }
        set
        {
            bool shouldSave;
            using (_dataLock.Lock())
            {
                _data.IsNpmAccelerationEnabled = value;
                shouldSave = !_isLoading;
            }
            if (shouldSave)
                _ = SaveAsyncDelayed();
        }
    }

    public bool IsPypiAccelerationEnabled
    {
        get
        {
            using (_dataLock.Lock())
                return _data.IsPypiAccelerationEnabled;
        }
        set
        {
            bool shouldSave;
            using (_dataLock.Lock())
            {
                _data.IsPypiAccelerationEnabled = value;
                shouldSave = !_isLoading;
            }
            if (shouldSave)
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
        public bool AutoStartService { get; set; }
        public int ProxyPort { get; set; } = 8888;
        public int ConnectionTimeout { get; set; } = 30;
        public bool IsSteamAccelerationEnabled { get; set; }
        public bool IsDiscordAccelerationEnabled { get; set; }
        public bool IsNpmAccelerationEnabled { get; set; }
        public bool IsPypiAccelerationEnabled { get; set; }
    }
}


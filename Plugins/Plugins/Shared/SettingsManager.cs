using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace LenovoLegionToolkit.Plugins.Shared;

/// <summary>
/// Unified settings persistence manager for all plugins.
/// Provides a standardized interface for saving and loading plugin settings.
/// </summary>
/// <typeparam name="T">The type of settings to manage</typeparam>
public class SettingsManager<T> where T : class, new()
{
    private const string _settingsFileName = "settings.json";
    private const string _settingsFileNameMpck = "settings.mpack";
    private static readonly string _defaultSettingsRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "UniversalDeviceToolkit",
        "plugins");

    private readonly string _settingsFilePath;
    private readonly string _settingsFilePathMpck;
    private readonly string _legacySettingsFilePath;
    private readonly string _legacyAppDataSettingsFilePath; // Pre-rebrand %LocalAppData%\LenovoLegionToolkit\plugins\<name>
    private readonly ILogger? _logger;
    private readonly object _lock = new object();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly Timer? _saveDebounceTimer; // Debounce timer for batching rapid saves
    private T? _cachedSettings;
    private string? _lastSavedJson; // Track last saved settings JSON for memory transaction
    private readonly bool _useMessagePack; // Use MessagePack for faster serialization
    private T? _pendingSettings; // Settings waiting to be saved (debounce)
    private readonly int _debounceDelayMs; // Debounce delay in milliseconds

    /// <summary>
    /// Event raised when settings are changed.
    /// </summary>
    public event EventHandler<T>? SettingsChanged;

    /// <summary>
    /// Initializes a new instance of the SettingsManager class.
    /// </summary>
    /// <param name="pluginName">The name of the plugin (used to determine settings file location)</param>
    /// <param name="logger">Optional logger for diagnostic messages</param>
    /// <param name="settingsRoot">Optional override for the settings root directory. Defaults to the current user's local application data.</param>
    /// <param name="useMessagePack">Use MessagePack for faster serialization (default: false, uses JSON)</param>
    /// <param name="enableDebounce">Enable save debounce (batch rapid saves, default: false)</param>
    /// <param name="debounceDelayMs">Debounce delay in milliseconds (default: 500ms)</param>
    /// <exception cref="ArgumentException">Thrown when pluginName is null or empty</exception>
    public SettingsManager(string pluginName, ILogger? logger = null, string? settingsRoot = null, bool useMessagePack = false, bool enableDebounce = false, int debounceDelayMs = 500)
    {
        if (string.IsNullOrWhiteSpace(pluginName))
        {
            throw new ArgumentException("Plugin name cannot be null or empty", nameof(pluginName));
        }

        _logger = logger;
        _useMessagePack = useMessagePack;

        var effectiveSettingsRoot = string.IsNullOrWhiteSpace(settingsRoot)
            ? _defaultSettingsRoot
            : settingsRoot;
        var pluginDirectory = Path.Combine(effectiveSettingsRoot, pluginName);
        Directory.CreateDirectory(pluginDirectory);
        _settingsFilePath = Path.Combine(pluginDirectory, _settingsFileName);
        _settingsFilePathMpck = Path.Combine(pluginDirectory, _settingsFileNameMpck);
        _legacySettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", pluginName, _settingsFileName);
        _legacyAppDataSettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LenovoLegionToolkit",
            "plugins",
            pluginName,
            _settingsFileName);

        if (enableDebounce)
        {
            _saveDebounceTimer = new Timer(OnSaveDebounceTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            _debounceDelayMs = debounceDelayMs;
        }
    }

    /// <summary>
    /// Loads settings from file, or creates default settings if file doesn't exist.
    /// Uses MessagePack or JSON based on _useMessagePack flag.
    /// </summary>
    public T Load()
    {
        lock (_lock)
        {
            try
            {
                if (_cachedSettings != null)
                {
                    return _cachedSettings;
                }

                EnsureLegacySettingsMigrated();

                var settingsFilePath = _useMessagePack ? _settingsFilePathMpck : _settingsFilePath;
                if (!File.Exists(settingsFilePath))
                {
                    _logger?.LogInformation("Settings file not found, creating default settings");
                    return _cachedSettings = new T();
                }

                if (_useMessagePack)
                {
                    using var stream = File.OpenRead(_settingsFilePathMpck);
                    var settings = MessagePackSerializer.Deserialize<T>(stream);
                    return _cachedSettings = settings ?? new T();
                }
                else
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<T>(json);
                    return _cachedSettings = settings ?? new T();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load settings from {FilePath}", _useMessagePack ? _settingsFilePathMpck : _settingsFilePath);
                return _cachedSettings = new T();
            }
        }
    }

    /// <summary>
    /// Saves settings to file asynchronously.
    /// </summary>
    public async Task<bool> SaveAsync(T settings, CancellationToken cancellationToken = default)
    {
        if (settings == null)
        {
            _logger?.LogError("Cannot save null settings");
            return false;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var tempPath = _settingsFilePath + ".tmp";
            await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                await fileStream.WriteAsync(bytes, cancellationToken);
            }
            File.Move(tempPath, _settingsFilePath, overwrite: true);
            _cachedSettings = settings;

            var handler = SettingsChanged;
            handler?.Invoke(this, settings);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save settings to {FilePath}", _settingsFilePath);
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Saves settings to file with optional debounce (batch rapid saves).
    /// If debounce is enabled, saves are delayed by _debounceDelayMs and batched.
    /// </summary>
    public bool SaveWithDebounce(T settings)
    {
        if (!(_saveDebounceTimer != null))
        {
            // Debounce not enabled, save immediately
            return Save(settings);
        }

        lock (_lock)
        {
            _pendingSettings = settings;
            _saveDebounceTimer.Change(_debounceDelayMs, Timeout.Infinite);
            _logger?.LogTrace("Save debounced, will execute in {Delay}ms", _debounceDelayMs);
        }
        return true;
    }

    /// <summary>
    /// Saves settings to file.
    /// </summary>
    public bool Save(T settings)
    {
        if (settings == null)
        {
            _logger?.LogError("Cannot save null settings");
            return false;
        }

        EventHandler<T>? handler;
        lock (_lock)
        {
            try
            {
                // Memory transaction: skip save if settings unchanged
                if (_lastSavedJson != null)
                {
                    // Fast path: compare serialized JSON directly
                    var currentJson = JsonSerializer.Serialize(settings);
                    if (string.Equals(currentJson, _lastSavedJson, StringComparison.Ordinal))
                    {
                        _logger?.LogTrace("Settings unchanged, skipping save");
                        return true;
                    }
                    _lastSavedJson = currentJson; // Update cache
                }
                else
                {
                    _lastSavedJson = JsonSerializer.Serialize(settings);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);

                var tempPath = _settingsFilePath + ".tmp";
                if (_useMessagePack)
                {
                    // MessagePack serialization (binary, faster)
                    var bytes = MessagePackSerializer.Serialize(settings);
                    File.WriteAllBytes(tempPath, bytes);
                    File.Move(tempPath, _settingsFilePathMpck, overwrite: true);
                    _lastSavedJson = Convert.ToBase64String(bytes); // Cache as base64 for memory transaction
                }
                else
                {
                    // JSON serialization (text, human-readable)
                    var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    _lastSavedJson = json; // Cache for memory transaction
                    File.WriteAllText(tempPath, json, Encoding.UTF8);
                    File.Move(tempPath, _settingsFilePath, overwrite: true);
                }
                _cachedSettings = settings;

                handler = SettingsChanged;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save settings to {FilePath}", _settingsFilePath);
                return false;
            }
        }

        handler?.Invoke(this, settings);
        return true;
    }

    /// <summary>
    /// Updates settings using the provided action and saves them.
    /// </summary>
    public bool Update(Action<T> updateAction)
    {
        if (updateAction == null)
        {
            _logger?.LogError("Update action cannot be null");
            return false;
        }

        try
        {
            var settings = Load();
            updateAction(settings);
            return Save(settings);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to update settings");
            return false;
        }
    }

    /// <summary>
    /// Clears the settings cache and optionally deletes the settings file.
    /// </summary>
    public void Clear(bool deleteFile = false)
    {
        lock (_lock)
        {
            _cachedSettings = null;
            _lastSavedJson = null;
            _pendingSettings = null;

            if (deleteFile && File.Exists(_settingsFilePath))
            {
                try
                {
                    File.Delete(_settingsFilePath);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to delete settings file");
                }
            }
        }
    }

    /// <summary>
    /// Disposes the SettingsManager and flushes any pending debounced save.
    /// </summary>
    public void Dispose()
    {
        if (_saveDebounceTimer != null)
        {
            _saveDebounceTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _saveDebounceTimer.Dispose();
            // Flush pending save
            if (_pendingSettings != null)
            {
                Save(_pendingSettings);
                _pendingSettings = null;
            }
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Handles debounce timer elapsed event.
    /// </summary>
    private void OnSaveDebounceTimerElapsed(object? state)
    {
        lock (_lock)
        {
            if (_pendingSettings != null)
            {
                var settings = _pendingSettings;
                _pendingSettings = null;
                Save(settings);
            }
        }
    }

    /// <summary>
    /// Fast equality check for settings (override in derived classes for better performance).
    /// </summary>
    protected virtual bool AreSettingsEqual(T? left, T? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        // Default: compare serialized JSON
        var leftJson = JsonSerializer.Serialize(left);
        var rightJson = JsonSerializer.Serialize(right);
        return string.Equals(leftJson, rightJson, StringComparison.Ordinal);
    }

    private void EnsureLegacySettingsMigrated()
    {
        if (File.Exists(_settingsFilePath))
        {
            return;
        }

        TryMigrateFromLegacySettings(_legacySettingsFilePath);
        TryMigrateFromLegacySettings(_legacyAppDataSettingsFilePath);
    }

    private void TryMigrateFromLegacySettings(string legacySettingsFilePath)
    {
        if (File.Exists(_settingsFilePath))
        {
            return;
        }

        if (string.Equals(_settingsFilePath, legacySettingsFilePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!File.Exists(legacySettingsFilePath))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);
            File.Copy(legacySettingsFilePath, _settingsFilePath, overwrite: false);
            _logger?.LogInformation("Migrated settings from legacy path {LegacyPath} to {SettingsPath}", legacySettingsFilePath, _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to migrate settings from legacy path {LegacyPath} to {SettingsPath}", legacySettingsFilePath, _settingsFilePath);
        }
    }
}

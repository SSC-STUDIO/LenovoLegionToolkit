using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
    private static readonly string _defaultSettingsRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LenovoLegionToolkit",
        "plugins");

    private readonly string _settingsFilePath;
    private readonly string _legacySettingsFilePath;
    private readonly ILogger? _logger;
    private readonly object _lock = new object();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private T? _cachedSettings;
    private string? _lastSavedJson; // Track last saved settings JSON for memory transaction

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
    /// <exception cref="ArgumentException">Thrown when pluginName is null or empty</exception>
    public SettingsManager(string pluginName, ILogger? logger = null, string? settingsRoot = null)
    {
        if (string.IsNullOrWhiteSpace(pluginName))
        {
            throw new ArgumentException("Plugin name cannot be null or empty", nameof(pluginName));
        }

        _logger = logger;

        var effectiveSettingsRoot = string.IsNullOrWhiteSpace(settingsRoot)
            ? _defaultSettingsRoot
            : settingsRoot;
        var pluginDirectory = Path.Combine(effectiveSettingsRoot, pluginName);
        Directory.CreateDirectory(pluginDirectory);
        _settingsFilePath = Path.Combine(pluginDirectory, _settingsFileName);
        _legacySettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", pluginName, _settingsFileName);
    }

    /// <summary>
    /// Loads settings from file, or creates default settings if file doesn't exist.
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

                if (!File.Exists(_settingsFilePath))
                {
                    _logger?.LogInformation("Settings file not found, creating default settings");
                    return _cachedSettings = new T();
                }

                var json = File.ReadAllText(_settingsFilePath);

                // Validate file size
                if (json.Length > Constants.MaxConfigFileSizeBytes)
                {
                    _logger?.LogError("Settings file exceeds maximum size limit");
                    return _cachedSettings = new T();
                }

                var settings = JsonSerializer.Deserialize<T>(json);
                return _cachedSettings = settings ?? new T();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load settings from {FilePath}", _settingsFilePath);
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

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                _lastSavedJson = json; // Cache for memory transaction

                var tempPath = _settingsFilePath + ".tmp";
                File.WriteAllText(tempPath, json, Encoding.UTF8);
                File.Move(tempPath, _settingsFilePath, overwrite: true);
                _cachedSettings = settings;
                _lastSavedJson = json; // Cache for memory transaction

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

        if (string.Equals(_settingsFilePath, _legacySettingsFilePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!File.Exists(_legacySettingsFilePath))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);
            File.Copy(_legacySettingsFilePath, _settingsFilePath, overwrite: false);
            _logger?.LogInformation("Migrated settings from legacy path {LegacyPath} to {SettingsPath}", _legacySettingsFilePath, _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to migrate settings from legacy path {LegacyPath} to {SettingsPath}", _legacySettingsFilePath, _settingsFilePath);
        }
    }
}

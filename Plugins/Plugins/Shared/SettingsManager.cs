using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace UniversalDeviceToolkit.Plugins.Shared;

/// <summary>
/// Unified settings persistence manager for all plugins.
/// Provides a standardized interface for saving and loading plugin settings.
/// </summary>
/// <typeparam name="T">The type of settings to manage</typeparam>
public class SettingsManager<T> : IDisposable where T : class, new()
{
    private const string _settingsFileName = "settings.json";
    private const string _settingsFileNameMpck = "settings.mpack";
    private static readonly string _defaultSettingsRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "UniversalDeviceToolkit",
        "plugins");

    private static readonly MessagePackSerializerOptions _messagePackOptions =
        MessagePackSerializerOptions.Standard
            .WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);

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
    /// Event raised when settings file is corrupted and defaults are returned.
    /// The argument is the path of the corrupted file that was backed up.
    /// </summary>
    public event EventHandler<string>? SettingsCorrupted;

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
                    var settings = MessagePackSerializer.Deserialize<T>(stream, _messagePackOptions);
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
                var settingsFilePath = _useMessagePack ? _settingsFilePathMpck : _settingsFilePath;
                _logger?.LogError(ex, "Failed to load settings from {FilePath} \u2014 returning defaults and backing up corrupted file", settingsFilePath);
                try
                {
                    if (File.Exists(settingsFilePath))
                    {
                        var backupPath = settingsFilePath + $".corrupt.{DateTime.Now:yyyyMMdd-HHmmss}";
                        File.Copy(settingsFilePath, backupPath, overwrite: true);
                        _logger?.LogWarning("Corrupted settings backed up to {BackupPath}", backupPath);
                        SettingsCorrupted?.Invoke(this, backupPath);
                    }
                }
                catch (Exception backupEx)
                {
                    _logger?.LogError(backupEx, "Failed to backup corrupted settings file");
                }
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
        EventHandler<T>? handler;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);

            if (_useMessagePack)
            {
                // Serialize once — reuse for both comparison and writing
                var bytes = MessagePackSerializer.Serialize(settings, _messagePackOptions);

                // Memory transaction: skip save if settings unchanged
                var currentSig = Convert.ToBase64String(bytes);
                bool skip;
                lock (_lock)
                {
                    skip = _lastSavedJson != null && string.Equals(currentSig, _lastSavedJson, StringComparison.Ordinal);
                }
                if (skip)
                {
                    _logger?.LogTrace("Settings unchanged (MessagePack), skipping async save");
                    lock (_lock) { _cachedSettings = settings; }
                    return true;
                }

                var tempMpckPath = _settingsFilePathMpck + ".tmp";
                try
                {
                    await using (var fileStream = new FileStream(tempMpckPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                    {
                        await fileStream.WriteAsync(bytes, cancellationToken);
                    }
                    File.Move(tempMpckPath, _settingsFilePathMpck, overwrite: true);
                }
                catch
                {
                    CleanupTempFile(tempMpckPath);
                    throw;
                }
                lock (_lock)
                {
                    _lastSavedJson = currentSig;
                    _cachedSettings = settings;
                }
            }
            else
            {
                // Compact form for memory transaction comparison (consistent with Save())
                var compactJson = JsonSerializer.Serialize(settings);
                bool skip;
                lock (_lock)
                {
                    skip = _lastSavedJson != null && string.Equals(compactJson, _lastSavedJson, StringComparison.Ordinal);
                }
                if (skip)
                {
                    _logger?.LogTrace("Settings unchanged, skipping async save");
                    lock (_lock) { _cachedSettings = settings; }
                    return true;
                }

                // Indented form for human-readable file
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var tempPath = _settingsFilePath + ".tmp";
                try
                {
                    await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                    {
                        var jsonBytes = Encoding.UTF8.GetBytes(json);
                        await fileStream.WriteAsync(jsonBytes, cancellationToken);
                    }
                    File.Move(tempPath, _settingsFilePath, overwrite: true);
                }
                catch
                {
                    CleanupTempFile(tempPath);
                    throw;
                }
                lock (_lock)
                {
                    _lastSavedJson = compactJson;
                    _cachedSettings = settings;
                }
            }

            handler = SettingsChanged;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save settings to {FilePath}", _useMessagePack ? _settingsFilePathMpck : _settingsFilePath);
            return false;
        }
        finally
        {
            _semaphore.Release();
        }

        // Fire SettingsChanged outside the semaphore to avoid deadlock when a subscriber
        // calls Save/SaveAsync on the same instance (matching the pattern in Save()).
        handler?.Invoke(this, settings);
        return true;
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
    /// Uses _semaphore (not _lock) for file-write mutual exclusion with SaveAsync.
    /// Memory transaction state (_lastSavedJson, _cachedSettings) is protected by _lock.
    /// </summary>
    public bool Save(T settings)
    {
        if (settings == null)
        {
            _logger?.LogError("Cannot save null settings");
            return false;
        }

        EventHandler<T>? handler;

        // Phase 1: memory transaction under _lock (re-entrant with Update/Load)
        byte[]? mpckBytes = null;
        string? indentedJson = null;
        lock (_lock)
        {
            try
            {
                // Pre-serialize once for both comparison and writing.
                string? currentSig = null;
                if (_useMessagePack)
                {
                    mpckBytes = MessagePackSerializer.Serialize(settings, _messagePackOptions);
                    currentSig = Convert.ToBase64String(mpckBytes);
                }
                else
                {
                    indentedJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                }

                // Memory transaction: skip save if settings unchanged.
                if (_lastSavedJson != null)
                {
                    var sigForCompare = _useMessagePack
                        ? currentSig
                        : JsonSerializer.Serialize(settings);
                    if (string.Equals(sigForCompare, _lastSavedJson, StringComparison.Ordinal))
                    {
                        _logger?.LogTrace("Settings unchanged, skipping save ({Mode})", _useMessagePack ? "MessagePack" : "JSON");
                        return true;
                    }
                    _lastSavedJson = sigForCompare;
                }
                else
                {
                    _lastSavedJson = _useMessagePack
                        ? currentSig
                        : JsonSerializer.Serialize(settings);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to prepare settings for save to {FilePath}", _useMessagePack ? _settingsFilePathMpck : _settingsFilePath);
                return false;
            }
        }

        // Phase 2: file write under _semaphore — mutually exclusive with SaveAsync
        _semaphore.Wait();
        try
        {
            if (_useMessagePack)
            {
                var tempMpckPath = _settingsFilePathMpck + ".tmp";
                try
                {
                    File.WriteAllBytes(tempMpckPath, mpckBytes!);
                    File.Move(tempMpckPath, _settingsFilePathMpck, overwrite: true);
                }
                catch
                {
                    CleanupTempFile(tempMpckPath);
                    throw;
                }
            }
            else
            {
                var tempPath = _settingsFilePath + ".tmp";
                try
                {
                    File.WriteAllText(tempPath, indentedJson!, Encoding.UTF8);
                    File.Move(tempPath, _settingsFilePath, overwrite: true);
                }
                catch
                {
                    CleanupTempFile(tempPath);
                    throw;
                }
            }

            lock (_lock)
            {
                _cachedSettings = settings;
            }

            handler = SettingsChanged;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save settings to {FilePath}", _useMessagePack ? _settingsFilePathMpck : _settingsFilePath);
            return false;
        }
        finally
        {
            _semaphore.Release();
        }

        // Fire SettingsChanged outside the semaphore to avoid deadlock when a subscriber
        // calls Save/SaveAsync on the same instance (matching the pattern in SaveAsync).
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

        T settings;
        lock (_lock)
        {
            try
            {
                settings = Load();
                updateAction(settings);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update settings");
                return false;
            }
        }

        return Save(settings);
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

            if (deleteFile)
            {
                DeleteIfExists(_settingsFilePath);
                DeleteIfExists(_settingsFilePathMpck);
            }
        }
    }

    private bool _disposed;

    /// <summary>
    /// Disposes the SettingsManager and flushes any pending debounced save.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_saveDebounceTimer != null)
        {
            _saveDebounceTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _saveDebounceTimer.Dispose();

            T? pending;
            lock (_lock)
            {
                pending = _pendingSettings;
                _pendingSettings = null;
            }

            // Flush pending save (outside _lock to avoid lock-order inversion with Save)
            if (pending != null)
            {
                Save(pending);
            }
        }

        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Handles debounce timer elapsed event.
    /// </summary>
    private void OnSaveDebounceTimerElapsed(object? state)
    {
        T? settingsToSave;
        lock (_lock)
        {
            if (_disposed || _pendingSettings == null)
            {
                return;
            }

            settingsToSave = _pendingSettings;
            _pendingSettings = null;
        }

        // Save outside _lock to avoid lock-order inversion with SaveAsync:
        // Timer holds _lock → Save() acquires _semaphore (Phase 2),
        // while SaveAsync holds _semaphore → tries _lock at line 234.
        Save(settingsToSave);
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

    private static void DeleteIfExists(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (Exception)
        {
            // Best-effort cleanup — file may be locked by another reader.
        }
    }

    /// <summary>
    /// Removes an orphaned temp file left behind when a save operation fails
    /// (cancellation, I/O error, or File.Move failure). Best-effort: a locked
    /// temp file is logged but does not propagate.
    /// </summary>
    private void CleanupTempFile(string tempPath)
    {
        try
        {
            DeleteIfExists(tempPath);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to clean up temp file {TempPath}", tempPath);
        }
    }
}

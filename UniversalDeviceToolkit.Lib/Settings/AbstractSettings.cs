using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Serialization;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Settings;

public abstract class AbstractSettings<T> where T : class, new()
{
    protected readonly JsonSerializerOptions JsonSerializerOptions;
    private readonly string _settingsStorePath;
    private readonly string _fileName;
    private readonly object _lock = new();
    private T? _cachedStore;
    private DateTime _lastLoadTime = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(5);

    protected virtual T Default => new();

    public T Store
    {
        get
        {
            lock (_lock)
            {
                return _cachedStore ??= LoadStore() ?? Default;
            }
        }
    }

    protected AbstractSettings(string filename)
    {
        if (!PathSecurity.IsValidFileName(filename))
            throw new ArgumentException($"Invalid settings filename: {filename}", nameof(filename));

        JsonSerializerOptions = LltJson.CreateSettingsOptions();
        ConfigureJsonSerializerOptions(JsonSerializerOptions);

        _fileName = filename;
        _settingsStorePath = Path.Combine(Folders.AppData, _fileName);

        if (!PathSecurity.IsPathWithinAllowedDirectory(_settingsStorePath, Folders.AppData))
            throw new InvalidOperationException($"Settings path escapes allowed directory: {_settingsStorePath}");
    }

    /// <summary>
    /// Optional converters / options tweaks per settings file (e.g. automation polymorphism, legacy GUID migration).
    /// </summary>
    protected virtual void ConfigureJsonSerializerOptions(JsonSerializerOptions options) { }

    public void SynchronizeStore()
    {
        lock (_lock)
        {
            var settingsSerialized = JsonSerializer.Serialize(_cachedStore ?? Default, JsonSerializerOptions);
            File.WriteAllText(_settingsStorePath, settingsSerialized);
            _lastLoadTime = DateTime.UtcNow;
        }
    }

    public async Task SynchronizeStoreAsync()
    {
        string settingsSerialized;
        lock (_lock)
        {
            settingsSerialized = JsonSerializer.Serialize(_cachedStore ?? Default, JsonSerializerOptions);
            _lastLoadTime = DateTime.UtcNow;
        }

        await File.WriteAllTextAsync(_settingsStorePath, settingsSerialized).ConfigureAwait(false);
    }

    public virtual T? LoadStore()
    {
        lock (_lock)
        {
            if (_cachedStore != null && DateTime.UtcNow - _lastLoadTime < _cacheDuration)
                return _cachedStore;

            T? store = null;
            try
            {
                var settingsSerialized = File.ReadAllText(_settingsStorePath);
                store = JsonSerializer.Deserialize<T>(settingsSerialized, JsonSerializerOptions);

                if (store is null)
                    TryBackup();
            }
            catch (FileNotFoundException)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Settings file {_fileName} not found; using defaults.");
            }
            catch (DirectoryNotFoundException)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Settings file {_fileName} directory not found; using defaults.");
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error loading settings for {_fileName}", ex);
                TryBackup();
            }

            _cachedStore = store;
            _lastLoadTime = DateTime.UtcNow;
            return store;
        }
    }

    public virtual async Task<T?> LoadStoreAsync()
    {
        T? store = null;

        lock (_lock)
        {
            if (_cachedStore != null && DateTime.UtcNow - _lastLoadTime < _cacheDuration)
                return _cachedStore;
        }

        try
        {
            var settingsSerialized = await File.ReadAllTextAsync(_settingsStorePath).ConfigureAwait(false);
            store = JsonSerializer.Deserialize<T>(settingsSerialized, JsonSerializerOptions);

            if (store is null)
                TryBackup();
        }
        catch (FileNotFoundException)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Settings file {_fileName} not found; using defaults.");
        }
        catch (DirectoryNotFoundException)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Settings file {_fileName} directory not found; using defaults.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading settings for {_fileName}", ex);
            TryBackup();
        }

        lock (_lock)
        {
            _cachedStore = store;
            _lastLoadTime = DateTime.UtcNow;
        }

        return store;
    }

    public void InvalidateCache()
    {
        lock (_lock)
        {
            _cachedStore = null;
            _lastLoadTime = DateTime.MinValue;
        }
    }

    private void TryBackup()
    {
        try
        {
            if (!File.Exists(_settingsStorePath))
                return;

            var backupFileName = $"{Path.GetFileNameWithoutExtension(_fileName)}_backup_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(_fileName)}";
            var backupFilePath = Path.Combine(Folders.AppData, backupFileName);
            File.Copy(_settingsStorePath, backupFilePath);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Unable to create backup for {_fileName}", ex);
        }
    }
}

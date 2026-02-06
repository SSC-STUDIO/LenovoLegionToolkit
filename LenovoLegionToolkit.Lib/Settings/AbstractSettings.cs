using System;
using System.IO;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LenovoLegionToolkit.Lib.Settings;

public abstract class AbstractSettings<T> where T : class, new()
{
    protected readonly JsonSerializerSettings JsonSerializerSettings;
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
        JsonSerializerSettings = new()
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.None,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Converters = { new StringEnumConverter() },
            MaxDepth = 32
        };

        _fileName = filename;
        _settingsStorePath = Path.Combine(Folders.AppData, _fileName);
    }

    public void SynchronizeStore()
    {
        lock (_lock)
        {
            var settingsSerialized = JsonConvert.SerializeObject(_cachedStore ?? Default, JsonSerializerSettings);
            File.WriteAllText(_settingsStorePath, settingsSerialized);
            _lastLoadTime = DateTime.UtcNow;
        }
    }

    public async Task SynchronizeStoreAsync()
    {
        string settingsSerialized;
        lock (_lock)
        {
            settingsSerialized = JsonConvert.SerializeObject(_cachedStore ?? Default, JsonSerializerSettings);
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
                store = JsonConvert.DeserializeObject<T>(settingsSerialized, JsonSerializerSettings);

                if (store is null)
                    TryBackup();
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
            store = JsonConvert.DeserializeObject<T>(settingsSerialized, JsonSerializerSettings);

            if (store is null)
                TryBackup();
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

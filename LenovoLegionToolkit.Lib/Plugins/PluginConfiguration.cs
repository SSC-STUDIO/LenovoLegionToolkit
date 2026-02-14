using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// 插件配置的默认实现
/// </summary>
public class PluginConfiguration : IPluginConfiguration
{
    private readonly string _pluginId;
    private readonly string _configFilePath;
    private Dictionary<string, object?> _configuration;
    private readonly object _lock = new();
    private bool _isDirty;

    public PluginConfiguration(string pluginId)
    {
        _pluginId = pluginId;
        _configuration = new Dictionary<string, object?>();
        
        var configDir = GetConfigDirectory();
        Directory.CreateDirectory(configDir);
        _configFilePath = Path.Combine(configDir, $"{pluginId}.json");
        
        LoadFromFile();
    }

    public T GetValue<T>(string key, T defaultValue = default!)
    {
        lock (_lock)
        {
            if (_configuration.TryGetValue(key, out var value))
            {
                if (value is JsonElement jsonElement)
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue;
                }
                if (value is T typedValue)
                {
                    return typedValue;
                }
                try
                {
                    var convertedValue = Convert.ChangeType(value, typeof(T));
                    return convertedValue != null ? (T)convertedValue : defaultValue;
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }

    public void SetValue<T>(string key, T value)
    {
        lock (_lock)
        {
            _configuration[key] = value;
            _isDirty = true;
        }
    }

    public bool HasKey(string key)
    {
        lock (_lock)
        {
            return _configuration.ContainsKey(key);
        }
    }

    public void RemoveKey(string key)
    {
        lock (_lock)
        {
            if (_configuration.Remove(key))
            {
                _isDirty = true;
            }
        }
    }

    public async Task SaveAsync()
    {
        lock (_lock)
        {
            if (!_isDirty) return;
        }

        try
        {
            var json = JsonSerializer.Serialize(_configuration, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(_configFilePath, json).ConfigureAwait(false);
            
            lock (_lock)
            {
                _isDirty = false;
            }
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugin configuration saved: {_pluginId}");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to save plugin configuration: {ex.Message}");
        }
    }

    public async Task ReloadAsync()
    {
        await Task.Run(() => LoadFromFile()).ConfigureAwait(false);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _configuration.Clear();
            _isDirty = true;
        }
    }

    private void LoadFromFile()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
                
                lock (_lock)
                {
                    _configuration = config ?? new Dictionary<string, object?>();
                }
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin configuration loaded: {_pluginId}");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load plugin configuration: {ex.Message}");
            
            lock (_lock)
            {
                _configuration = new Dictionary<string, object?>();
            }
        }
    }

    private static string GetConfigDirectory()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, "LenovoLegionToolkit", "plugin-config");
    }
}

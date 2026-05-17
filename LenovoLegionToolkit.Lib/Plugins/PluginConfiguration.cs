using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Plugins;

public class PluginConfiguration : IPluginConfiguration
{
    public const string ConfigurationRootEnvironmentVariable = "LLT_PLUGIN_CONFIG_ROOT";

    private readonly string _pluginId;
    private readonly string _configFilePath;
    private Dictionary<string, object?> _configuration = [];
    private readonly object _lock = new();
    private bool _isDirty;

    public PluginConfiguration(string pluginId)
    {
        _pluginId = pluginId;
        var configDir = GetConfigDirectory();
        Directory.CreateDirectory(configDir);
        _configFilePath = Path.Combine(configDir, $"{pluginId}.json");
        LoadFromFile();
    }

    public T GetValue<T>(string key, T defaultValue = default!)
    {
        lock (_lock)
        {
            if (!_configuration.TryGetValue(key, out var value))
                return defaultValue;

            if (value is JsonElement jsonElement)
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue;

            if (value is T typedValue)
                return typedValue;

            try
            {
                var convertedValue = Convert.ChangeType(value, typeof(T));
                return convertedValue is T converted ? converted : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
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
            return _configuration.ContainsKey(key);
    }

    public void RemoveKey(string key)
    {
        lock (_lock)
        {
            if (_configuration.Remove(key))
                _isDirty = true;
        }
    }

    public async Task SaveAsync()
    {
        lock (_lock)
        {
            if (!_isDirty)
                return;
        }

        var json = JsonSerializer.Serialize(_configuration, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_configFilePath, json).ConfigureAwait(false);

        lock (_lock)
            _isDirty = false;
    }

    public async Task ReloadAsync()
    {
        await Task.Run(LoadFromFile).ConfigureAwait(false);
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
            if (!File.Exists(_configFilePath))
            {
                lock (_lock)
                    _configuration = [];
                return;
            }

            var json = File.ReadAllText(_configFilePath);
            var configuration = JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? [];

            lock (_lock)
                _configuration = configuration;
        }
        catch
        {
            lock (_lock)
                _configuration = [];
        }
    }

    private static string GetConfigDirectory()
    {
        var configuredRoot = Environment.GetEnvironmentVariable(ConfigurationRootEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredRoot))
            return Path.Combine(configuredRoot, "plugin-config");

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, "LenovoLegionToolkit", "plugin-config");
    }
}

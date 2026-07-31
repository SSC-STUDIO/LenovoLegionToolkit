using System.Text.Json;
using System.Text.Json.Serialization;
using UniversalDeviceToolkit.Abstractions.Platform;

namespace UniversalDeviceToolkit.Platform.MacOS.Platform;

/// <summary>
/// macOS implementation of <see cref="IConfigurationStore"/>.
/// Persists configuration as a JSON file at <c>~/Library/Application Support/udt/config.json</c>.
/// </summary>
public sealed class MacOSConfigurationStore : IConfigurationStore
{
    private readonly string _configDir;
    private readonly string _configFile;
    private readonly object _syncLock = new();
    private Dictionary<string, Dictionary<string, string>> _data;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="MacOSConfigurationStore"/> class.
    /// </summary>
    public MacOSConfigurationStore()
    {
        _configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "udt");
        _configFile = Path.Combine(_configDir, "config.json");
        _data = LoadData();
    }

    /// <inheritdoc />
    public string? GetValue(string section, string key)
    {
        lock (_syncLock)
        {
            return _data.TryGetValue(section, out var sectionData)
                && sectionData.TryGetValue(key, out var value)
                ? value
                : null;
        }
    }

    /// <inheritdoc />
    public void SetValue(string section, string key, string? value)
    {
        lock (_syncLock)
        {
            if (!_data.TryGetValue(section, out var sectionData))
            {
                sectionData = new Dictionary<string, string>();
                _data[section] = sectionData;
            }

            if (value is null)
                sectionData.Remove(key);
            else
                sectionData[key] = value;

            SaveData();
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> GetSection(string section)
    {
        lock (_syncLock)
        {
            return _data.TryGetValue(section, out var sectionData)
                ? new Dictionary<string, string>(sectionData)
                : new Dictionary<string, string>();
        }
    }

    private Dictionary<string, Dictionary<string, string>> LoadData()
    {
        try
        {
            if (!File.Exists(_configFile))
                return new Dictionary<string, Dictionary<string, string>>();

            var json = File.ReadAllText(_configFile);
            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json, JsonOptions)
                   ?? new Dictionary<string, Dictionary<string, string>>();
        }
        catch
        {
            return new Dictionary<string, Dictionary<string, string>>();
        }
    }

    private void SaveData()
    {
        try
        {
            Directory.CreateDirectory(_configDir);
            var json = JsonSerializer.Serialize(_data, JsonOptions);
            File.WriteAllText(_configFile, json);
        }
        catch
        {
            // Silently ignore write failures (e.g. read-only filesystem)
        }
    }
}

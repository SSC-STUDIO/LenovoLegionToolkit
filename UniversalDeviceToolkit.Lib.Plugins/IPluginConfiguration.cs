using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Plugin configuration interface for plugin persistent configuration
/// </summary>
public interface IPluginConfiguration
{
    /// <summary>
    /// Gets a configuration value
    /// </summary>
    /// <typeparam name="T">Value type</typeparam>
    /// <param name="key">Configuration key</param>
    /// <param name="defaultValue">Default value</param>
    /// <returns>The configuration value</returns>
    T GetValue<T>(string key, T defaultValue = default!);
    
    /// <summary>
    /// Sets a configuration value
    /// </summary>
    /// <typeparam name="T">Value type</typeparam>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Configuration value</param>
    void SetValue<T>(string key, T value);
    
    /// <summary>
    /// Checks if a configuration key exists
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <returns>True if the key exists</returns>
    bool HasKey(string key);
    
    /// <summary>
    /// Removes a configuration key
    /// </summary>
    /// <param name="key">Configuration key</param>
    void RemoveKey(string key);
    
    /// <summary>
    /// Saves configuration to storage
    /// </summary>
    Task SaveAsync();
    
    /// <summary>
    /// Reloads configuration from storage
    /// </summary>
    Task ReloadAsync();
    
    /// <summary>
    /// Clears all configuration
    /// </summary>
    void Clear();
}

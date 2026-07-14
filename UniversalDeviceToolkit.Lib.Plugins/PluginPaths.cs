using System;
using System.IO;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Plugin path utility class providing unified plugin directory discovery and management
/// </summary>
public static class PluginPaths
{
    private static readonly string AppDataBaseDir = AppContext.BaseDirectory;
    
    /// <summary>
    /// Plugin directory name
    /// </summary>
    public const string PluginsDirectoryName = "plugins";
    
    /// <summary>
    /// Plugin metadata file name
    /// </summary>
    public const string PluginMetadataFileName = "Plugin.json";

    /// <summary>
    /// Gets the plugin root directory
    /// </summary>
    /// <returns>Plugin root directory path</returns>
    public static string GetPluginsDirectory()
    {
        return Folders.GetAppDataSubdirectory(PluginsDirectoryName);
    }

    /// <summary>
    /// Gets the directory for a specific plugin
    /// </summary>
    /// <param name="pluginId">Plugin ID</param>
    /// <returns>Plugin directory path</returns>
    public static string GetPluginDirectory(string pluginId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        return Path.Combine(GetPluginsDirectory(), pluginId);
    }

    /// <summary>
    /// Gets plugin directories for development environment
    /// </summary>
    /// <returns>List of development environment plugin directory paths</returns>
    public static string[] GetDevelopmentPluginsDirectories()
    {
        return new[]
        {
            Path.Combine(AppDataBaseDir, "Build", PluginsDirectoryName),
            Path.Combine(AppDataBaseDir, "..", "..", "..", "Build", PluginsDirectoryName),
            Path.Combine(AppDataBaseDir, "..", "..", "..", "..", "Build", PluginsDirectoryName),
            Path.Combine(AppDataBaseDir, PluginsDirectoryName),
        };
    }

    /// <summary>
    /// Gets all possible plugin directories
    /// </summary>
    /// <returns>List of plugin directory paths</returns>
    public static string[] GetAllPossiblePluginsDirectories()
    {
        var devDirs = GetDevelopmentPluginsDirectories();
        var appDataDir = GetPluginsDirectory();
        
        var result = new string[devDirs.Length + 1];
        devDirs.CopyTo(result, 0);
        result[^1] = appDataDir;
        
        return result;
    }

    /// <summary>
    /// Gets plugin assembly file paths
    /// </summary>
    /// <param name="pluginDirectory">Plugin directory</param>
    /// <returns>Array of DLL file paths</returns>
    public static string[] GetPluginAssemblyFiles(string pluginDirectory)
    {
        if (!Directory.Exists(pluginDirectory))
            return Array.Empty<string>();
        
        return Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
    }

    /// <summary>
    /// Gets the plugin metadata file path
    /// </summary>
    /// <param name="pluginDirectory">Plugin directory</param>
    /// <returns>Metadata file path</returns>
    public static string? GetPluginMetadataFilePath(string pluginDirectory)
    {
        var filePath = Path.Combine(pluginDirectory, PluginMetadataFileName);
        return File.Exists(filePath) ? filePath : null;
    }

    /// <summary>
    /// Checks if a directory contains a valid plugin
    /// </summary>
    /// <param name="directory">Directory path</param>
    /// <returns>True if the directory contains a plugin</returns>
    public static bool ContainsPlugin(string directory)
    {
        if (!Directory.Exists(directory))
            return false;
        
        var dllFiles = Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly);
        return dllFiles.Length > 0;
    }

    /// <summary>
    /// Gets the plugin resources directory
    /// </summary>
    /// <param name="pluginId">Plugin ID</param>
    /// <returns>Resource directory path</returns>
    public static string GetPluginResourcesDirectory(string pluginId)
    {
        return Path.Combine(GetPluginDirectory(pluginId), "Resources");
    }

    /// <summary>
    /// Gets the plugin configuration file path
    /// </summary>
    /// <param name="pluginId">Plugin ID</param>
    /// <returns>Configuration file path</returns>
    public static string GetPluginConfigFilePath(string pluginId)
    {
        return Path.Combine(GetPluginDirectory(pluginId), "config.json");
    }
}
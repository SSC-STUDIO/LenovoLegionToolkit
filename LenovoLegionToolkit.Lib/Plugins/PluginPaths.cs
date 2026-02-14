using System;
using System.IO;
using System.Reflection;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// 插件路径工具类，提供统一的插件目录发现和管理功能
/// </summary>
public static class PluginPaths
{
    private static readonly string AppDataBaseDir = AppContext.BaseDirectory;
    
    /// <summary>
    /// 插件目录名称
    /// </summary>
    public const string PluginsDirectoryName = "plugins";
    
    /// <summary>
    /// 插件元数据文件名
    /// </summary>
    public const string PluginMetadataFileName = "Plugin.json";

    /// <summary>
    /// 获取插件根目录
    /// </summary>
    /// <returns>插件根目录路径</returns>
    public static string GetPluginsDirectory()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var pluginsPath = Path.Combine(appDataPath, "LenovoLegionToolkit", PluginsDirectoryName);
        
        if (!Directory.Exists(pluginsPath))
        {
            try
            {
                Directory.CreateDirectory(pluginsPath);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to create plugins directory: {ex.Message}");
            }
        }
        
        return pluginsPath;
    }

    /// <summary>
    /// 获取特定插件的目录
    /// </summary>
    /// <param name="pluginId">插件ID</param>
    /// <returns>插件目录路径</returns>
    public static string GetPluginDirectory(string pluginId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        return Path.Combine(GetPluginsDirectory(), pluginId);
    }

    /// <summary>
    /// 获取开发环境的插件目录
    /// </summary>
    /// <returns>开发环境插件目录路径列表</returns>
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
    /// 获取所有可能的插件目录
    /// </summary>
    /// <returns>插件目录路径列表</returns>
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
    /// 获取插件程序集文件路径
    /// </summary>
    /// <param name="pluginDirectory">插件目录</param>
    /// <returns>DLL文件路径数组</returns>
    public static string[] GetPluginAssemblyFiles(string pluginDirectory)
    {
        if (!Directory.Exists(pluginDirectory))
            return Array.Empty<string>();
        
        return Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
    }

    /// <summary>
    /// 获取插件元数据文件路径
    /// </summary>
    /// <param name="pluginDirectory">插件目录</param>
    /// <returns>元数据文件路径</returns>
    public static string? GetPluginMetadataFilePath(string pluginDirectory)
    {
        var filePath = Path.Combine(pluginDirectory, PluginMetadataFileName);
        return File.Exists(filePath) ? filePath : null;
    }

    /// <summary>
    /// 检查目录是否包含有效插件
    /// </summary>
    /// <param name="directory">目录路径</param>
    /// <returns>如果包含插件则返回true</returns>
    public static bool ContainsPlugin(string directory)
    {
        if (!Directory.Exists(directory))
            return false;
        
        var dllFiles = Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly);
        return dllFiles.Length > 0;
    }

    /// <summary>
    /// 获取插件资源目录
    /// </summary>
    /// <param name="pluginId">插件ID</param>
    /// <returns>资源目录路径</returns>
    public static string GetPluginResourcesDirectory(string pluginId)
    {
        return Path.Combine(GetPluginDirectory(pluginId), "Resources");
    }

    /// <summary>
    /// 获取插件配置文件路径
    /// </summary>
    /// <param name="pluginId">插件ID</param>
    /// <returns>配置文件路径</returns>
    public static string GetPluginConfigFilePath(string pluginId)
    {
        return Path.Combine(GetPluginDirectory(pluginId), "config.json");
    }
}

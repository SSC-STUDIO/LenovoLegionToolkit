using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Plugin manager implementation
/// </summary>
public class PluginManager : IPluginManager
{
    private readonly ApplicationSettings _applicationSettings;
    private readonly Dictionary<string, IPlugin> _registeredPlugins = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PluginMetadata> _pluginMetadataCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _pluginFileCache = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<PluginEventArgs>? PluginStateChanged;

    public PluginManager(ApplicationSettings applicationSettings)
    {
        _applicationSettings = applicationSettings;
    }

    /// <summary>
    /// Scan and load plugins from the plugins directory
    /// </summary>
    public void ScanAndLoadPlugins()
    {
        try
        {
            // Get the plugins directory path
            var pluginsDirectory = GetPluginsDirectory();
            
            if (!Directory.Exists(pluginsDirectory))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugins directory does not exist: {pluginsDirectory}");
                return;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Scanning plugins directory: {pluginsDirectory}");

            // Register assembly resolver to handle plugin dependencies and satellite assemblies
            // This ensures that when loading a plugin, its dependencies (like Plugins.SDK, Lib, WPF) and
            // satellite assemblies (localized resources) can be found
            ResolveEventHandler assemblyResolver = (sender, args) =>
            {
                try
                {
                    var requestedAssemblyName = new AssemblyName(args.Name);
                    var assemblyName = requestedAssemblyName.Name;
                    if (string.IsNullOrWhiteSpace(assemblyName))
                        return null;
                    
                    // Check if this is a satellite assembly request (resources.dll)
                    // ResourceManager requests satellite assemblies with format: "AssemblyName.resources, Culture=zh-hans, ..."
                    var isSatelliteAssembly = requestedAssemblyName.CultureInfo != null && 
                                              !string.IsNullOrEmpty(requestedAssemblyName.CultureInfo.Name) &&
                                              assemblyName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase);
                    
                    if (isSatelliteAssembly)
                    {
                        // For satellite assemblies, extract culture and base assembly name
                        var cultureName = requestedAssemblyName.CultureInfo!.Name;
                        // Remove ".resources" suffix to get base assembly name
                        var baseName = assemblyName.Substring(0, assemblyName.Length - ".resources".Length);
                        
                        if (Directory.Exists(pluginsDirectory))
                        {
                            var subdirectories = Directory.GetDirectories(pluginsDirectory);
                            var cultureFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                                "ar", "bg", "bs", "ca", "cs", "de", "el", "es", "fr", "hu", "it", "ja", "ko",
                                "lv", "nl-nl", "pl", "pt", "pt-br", "ro", "ru", "sk", "tr", "uk", "uz-latn-uz",
                                "vi", "zh-hans", "zh-hant", "tools"
                            };
                            
                            foreach (var subdir in subdirectories)
                            {
                                // Skip culture folders
                                var dirName = Path.GetFileName(subdir);
                                if (cultureFolders.Contains(dirName))
                                    continue;
                                
                                // Check for satellite assembly in culture subfolder of plugin directory
                                // Format: pluginDir/cultureName/AssemblyName.resources.dll
                                var satellitePath = Path.Combine(subdir, cultureName, $"{baseName}.resources.dll");
                                if (File.Exists(satellitePath))
                                {
                                    if (Log.Instance.IsTraceEnabled)
                                        Log.Instance.Trace($"Resolving satellite assembly: {args.Name} from {satellitePath}");
                                    return Assembly.LoadFrom(satellitePath);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Regular assembly resolution for dependencies
                        // Check in plugins directory first (root level)
                        var dllPath = Path.Combine(pluginsDirectory, $"{assemblyName}.dll");
                        if (File.Exists(dllPath))
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Resolving assembly dependency: {assemblyName} from {dllPath}");
                            return Assembly.LoadFrom(dllPath);
                        }
                        
                        // Also check in plugin subdirectories
                        if (Directory.Exists(pluginsDirectory))
                        {
                            var subdirectories = Directory.GetDirectories(pluginsDirectory);
                            foreach (var subdir in subdirectories)
                            {
                                var subDirDllPath = Path.Combine(subdir, $"{assemblyName}.dll");
                                if (File.Exists(subDirDllPath))
                                {
                                    if (Log.Instance.IsTraceEnabled)
                                        Log.Instance.Trace($"Resolving assembly dependency: {assemblyName} from {subDirDllPath}");
                                    return Assembly.LoadFrom(subDirDllPath);
                                }
                            }
                        }
                        
                        // Also check in the application base directory
                        var appBaseDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{assemblyName}.dll");
                        if (File.Exists(appBaseDllPath))
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Resolving assembly dependency: {assemblyName} from {appBaseDllPath}");
                            return Assembly.LoadFrom(appBaseDllPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to resolve assembly: {args.Name}, {ex.Message}", ex);
                }
                return null;
            };
            
            AppDomain.CurrentDomain.AssemblyResolve += assemblyResolver;

            // Scan for plugin DLLs in subdirectories (each plugin in its own folder)
            // Also check the root plugins directory for backward compatibility
            var allDllFiles = new List<string>();
            
            // First, check subdirectories (each plugin should be in its own folder)
            // Skip resource directories (culture folders like zh-hans, en, etc.)
            if (Directory.Exists(pluginsDirectory))
            {
                var subdirectories = Directory.GetDirectories(pluginsDirectory);
                var cultureFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "ar", "bg", "bs", "ca", "cs", "de", "el", "es", "fr", "hu", "it", "ja", "ko",
                    "lv", "nl-nl", "pl", "pt", "pt-br", "ro", "ru", "sk", "tr", "uk", "uz-latn-uz",
                    "vi", "zh-hans", "zh-hant", "tools"
                };
                
                foreach (var subdir in subdirectories)
                {
                    var dirName = Path.GetFileName(subdir);
                    // Skip culture folders and known non-plugin folders
                    if (cultureFolders.Contains(dirName) || dirName.Equals("LenovoLegionToolkit.Plugins.SDK", StringComparison.OrdinalIgnoreCase))
                        continue;
                        
                    var dllFiles = Directory.GetFiles(subdir, "*.dll", SearchOption.TopDirectoryOnly);
                    allDllFiles.AddRange(dllFiles);
                }
                
                // Also check the root plugins directory for backward compatibility
                var rootDllFiles = Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly);
                allDllFiles.AddRange(rootDllFiles);
            }
            
            // Filter to only plugin DLLs (exclude main application DLLs, SDK DLL, and resource DLLs)
            var pluginFiles = allDllFiles
                .Where(f => 
                {
                    var fileName = Path.GetFileName(f);
                    var fileInfo = new FileInfo(f);
                    
                    // Check cache to avoid reloading unchanged files
                    if (_pluginFileCache.TryGetValue(fileName, out var cachedTime))
                    {
                        if (fileInfo.LastWriteTime <= cachedTime)
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Skipping unchanged plugin: {fileName}");
                            return false;
                        }
                    }
                    
                    // Only include plugin DLLs (must start with "LenovoLegionToolkit.Plugins.")
                    // Exclude SDK DLL itself (it's a dependency, not a plugin)
                    // Exclude resource DLLs
                    return fileName.StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.OrdinalIgnoreCase) &&
                           !fileName.Equals("LenovoLegionToolkit.Plugins.SDK.dll", StringComparison.OrdinalIgnoreCase) &&
                           !fileName.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase);
                })
                .Distinct() // Remove duplicates if a plugin exists in both root and subdirectory
                .ToList();
            
            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"Found {allDllFiles.Count} total DLL file(s) in {pluginsDirectory}");
                Log.Instance.Trace($"Filtered to {pluginFiles.Count} plugin DLL file(s)");
                foreach (var pluginFile in pluginFiles)
                {
                    Log.Instance.Trace($"  - {Path.GetFileName(pluginFile)}");
                }
            }

            foreach (var pluginFile in pluginFiles)
            {
                try
                {
                    LoadPluginFromFile(pluginFile);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to load plugin from {pluginFile}: {ex.Message}", ex);
                }
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugin scan completed. Total registered plugins: {_registeredPlugins.Count}");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error scanning plugins directory: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get the plugins directory path
    /// </summary>
    private string GetPluginsDirectory()
    {
        // Try to find the plugins directory relative to the application base directory
        var appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
        
        // Check for build/plugins directory (development/build scenario)
        // Try multiple relative paths to handle different build configurations
        var possiblePaths = new[]
        {
            Path.Combine(appBaseDir, "build", "plugins"),  // Direct build/plugins
            Path.Combine(appBaseDir, "..", "..", "..", "build", "plugins"),  // Relative to bin
            Path.Combine(appBaseDir, "..", "build", "plugins"),  // One level up
            Path.Combine(appBaseDir, "Plugins"),  // Same directory as executable (release, legacy)
            Path.Combine(appBaseDir, "plugins"),  // Same directory as executable (release)
        };

        foreach (var possiblePath in possiblePaths)
        {
            var fullPath = Path.GetFullPath(possiblePath);
            if (Directory.Exists(fullPath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Found plugins directory: {fullPath}");
                return fullPath;
            }
        }

        // Default to build/plugins relative to app base directory (will be created if needed)
        var defaultPath = Path.Combine(appBaseDir, "build", "plugins");
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Using default plugins directory: {defaultPath}");
        return defaultPath;
    }

    /// <summary>
    /// Load a plugin from a DLL file
    /// </summary>
    private void LoadPluginFromFile(string pluginFilePath)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Attempting to load plugin from: {pluginFilePath}");

            // Load the assembly with reflection-only context first to check for types
            Assembly? assembly = null;
            try
            {
                assembly = Assembly.LoadFrom(pluginFilePath);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to load assembly from {pluginFilePath}: {ex.Message}", ex);
                return;
            }

            // Find all types that implement IPlugin
            Type[] pluginTypes;
            try
            {
                pluginTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                if (Log.Instance.IsTraceEnabled)
                {
                    Log.Instance.Trace($"Failed to get types from assembly {pluginFilePath}. Loader exceptions:", ex);
                    if (ex.LoaderExceptions != null)
                    {
                        foreach (var loaderEx in ex.LoaderExceptions)
                        {
                            Log.Instance.Trace($"  Loader exception: {loaderEx?.Message}", loaderEx);
                        }
                    }
                }
                // Try to continue with successfully loaded types
                pluginTypes = ex.Types.Where(t => t != null).OfType<Type>().ToArray();
            }

            var validPluginTypes = pluginTypes
                .Where(t => t != null && typeof(IPlugin).IsAssignableFrom(t) 
                    && !t.IsInterface 
                    && !t.IsAbstract
                    && t.GetConstructor(Type.EmptyTypes) != null)
                .ToList();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Found {validPluginTypes.Count} plugin type(s) in {pluginFilePath}");

            foreach (var pluginType in validPluginTypes)
            {
                try
                {
                    // Try to get PluginAttribute for version and compatibility info
                    string pluginVersion = "1.0.0";
                    string minimumHostVersion = "1.0.0";
                    var pluginAttribute = pluginType.GetCustomAttributes(true)
                        .FirstOrDefault(attr => attr.GetType().Name == "PluginAttribute");
                    
                    if (pluginAttribute != null)
                    {
                        // Use reflection to get version and MinimumHostVersion from attribute
                        var versionProp = pluginAttribute.GetType().GetProperty("Version");
                        var minHostVersionProp = pluginAttribute.GetType().GetProperty("MinimumHostVersion");
                        
                        if (versionProp != null)
                        {
                            pluginVersion = versionProp.GetValue(pluginAttribute)?.ToString() ?? "1.0.0";
                        }
                        
                        if (minHostVersionProp != null)
                        {
                            minimumHostVersion = minHostVersionProp.GetValue(pluginAttribute)?.ToString() ?? "1.0.0";
                        }
                    }
                    
                    // Create an instance of the plugin
                    if (Activator.CreateInstance(pluginType) is IPlugin plugin)
                    {
                        // Check if this plugin has the GetFeatureExtension method (SDK plugin)
                        var hasGetFeatureExtension = pluginType.GetMethod("GetFeatureExtension", 
                            BindingFlags.Public | BindingFlags.Instance, 
                            null, 
                            Type.EmptyTypes, 
                            null) != null;

                        // Store plugin metadata with version and compatibility info
                        var metadata = new PluginMetadata
                        {
                            Id = plugin.Id,
                            Name = plugin.Name,
                            Description = plugin.Description,
                            Icon = plugin.Icon,
                            IsSystemPlugin = plugin.IsSystemPlugin,
                            Dependencies = plugin.Dependencies,
                            Version = pluginVersion,
                            MinimumHostVersion = minimumHostVersion
                        };
                        
                        // Try to get Author from attribute
                        if (pluginAttribute != null)
                        {
                            var authorProp = pluginAttribute.GetType().GetProperty("Author");
                            if (authorProp != null)
                            {
                                metadata.Author = authorProp.GetValue(pluginAttribute)?.ToString();
                            }
                        }
                        
                        _pluginMetadataCache[plugin.Id] = metadata;

                        // Register the plugin (both SDK and direct IPlugin implementations)
                        RegisterPlugin(plugin);
                        
                        if (Log.Instance.IsTraceEnabled)
                        {
                            var pluginTypeInfo = hasGetFeatureExtension ? "SDK" : "direct";
                            Log.Instance.Trace($"Successfully loaded {pluginTypeInfo} plugin: {plugin.Id} ({plugin.Name}) v{pluginVersion} (MinHost: {minimumHostVersion}) from {pluginFilePath}");
                        }
                    }
                    else
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Failed to create plugin instance from type {pluginType.Name}: Activator.CreateInstance returned null");
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to create instance of plugin type {pluginType.Name}: {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load plugin assembly from {pluginFilePath}: {ex.Message}", ex);
        }
    }

    public void RegisterPlugin(IPlugin plugin)
    {
        if (string.IsNullOrWhiteSpace(plugin.Id))
            throw new ArgumentException("Plugin ID cannot be null or empty", nameof(plugin));

        if (_registeredPlugins.ContainsKey(plugin.Id))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugin {plugin.Id} is already registered. Replacing existing registration.");
        }

        _registeredPlugins[plugin.Id] = plugin;
    }

    public IEnumerable<IPlugin> GetRegisteredPlugins()
    {
        return _registeredPlugins.Values;
    }

    public PluginMetadata? GetPluginMetadata(string pluginId)
    {
        // First try to get from cache (contains version info)
        if (_pluginMetadataCache.TryGetValue(pluginId, out var cachedMetadata))
        {
            return cachedMetadata;
        }
        
        // Fallback to creating from plugin instance
        if (!_registeredPlugins.TryGetValue(pluginId, out var plugin))
            return null;

        return new PluginMetadata
        {
            Id = plugin.Id,
            Name = plugin.Name,
            Description = plugin.Description,
            Icon = plugin.Icon,
            IsSystemPlugin = plugin.IsSystemPlugin,
            Dependencies = plugin.Dependencies,
            Version = "1.0.0",
            MinimumHostVersion = "1.0.0"
        };
    }

    public bool IsInstalled(string pluginId)
    {
        return _applicationSettings.Store.InstalledExtensions.Contains(pluginId, StringComparer.OrdinalIgnoreCase);
    }

    public void InstallPlugin(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return;

        var installedExtensions = _applicationSettings.Store.InstalledExtensions;

        if (!installedExtensions.Contains(pluginId, StringComparer.OrdinalIgnoreCase))
        {
            installedExtensions.Add(pluginId);
            _applicationSettings.SynchronizeStore();

            // Check and install dependencies
            if (_registeredPlugins.TryGetValue(pluginId, out var plugin) && plugin.Dependencies != null)
            {
                foreach (var dependency in plugin.Dependencies)
                {
                    if (!IsInstalled(dependency))
                    {
                        InstallPlugin(dependency);
                    }
                }
            }

            // Trigger install callback
            if (_registeredPlugins.TryGetValue(pluginId, out var installedPlugin))
            {
                installedPlugin.OnInstalled();
            }

            OnPluginStateChanged(pluginId, true);
        }
    }

    public bool UninstallPlugin(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        var installedExtensions = _applicationSettings.Store.InstalledExtensions;

        if (!installedExtensions.Contains(pluginId, StringComparer.OrdinalIgnoreCase))
            return false;

        // Get plugin instance
        _registeredPlugins.TryGetValue(pluginId, out var plugin);

        // 检查是否有其他插件依赖此插件
        var dependentPlugins = _registeredPlugins.Values
            .Where(p => p.Dependencies != null && p.Dependencies.Contains(pluginId, StringComparer.OrdinalIgnoreCase))
            .Where(p => IsInstalled(p.Id))
            .ToList();

        if (dependentPlugins.Any())
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Cannot uninstall plugin {pluginId} because it is a dependency of other installed plugins.");
            return false;
        }

        installedExtensions.Remove(pluginId);
        
        // Add to pending deletion list for actual deletion on app exit
        if (!_applicationSettings.Store.PendingDeletionExtensions.Contains(pluginId, StringComparer.OrdinalIgnoreCase))
        {
            _applicationSettings.Store.PendingDeletionExtensions.Add(pluginId);
        }
        
        _applicationSettings.SynchronizeStore();

        // 触发卸载回调
        plugin?.OnUninstalled();

        OnPluginStateChanged(pluginId, false);

        return true;
    }

    public IEnumerable<string> GetInstalledPluginIds()
    {
        return _applicationSettings.Store.InstalledExtensions;
    }

    public bool PermanentlyDeletePlugin(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        try
        {
            // Remove from registered plugins and metadata cache
            _registeredPlugins.Remove(pluginId);
            _pluginMetadataCache.Remove(pluginId);

            // Get plugins directory
            var pluginsDirectory = GetPluginsDirectory();
            if (!Directory.Exists(pluginsDirectory))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugins directory does not exist: {pluginsDirectory}");
                return false;
            }

            // Try to find plugin file by scanning all plugin DLLs and matching by ID
            // We need to check both the expected naming pattern and scan for actual plugin IDs
            var foundFiles = new List<string>();
            var pluginDirectoryToDelete = new List<string>();

            // Check subdirectories (plugins are often in their own folder)
            var subdirectories = Directory.GetDirectories(pluginsDirectory);
            var cultureFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ar", "bg", "bs", "ca", "cs", "de", "el", "es", "fr", "hu", "it", "ja", "ko",
                "lv", "nl-nl", "pl", "pt", "pt-br", "ro", "ru", "sk", "tr", "uk", "uz-latn-uz",
                "vi", "zh-hans", "zh-hant", "tools"
            };

            foreach (var subdir in subdirectories)
            {
                var dirName = Path.GetFileName(subdir);
                if (cultureFolders.Contains(dirName))
                    continue;

                // Check all DLL files in this subdirectory
                var dllFiles = Directory.GetFiles(subdir, "*.dll", SearchOption.TopDirectoryOnly)
                    .Where(f => 
                    {
                        var fileName = Path.GetFileName(f);
                        return fileName.StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.OrdinalIgnoreCase) &&
                               !fileName.Equals("LenovoLegionToolkit.Plugins.SDK.dll", StringComparison.OrdinalIgnoreCase) &&
                               !fileName.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase);
                    });

                foreach (var dllFile in dllFiles)
                {
                    try
                    {
                        // Try to load the assembly and check if it contains our plugin
                        var assembly = Assembly.LoadFrom(dllFile);
                        var pluginTypes = assembly.GetTypes()
                            .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                        foreach (var pluginType in pluginTypes)
                        {
                            if (Activator.CreateInstance(pluginType) is IPlugin testPlugin && 
                                testPlugin.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase))
                            {
                                foundFiles.Add(dllFile);
                                pluginDirectoryToDelete.Add(subdir);
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // If we can't load it, try matching by filename pattern
                        var fileName = Path.GetFileNameWithoutExtension(dllFile);
                        if (fileName.EndsWith($".{pluginId}", StringComparison.OrdinalIgnoreCase) ||
                            fileName.Equals($"LenovoLegionToolkit.Plugins.{pluginId}", StringComparison.OrdinalIgnoreCase))
                        {
                            foundFiles.Add(dllFile);
                            pluginDirectoryToDelete.Add(subdir);
                        }
                    }
                }
            }

            // Also check root plugins directory
            var rootDllFiles = Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                .Where(f => 
                {
                    var fileName = Path.GetFileName(f);
                    return fileName.StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.OrdinalIgnoreCase) &&
                           !fileName.Equals("LenovoLegionToolkit.Plugins.SDK.dll", StringComparison.OrdinalIgnoreCase) &&
                           !fileName.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase);
                });

            foreach (var dllFile in rootDllFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dllFile);
                    var pluginTypes = assembly.GetTypes()
                        .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var pluginType in pluginTypes)
                    {
                        if (Activator.CreateInstance(pluginType) is IPlugin testPlugin && 
                            testPlugin.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase))
                        {
                            foundFiles.Add(dllFile);
                            break;
                        }
                    }
                }
                catch
                {
                    var fileName = Path.GetFileNameWithoutExtension(dllFile);
                    if (fileName.EndsWith($".{pluginId}", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals($"LenovoLegionToolkit.Plugins.{pluginId}", StringComparison.OrdinalIgnoreCase))
                    {
                        foundFiles.Add(dllFile);
                    }
                }
            }

            // Delete plugin directories (if entire directory contains only this plugin)
            foreach (var dir in pluginDirectoryToDelete.Distinct())
            {
                try
                {
                    // Check if directory only contains files related to this plugin
                    var allFiles = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
                        .Where(f => !f.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    var pluginBaseName = Path.GetFileNameWithoutExtension(foundFiles.FirstOrDefault(f => f.StartsWith(dir)) ?? "");
                    if (!string.IsNullOrEmpty(pluginBaseName) && 
                        allFiles.All(f => Path.GetFileName(f).StartsWith(pluginBaseName, StringComparison.OrdinalIgnoreCase)))
                    {
                        Directory.Delete(dir, true);
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Deleted plugin directory: {dir}");
                        // Remove files from foundFiles list since directory is deleted
                        foundFiles.RemoveAll(f => f.StartsWith(dir));
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to delete plugin directory {dir}: {ex.Message}", ex);
                }
            }

            // Delete all found plugin files
            var deletedAny = false;
            foreach (var filePath in foundFiles)
            {
                try
                {
                    File.Delete(filePath);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Deleted plugin file: {filePath}");
                    deletedAny = true;

                    // Also delete related files (pdb, deps.json, etc.)
                    var basePath = Path.ChangeExtension(filePath, null);
                    var dir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        var relatedExtensions = new[] { ".pdb", ".deps.json", ".config" };
                        foreach (var ext in relatedExtensions)
                        {
                            var relatedFile = basePath + ext;
                            if (File.Exists(relatedFile))
                            {
                                try
                                {
                                    File.Delete(relatedFile);
                                    if (Log.Instance.IsTraceEnabled)
                                        Log.Instance.Trace($"Deleted related file: {relatedFile}");
                                }
                                catch (Exception ex)
                                {
                                    if (Log.Instance.IsTraceEnabled)
                                        Log.Instance.Trace($"Failed to delete related file {relatedFile}: {ex.Message}", ex);
                                }
                            }
                        }

                        // Also delete satellite assemblies (resource DLLs)
                        var cultureDirs = Directory.GetDirectories(dir);
                        foreach (var cultureDir in cultureDirs)
                        {
                            var cultureName = Path.GetFileName(cultureDir);
                            if (cultureFolders.Contains(cultureName))
                            {
                                var satelliteFiles = Directory.GetFiles(cultureDir, "*.*.resources.dll");
                                var pluginBaseName = Path.GetFileNameWithoutExtension(filePath);
                                foreach (var satelliteFile in satelliteFiles)
                                {
                                    var satelliteFileName = Path.GetFileNameWithoutExtension(satelliteFile);
                                    if (satelliteFileName.StartsWith(pluginBaseName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            File.Delete(satelliteFile);
                                            if (Log.Instance.IsTraceEnabled)
                                                Log.Instance.Trace($"Deleted satellite assembly: {satelliteFile}");
                                        }
                                        catch (Exception ex)
                                        {
                                            if (Log.Instance.IsTraceEnabled)
                                                Log.Instance.Trace($"Failed to delete satellite assembly {satelliteFile}: {ex.Message}", ex);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to delete plugin file {filePath}: {ex.Message}", ex);
                }
            }

            return deletedAny;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error permanently deleting plugin {pluginId}: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Delete a file with retry mechanism to handle locked files
    /// </summary>
    private bool DeleteFileWithRetry(string filePath, int maxRetries = 10, int delayMs = 200)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return true;
            }
            catch (IOException)
            {
                if (i == maxRetries - 1)
                    return false;
                global::System.Threading.Thread.Sleep(delayMs);
            }
            catch (UnauthorizedAccessException)
            {
                if (i == maxRetries - 1)
                    return false;
                global::System.Threading.Thread.Sleep(delayMs);
            }
        }
        return false;
    }

    /// <summary>
    /// Delete a directory with retry mechanism to handle locked files
    /// </summary>
    private bool DeleteDirectoryWithRetry(string directoryPath, int maxRetries = 10, int delayMs = 200)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                    return true;
                }
                return true;
            }
            catch (IOException)
            {
                if (i == maxRetries - 1)
                    return false;
                global::System.Threading.Thread.Sleep(delayMs);
            }
            catch (UnauthorizedAccessException)
            {
                if (i == maxRetries - 1)
                    return false;
                global::System.Threading.Thread.Sleep(delayMs);
            }
        }
        return false;
    }

    /// <summary>
    /// Perform actual deletion of plugins marked for deletion (call on app exit)
    /// </summary>
    public void PerformPendingDeletions()
    {
        var pendingDeletions = _applicationSettings.Store.PendingDeletionExtensions.ToList();
        
        if (!pendingDeletions.Any())
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"No pending plugin deletions to process.");
            return;
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Processing {pendingDeletions.Count} pending plugin deletion(s).");

        foreach (var pluginId in pendingDeletions)
        {
            try
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Deleting plugin files for: {pluginId}");
                
                PermanentlyDeletePlugin(pluginId);
                
                // Remove from pending deletions list
                _applicationSettings.Store.PendingDeletionExtensions.Remove(pluginId);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to delete plugin {pluginId}: {ex.Message}", ex);
            }
        }

        // Save settings after processing all deletions
        _applicationSettings.SynchronizeStore();

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Pending plugin deletions completed.");
    }

    protected virtual void OnPluginStateChanged(string pluginId, bool isInstalled)
    {
        PluginStateChanged?.Invoke(this, new PluginEventArgs(pluginId, isInstalled));
    }
}


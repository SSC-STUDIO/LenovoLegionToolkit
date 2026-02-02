using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
    private ResolveEventHandler? _assemblyResolveHandler;
    private bool _disposed;

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
            var pluginsDirectory = GetPluginsDirectory();
            if (!Directory.Exists(pluginsDirectory))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugins directory does not exist: {pluginsDirectory}");
                return;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Scanning plugins directory: {pluginsDirectory}");

            RegisterAssemblyResolver(pluginsDirectory);

            var pluginFiles = GetPluginDllFiles(pluginsDirectory);
            
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

    private void RegisterAssemblyResolver(string pluginsDirectory)
    {
        if (_assemblyResolveHandler != null)
            return;

        _assemblyResolveHandler = (sender, args) =>
        {
            try
            {
                var requestedAssemblyName = new AssemblyName(args.Name);
                var assemblyName = requestedAssemblyName.Name;
                if (string.IsNullOrWhiteSpace(assemblyName))
                    return null;
                
                var isSatelliteAssembly = requestedAssemblyName.CultureInfo != null && 
                                          !string.IsNullOrEmpty(requestedAssemblyName.CultureInfo.Name) &&
                                          assemblyName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase);
                
                if (isSatelliteAssembly)
                {
                    return ResolveSatelliteAssembly(pluginsDirectory, requestedAssemblyName, assemblyName);
                }
                
                return ResolveDependencyAssembly(pluginsDirectory, assemblyName);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to resolve assembly: {args.Name}, {ex.Message}", ex);
            }
            return null;
        };

        AppDomain.CurrentDomain.AssemblyResolve += _assemblyResolveHandler;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (_assemblyResolveHandler != null)
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= _assemblyResolveHandler;
                    _assemblyResolveHandler = null;
                }
            }
            _disposed = true;
        }
    }

    private Assembly? ResolveSatelliteAssembly(string pluginsDirectory, AssemblyName requestedAssemblyName, string assemblyName)
    {
        var cultureName = requestedAssemblyName.CultureInfo!.Name;
        var baseName = assemblyName.Substring(0, assemblyName.Length - ".resources".Length);
        
        var subdirectories = Directory.GetDirectories(pluginsDirectory);
        var cultureFolders = GetCultureFolders();
        
        foreach (var subdir in subdirectories)
        {
            var dirName = Path.GetFileName(subdir);
            if (cultureFolders.Contains(dirName))
                continue;
            
            var satellitePath = Path.Combine(subdir, cultureName, $"{baseName}.resources.dll");
            if (File.Exists(satellitePath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Resolving satellite assembly: {requestedAssemblyName.Name} from {satellitePath}");
                return Assembly.LoadFrom(satellitePath);
            }
        }
        return null;
    }

    private Assembly? ResolveDependencyAssembly(string pluginsDirectory, string assemblyName)
    {
        var dllPath = Path.Combine(pluginsDirectory, $"{assemblyName}.dll");
        if (File.Exists(dllPath))
            return Assembly.LoadFrom(dllPath);
        
        var subdirectories = Directory.GetDirectories(pluginsDirectory);
        foreach (var subdir in subdirectories)
        {
            var subDirDllPath = Path.Combine(subdir, $"{assemblyName}.dll");
            if (File.Exists(subDirDllPath))
                return Assembly.LoadFrom(subDirDllPath);
        }
        
        var appBaseDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{assemblyName}.dll");
        if (File.Exists(appBaseDllPath))
            return Assembly.LoadFrom(appBaseDllPath);

        return null;
    }

    private List<string> GetPluginDllFiles(string pluginsDirectory)
    {
        var allDllFiles = new List<string>();
        var subdirectories = Directory.GetDirectories(pluginsDirectory);
        var cultureFolders = GetCultureFolders();
        
        foreach (var subdir in subdirectories)
        {
            var dirName = Path.GetFileName(subdir);
            if (cultureFolders.Contains(dirName) || dirName.Equals("LenovoLegionToolkit.Plugins.SDK", StringComparison.OrdinalIgnoreCase))
                continue;

            // If this is the "local" directory, scan its subdirectories
            if (dirName.Equals("local", StringComparison.OrdinalIgnoreCase))
            {
                var localSubDirs = Directory.GetDirectories(subdir);
                foreach (var localSubDir in localSubDirs)
                {
                    allDllFiles.AddRange(Directory.GetFiles(localSubDir, "*.dll", SearchOption.TopDirectoryOnly));
                }
                continue;
            }
                
            allDllFiles.AddRange(Directory.GetFiles(subdir, "*.dll", SearchOption.TopDirectoryOnly));
        }
        
        allDllFiles.AddRange(Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly));
        
        return allDllFiles
            .Where(IsPluginDll)
            .Distinct()
            .ToList();
    }

    private bool IsPluginDll(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var fileInfo = new FileInfo(filePath);
        
        if (_pluginFileCache.TryGetValue(fileName, out var cachedTime) && fileInfo.LastWriteTime <= cachedTime)
            return false;
        
        return fileName.StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.OrdinalIgnoreCase) &&
               !fileName.Equals("LenovoLegionToolkit.Plugins.SDK.dll", StringComparison.OrdinalIgnoreCase) &&
               !fileName.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase);
    }

    private HashSet<string> GetCultureFolders() => new(StringComparer.OrdinalIgnoreCase)
    {
        "ar", "bg", "bs", "ca", "cs", "de", "el", "es", "fr", "hu", "it", "ja", "ko",
        "lv", "nl-nl", "pl", "pt", "pt-br", "ro", "ru", "sk", "tr", "uk", "uz-latn-uz",
        "vi", "zh-hans", "zh-hant", "tools"
    };

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
                            MinimumHostVersion = minimumHostVersion,
                            FilePath = pluginFilePath
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

    /// <inheritdoc />
    public bool TryGetPlugin(string pluginId, out IPlugin? plugin)
    {
        return _registeredPlugins.TryGetValue(pluginId, out plugin);
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
        var isInstalled = _applicationSettings.Store.InstalledExtensions.Contains(pluginId, StringComparer.OrdinalIgnoreCase);
        
        if (Log.Instance.IsTraceEnabled)
        {
            Log.Instance.Trace($"IsInstalled({pluginId}) = {isInstalled} (from settings)");
            Log.Instance.Trace($"  - Installed extensions count: {_applicationSettings.Store.InstalledExtensions.Count}");
            Log.Instance.Trace($"  - Installed extensions: [{string.Join(", ", _applicationSettings.Store.InstalledExtensions)}]");
        }
        
        // If not in installed list, definitely not installed
        if (!isInstalled)
            return false;
            
        // Check if plugin files actually exist on disk
        // This prevents showing "installed" state when files have been deleted manually
        try
        {
            var pluginsDirectory = GetPluginsDirectory();
            var pluginDirectory = Path.Combine(pluginsDirectory, pluginId);
            var localPluginDirectory = Path.Combine(pluginsDirectory, "local", pluginId);
            
            // Check for alternative directory naming (LenovoLegionToolkit.Plugins.{Id})
            if (!Directory.Exists(pluginDirectory) && !Directory.Exists(localPluginDirectory))
            {
                var altPluginDirectory = Path.Combine(pluginsDirectory, $"LenovoLegionToolkit.Plugins.{pluginId}");
                var altPluginDirectoryNoHyphen = Path.Combine(pluginsDirectory, $"LenovoLegionToolkit.Plugins.{pluginId.Replace("-", "")}");
                
                if (Directory.Exists(altPluginDirectory))
                {
                    pluginDirectory = altPluginDirectory;
                }
                else if (Directory.Exists(altPluginDirectoryNoHyphen))
                {
                    pluginDirectory = altPluginDirectoryNoHyphen;
                }
                else
                {
                    // Check for root level DLLs (backward compatibility)
                    var rootDllPath1 = Path.Combine(pluginsDirectory, $"{pluginId}.dll");
                    var rootDllPath2 = Path.Combine(pluginsDirectory, $"LenovoLegionToolkit.Plugins.{pluginId}.dll");
                    
                    if (File.Exists(rootDllPath1) || File.Exists(rootDllPath2))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"IsInstalled({pluginId}): Found as root level DLL");
                        return true;
                    }

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"IsInstalled({pluginId}): Plugin directory does not exist: {pluginDirectory} or {localPluginDirectory} (or alternative path)");
                    return false;
                }
            }
            else if (Directory.Exists(localPluginDirectory))
            {
                pluginDirectory = localPluginDirectory;
            }
            
            // Check for DLL files in the plugin directory
            var dllFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
            if (dllFiles.Length == 0)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"IsInstalled({pluginId}): No DLL files found in plugin directory");
                return false;
            }
            
            // Check for the main plugin DLL (either pluginId.dll or LenovoLegionToolkit.Plugins.{pluginId}.dll)
            var mainDllName1 = $"{pluginId}.dll";
            var mainDllName2 = $"LenovoLegionToolkit.Plugins.{pluginId}.dll";
            var hasMainDll = dllFiles.Any(f => 
                Path.GetFileName(f).Equals(mainDllName1, StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(f).Equals(mainDllName2, StringComparison.OrdinalIgnoreCase));
            
            if (!hasMainDll)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"IsInstalled({pluginId}): Main plugin DLL not found. Available DLLs: [{string.Join(", ", dllFiles.Select(Path.GetFileName))}]");
                // Still return true if there are DLL files (might be different naming convention)
                // But log warning
            }
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"IsInstalled({pluginId}): Plugin files exist, returning true");
            
            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"IsInstalled({pluginId}): Error checking plugin files: {ex.Message}");
            // If we can't check files, fall back to settings-based check
            return isInstalled;
        }
    }

    public void InstallPlugin(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return;

        if (Log.Instance.IsTraceEnabled)
        {
            Log.Instance.Trace($"InstallPlugin called for {pluginId}");
        }

        var installedExtensions = _applicationSettings.Store.InstalledExtensions;

        if (!installedExtensions.Contains(pluginId, StringComparer.OrdinalIgnoreCase))
        {
            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"Adding {pluginId} to installed extensions list");
            }
            
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

            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"Triggering PluginStateChanged for {pluginId} (installed=true)");
            }
            
            OnPluginStateChanged(pluginId, true);
        }
        else
        {
            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"Plugin {pluginId} is already installed");
            }
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

        // Check if any other plugins depend on this plugin
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

        // Stop plugin before uninstall callback
        try
        {
            plugin?.Stop();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error stopping plugin {pluginId}: {ex.Message}", ex);
        }

        // Trigger uninstall callback
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

                var directoriesToScan = new List<string> { subdir };
                
                // If this is the "local" directory, we need to scan its subdirectories
                if (dirName.Equals("local", StringComparison.OrdinalIgnoreCase))
                {
                    directoriesToScan.Clear();
                    directoriesToScan.AddRange(Directory.GetDirectories(subdir));
                }

                foreach (var scanDir in directoriesToScan)
                {
                    // Check all DLL files in this directory
                    var dllFiles = Directory.GetFiles(scanDir, "*.dll", SearchOption.TopDirectoryOnly)
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
                                    pluginDirectoryToDelete.Add(scanDir);
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
                                pluginDirectoryToDelete.Add(scanDir);
                            }
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
    private async Task<bool> DeleteFileWithRetryAsync(string filePath, int maxRetries = 10, int delayMs = 200)
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
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                if (i == maxRetries - 1)
                    return false;
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
        }
        return false;
    }

    /// <summary>
    /// Delete a directory with retry mechanism to handle locked files
    /// </summary>
    private async Task<bool> DeleteDirectoryWithRetryAsync(string directoryPath, int maxRetries = 10, int delayMs = 200)
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
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                if (i == maxRetries - 1)
                    return false;
                await Task.Delay(delayMs).ConfigureAwait(false);
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

    public void UnloadAllPlugins()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Unloading all plugins...");

            // Trigger OnUninstalled for all registered plugins
            foreach (var plugin in _registeredPlugins.Values)
            {
                try
                {
                    plugin.OnUninstalled();
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to trigger OnUninstalled for plugin {plugin.Id}: {ex.Message}", ex);
                }
            }

            // Clear all registered plugins and metadata
            _registeredPlugins.Clear();
            _pluginMetadataCache.Clear();
            _pluginFileCache.Clear();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"All plugins unloaded successfully");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error unloading plugins: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public bool StopPlugin(string pluginId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return false;

            if (!_registeredPlugins.TryGetValue(pluginId, out var plugin))
                return false;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Stopping plugin: {pluginId}");

            plugin.Stop();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugin {pluginId} stopped successfully");

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error stopping plugin {pluginId}: {ex.Message}", ex);
            return false;
        }
    }

    /// <inheritdoc/>
    public void StopAllPlugins()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Stopping all plugins...");

            foreach (var plugin in _registeredPlugins.Values)
            {
                try
                {
                    plugin.Stop();
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to stop plugin {plugin.Id}: {ex.Message}", ex);
                }
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"All plugins stopped successfully");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error stopping all plugins: {ex.Message}", ex);
        }
    }

    protected virtual void OnPluginStateChanged(string pluginId, bool isInstalled)
    {
        PluginStateChanged?.Invoke(this, new PluginEventArgs(pluginId, isInstalled));
    }
}


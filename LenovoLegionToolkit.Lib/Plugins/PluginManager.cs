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
/// Plugin manager implementation - coordinates plugin lifecycle operations
/// Delegates loading, registration, and file operations to specialized components
/// </summary>
public class PluginManager : IPluginManager
{
    private readonly ApplicationSettings _applicationSettings;
    private readonly IPluginSignatureValidator _signatureValidator;
    private readonly IPluginLoader _loader;
    private readonly IPluginRegistry _registry;
    private readonly IPluginFileSystemManager _fileSystemManager;
    private ResolveEventHandler? _assemblyResolveHandler;
    private bool _disposed;

    public event EventHandler<PluginEventArgs>? PluginStateChanged;

    public PluginManager(
        ApplicationSettings applicationSettings,
        IPluginSignatureValidator signatureValidator,
        IPluginLoader loader,
        IPluginRegistry registry,
        IPluginFileSystemManager fileSystemManager)
    {
        _applicationSettings = applicationSettings;
        _signatureValidator = signatureValidator ?? throw new ArgumentNullException(nameof(signatureValidator));
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _fileSystemManager = fileSystemManager ?? throw new ArgumentNullException(nameof(fileSystemManager));
    }

    /// <summary>
    /// Scan and load plugins from the plugins directory
    /// </summary>
    public async void ScanAndLoadPlugins()
    {
        try
        {
            var pluginsDirectory = _fileSystemManager.GetPluginsDirectory();
            if (!Directory.Exists(pluginsDirectory))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugins directory does not exist: {pluginsDirectory}");
                return;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Scanning plugins directory: {pluginsDirectory}");

            RegisterAssemblyResolver(pluginsDirectory);

            var pluginFiles = _fileSystemManager.GetPluginDllFiles();

            foreach (var pluginFile in pluginFiles)
            {
                try
                {
                    await LoadPluginFromFileAsync(pluginFile);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to load plugin from {pluginFile}: {ex.Message}", ex);
                }
            }

            StartInstalledPlugins();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugin scan completed. Total registered plugins: {_registry.Count}");
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

                if (!IsSafeAssemblyName(assemblyName))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Rejected assembly resolve request with unsafe name: {assemblyName}");
                    return null;
                }
                
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
        var cultureFolders = _fileSystemManager.GetCultureFolders();

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
                return TryLoadTrustedPluginAssembly(satellitePath, pluginsDirectory, $"satellite assembly {requestedAssemblyName.Name}");
            }
        }
        return null;
    }

    private Assembly? ResolveDependencyAssembly(string pluginsDirectory, string assemblyName)
    {
        var dllPath = Path.Combine(pluginsDirectory, $"{assemblyName}.dll");
        if (File.Exists(dllPath))
            return TryLoadTrustedPluginAssembly(dllPath, pluginsDirectory, $"dependency assembly {assemblyName}");

        var subdirectories = Directory.GetDirectories(pluginsDirectory);
        foreach (var subdir in subdirectories)
        {
            var subDirDllPath = Path.Combine(subdir, $"{assemblyName}.dll");
            if (File.Exists(subDirDllPath))
                return TryLoadTrustedPluginAssembly(subDirDllPath, pluginsDirectory, $"dependency assembly {assemblyName}");
        }

        var appBaseDirectory = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
        var appBaseDllPath = Path.Combine(appBaseDirectory, $"{assemblyName}.dll");
        if (File.Exists(appBaseDllPath))
            return Assembly.LoadFrom(Path.GetFullPath(appBaseDllPath));

        // Check SDK directory relative to plugins directory
        var sdkDirectory = Path.GetFullPath(Path.Combine(pluginsDirectory, "..", "SDK"));
        var sdkPath = Path.GetFullPath(Path.Combine(sdkDirectory, $"{assemblyName}.dll"));
        if (IsPathWithinDirectory(sdkPath, sdkDirectory) && File.Exists(sdkPath))
            return Assembly.LoadFrom(sdkPath);

        // For SDK assembly, try to resolve from already loaded assemblies
        // The SDK types forward to LenovoLegionToolkit.Lib which is already loaded
        var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);
        if (loadedAssembly != null)
            return loadedAssembly;

        return null;
    }

    private Assembly? TryLoadTrustedPluginAssembly(string candidatePath, string pluginsDirectory, string context)
    {
        try
        {
            var normalizedCandidatePath = Path.GetFullPath(candidatePath);
            var normalizedPluginsDirectory = Path.GetFullPath(pluginsDirectory);

            if (!IsPathWithinDirectory(normalizedCandidatePath, normalizedPluginsDirectory))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Rejected {context} outside plugins directory: {normalizedCandidatePath}");
                return null;
            }

            var signatureResult = _signatureValidator.ValidateAsync(normalizedCandidatePath).GetAwaiter().GetResult();
            if (!signatureResult.IsValid)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Rejected {context} due to invalid signature. [path={normalizedCandidatePath}, status={signatureResult.Status}, error={signatureResult.ErrorMessage}]");
                return null;
            }

            return Assembly.LoadFrom(normalizedCandidatePath);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to resolve trusted {context} from {candidatePath}: {ex.Message}", ex);
            return null;
        }
    }

    private static bool IsPathWithinDirectory(string path, string directoryPath)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = EnsureTrailingSeparator(Path.GetFullPath(directoryPath));
        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            return path;

        return path + Path.DirectorySeparatorChar;
    }

    private static bool IsSafeAssemblyName(string assemblyName)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
            return false;

        foreach (var c in assemblyName)
        {
            if (!(char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-'))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Load a plugin from a DLL file
    /// </summary>
    private async Task LoadPluginFromFileAsync(string pluginFilePath)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Attempting to load plugin from: {pluginFilePath}");

            // Validate plugin signature before loading (security check)
            var signatureResult = await _signatureValidator.ValidateAsync(pluginFilePath);
            if (!signatureResult.IsValid)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin signature validation failed for {pluginFilePath}. Status: {signatureResult.Status}, Error: {signatureResult.ErrorMessage}");

                // Reject plugin if signature is invalid
                throw new global::System.Security.SecurityException($"Plugin signature validation failed: {signatureResult.ErrorMessage}");
            }

            // Use the plugin loader to load the plugin
            var plugin = await _loader.LoadFromFileAsync(pluginFilePath, _signatureValidator);
            if (plugin == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to load plugin from {pluginFilePath}");
                return;
            }

            // Check if this plugin has the GetFeatureExtension method (SDK plugin)
            var pluginType = plugin.GetType();
            var hasGetFeatureExtension = pluginType.GetMethod("GetFeatureExtension",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null) != null;

            // Try to get PluginAttribute for version and compatibility info
            string pluginVersion = "1.0.0";
            string minimumHostVersion = "1.0.0";
            string? author = null;
            var pluginAttribute = pluginType.GetCustomAttributes(true)
                .FirstOrDefault(attr => attr.GetType().Name == "PluginAttribute");

            if (pluginAttribute != null)
            {
                var versionProp = pluginAttribute.GetType().GetProperty("Version");
                var minHostVersionProp = pluginAttribute.GetType().GetProperty("MinimumHostVersion");
                var authorProp = pluginAttribute.GetType().GetProperty("Author");

                if (versionProp != null)
                    pluginVersion = versionProp.GetValue(pluginAttribute)?.ToString() ?? "1.0.0";

                if (minHostVersionProp != null)
                    minimumHostVersion = minHostVersionProp.GetValue(pluginAttribute)?.ToString() ?? "1.0.0";

                if (authorProp != null)
                    author = authorProp.GetValue(pluginAttribute)?.ToString();
            }

            // Check version compatibility
            if (!IsVersionCompatible(minimumHostVersion))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin {pluginType.Name} requires host version {minimumHostVersion} or higher. Current host version is incompatible. Skipping.");
                return;
            }

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
                Author = author,
                FilePath = pluginFilePath
            };

            // Check for existing plugin with same ID
            var existingMetadata = _registry.GetMetadata(plugin.Id);
            if (existingMetadata != null)
            {
                var versionComparison = ComparePluginVersions(pluginVersion, existingMetadata.Version);
                if (versionComparison < 0)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Skipping plugin {plugin.Id} v{pluginVersion} from {pluginFilePath} because newer version {existingMetadata.Version} is already loaded from {existingMetadata.FilePath}.");
                    return;
                }

                if (versionComparison == 0 && _registry.IsRegistered(plugin.Id))
                {
                    var currentWriteTime = GetPluginFileWriteTimeUtc(pluginFilePath);
                    var existingWriteTime = GetPluginFileWriteTimeUtc(existingMetadata.FilePath);

                    if (currentWriteTime <= existingWriteTime)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Skipping duplicate plugin {plugin.Id} v{pluginVersion} from {pluginFilePath}; plugin already registered from {existingMetadata.FilePath}.");
                        return;
                    }

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Replacing duplicate plugin {plugin.Id} v{pluginVersion} with newer file {pluginFilePath} ({currentWriteTime:O}) over {existingMetadata.FilePath} ({existingWriteTime:O}).");
                }
            }

            // Register the plugin
            _registry.Register(plugin, metadata);
            _fileSystemManager.UpdateFileCache(pluginFilePath);

            if (Log.Instance.IsTraceEnabled)
            {
                var pluginTypeInfo = hasGetFeatureExtension ? "SDK" : "direct";
                Log.Instance.Trace($"Successfully loaded {pluginTypeInfo} plugin: {plugin.Id} ({plugin.Name}) v{pluginVersion} (MinHost: {minimumHostVersion}) from {pluginFilePath}");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load plugin assembly from {pluginFilePath}: {ex.Message}", ex);
        }
    }

    private static DateTime GetPluginFileWriteTimeUtc(string? filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return DateTime.MinValue;
            return File.GetLastWriteTimeUtc(filePath);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    public void StartInstalledPlugins()
    {
        try
        {
            foreach (var plugin in _registry.GetAll())
            {
                if (!IsInstalled(plugin.Id))
                    continue;

                if (plugin is not IAppStartupPlugin startupPlugin)
                    continue;

                if (!_registry.MarkStarted(plugin.Id))
                    continue;

                try
                {
                    startupPlugin.OnAppStarted();
                }
                catch (Exception ex)
                {
                    _registry.MarkStopped(plugin.Id);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to start plugin {plugin.Id}: {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to start installed plugins: {ex.Message}", ex);
        }
    }

    public IEnumerable<IPlugin> GetRegisteredPlugins()
    {
        return _registry.GetAll();
    }

    /// <inheritdoc />
    public bool TryGetPlugin(string pluginId, out IPlugin? plugin)
    {
        plugin = _registry.Get(pluginId);
        return plugin != null;
    }

    public PluginMetadata? GetPluginMetadata(string pluginId)
    {
        return _registry.GetMetadata(pluginId);
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
        try
        {
            var pluginsDirectory = _fileSystemManager.GetPluginsDirectory();
            var mainDllNameCandidates = _fileSystemManager.GetMainPluginDllNameCandidates(pluginId);

            // Check plugin directory
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
                    var rootDllCandidates = mainDllNameCandidates
                        .Select(fileName => Path.Combine(pluginsDirectory, fileName));

                    if (rootDllCandidates.Any(File.Exists))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"IsInstalled({pluginId}): Found as root level DLL");
                        return true;
                    }

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"IsInstalled({pluginId}): Plugin directory does not exist");
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

            // Check for the main plugin DLL
            var hasMainDll = dllFiles.Any(f => mainDllNameCandidates.Contains(Path.GetFileName(f), StringComparer.OrdinalIgnoreCase));

            if (!hasMainDll)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"IsInstalled({pluginId}): Main plugin DLL not found. Available DLLs: [{string.Join(", ", dllFiles.Select(Path.GetFileName))}]");
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"IsInstalled({pluginId}): Plugin files exist, returning true");

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"IsInstalled({pluginId}): Error checking plugin files: {ex.Message}");
            return isInstalled;
        }
    }

    public void InstallPlugin(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"InstallPlugin called for {pluginId}");

        var installedExtensions = _applicationSettings.Store.InstalledExtensions;
        var pendingDeletionExtensions = _applicationSettings.Store.PendingDeletionExtensions;
        var pendingDeletionRemoved = pendingDeletionExtensions.RemoveAll(id => StringComparer.OrdinalIgnoreCase.Equals(id, pluginId)) > 0;

        if (!installedExtensions.Contains(pluginId, StringComparer.OrdinalIgnoreCase))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Adding {pluginId} to installed extensions list");

            installedExtensions.Add(pluginId);
            _applicationSettings.SynchronizeStore();

            // Check and install dependencies
            var plugin = _registry.Get(pluginId);
            if (plugin?.Dependencies != null)
            {
                foreach (var dependency in plugin.Dependencies)
                {
                    if (!IsInstalled(dependency))
                        InstallPlugin(dependency);
                }
            }

            // Trigger install callback
            var installedPlugin = _registry.Get(pluginId);
            installedPlugin?.OnInstalled();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Triggering PluginStateChanged for {pluginId} (installed=true)");

            OnPluginStateChanged(pluginId, true);
        }
        else
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugin {pluginId} is already installed");

            if (pendingDeletionRemoved)
                _applicationSettings.SynchronizeStore();
        }

        if (pendingDeletionRemoved && Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Removed {pluginId} from pending deletion list during install/reinstall.");
    }

    public bool UninstallPlugin(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        var installedExtensions = _applicationSettings.Store.InstalledExtensions;

        if (!installedExtensions.Contains(pluginId, StringComparer.OrdinalIgnoreCase))
            return false;

        // Get plugin instance
        var plugin = _registry.Get(pluginId);

        // Check if any other plugins depend on this plugin
        var dependentPlugins = _registry.GetAll()
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

        _registry.MarkStopped(pluginId);

        // Trigger uninstall callback
        plugin?.OnUninstalled();

        OnPluginStateChanged(pluginId, false);

        return true;
    }

    public IEnumerable<string> GetInstalledPluginIds()
    {
        return _applicationSettings.Store.InstalledExtensions;
    }

    /// <inheritdoc/>
    public void RegisterPlugin(IPlugin plugin)
    {
        if (plugin == null)
            return;

        var metadata = new PluginMetadata
        {
            Id = plugin.Id,
            Name = plugin.Name,
            Description = plugin.Description,
            Icon = plugin.Icon,
            IsSystemPlugin = plugin.IsSystemPlugin,
            Dependencies = plugin.Dependencies
        };

        _registry.Register(plugin, metadata);
    }

    public async Task<bool> PermanentlyDeletePluginAsync(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        try
        {
            // Unregister the plugin from the registry
            _registry.Unregister(pluginId);

            // Get plugins directory
            var pluginsDirectory = _fileSystemManager.GetPluginsDirectory();
            if (!Directory.Exists(pluginsDirectory))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugins directory does not exist: {pluginsDirectory}");
                return false;
            }

            // Try to find plugin file by scanning all plugin DLLs and matching by ID
            var foundFiles = new List<string>();
            var pluginDirectoryToDelete = new List<string>();

            // Check subdirectories (plugins are often in their own folder)
            var subdirectories = Directory.GetDirectories(pluginsDirectory);
            var cultureFolders = _fileSystemManager.GetCultureFolders();

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
                        await _fileSystemManager.DeleteDirectoryWithRetryAsync(dir);
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
                var deleted = await _fileSystemManager.DeleteFileWithRetryAsync(filePath);
                if (deleted)
                {
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
                                await _fileSystemManager.DeleteFileWithRetryAsync(relatedFile);
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Deleted related file: {relatedFile}");
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
                                        await _fileSystemManager.DeleteFileWithRetryAsync(satelliteFile);
                                        if (Log.Instance.IsTraceEnabled)
                                            Log.Instance.Trace($"Deleted satellite assembly: {satelliteFile}");
                                    }
                                }
                            }
                        }
                    }
                }
                else if (Log.Instance.IsTraceEnabled)
                {
                    Log.Instance.Trace($"Failed to delete plugin file: {filePath}");
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

    /// <inheritdoc/>
    public bool PermanentlyDeletePlugin(string pluginId)
    {
        // Synchronous wrapper for interface compatibility
        return PermanentlyDeletePluginAsync(pluginId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Perform actual deletion of plugins marked for deletion (call on app exit)
    /// </summary>
    public async Task PerformPendingDeletionsAsync()
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

                await PermanentlyDeletePluginAsync(pluginId);

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

    /// <summary>
    /// Perform actual deletion of plugins marked for deletion (sync wrapper)
    /// </summary>
    public void PerformPendingDeletions()
    {
        PerformPendingDeletionsAsync().GetAwaiter().GetResult();
    }

    public void UnloadAllPlugins()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Unloading all plugins...");

            // Use registry.Clear() which triggers OnUninstalled for all plugins
            _registry.Clear();

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

            var plugin = _registry.Get(pluginId);
            if (plugin == null)
                return false;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Stopping plugin: {pluginId}");

            plugin.Stop();
            _registry.MarkStopped(pluginId);

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

            foreach (var plugin in _registry.GetAll())
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

            // Mark all plugins as stopped
            foreach (var startedId in _registry.GetStartedPluginIds().ToList())
            {
                _registry.MarkStopped(startedId);
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

    /// <summary>
    /// Check if the current host version meets the plugin's minimum requirements
    /// </summary>
    private static bool IsVersionCompatible(string minimumHostVersion)
    {
        try
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (currentVersion == null)
                return false;

            if (!Version.TryParse(minimumHostVersion, out var minVersion))
                return true; // If we can't parse, allow it (backward compatibility)

            return currentVersion >= minVersion;
        }
        catch
        {
            return true; // Default to allowing if check fails
        }
    }

    private static int ComparePluginVersions(string? left, string? right)
    {
        if (Version.TryParse(left, out var leftVersion) && Version.TryParse(right, out var rightVersion))
            return leftVersion.CompareTo(rightVersion);

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }
}

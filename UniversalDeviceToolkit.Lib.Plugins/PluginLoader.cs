using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Plugin loader interface
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// Load a plugin from a DLL file asynchronously
    /// </summary>
    Task<IPlugin?> LoadFromFileAsync(string dllPath, IPluginSignatureValidator signatureValidator);

    /// <summary>
    /// Unload a previously loaded plugin assembly context, when applicable.
    /// </summary>
    bool Unload(string pluginId);

    /// <summary>
    /// Check if a DLL file can be loaded as a plugin
    /// </summary>
    bool CanLoad(string filePath, string? parentDirectoryName = null);
}

/// <summary>
/// Plugin loader implementation
/// Handles loading plugin assemblies and creating plugin instances
/// </summary>
public class PluginLoader : IPluginLoader
{
    private static readonly ConcurrentDictionary<string, PluginDependencyResolutionContext> DependencyResolutionContexts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, PluginDependencyResolutionContext> PluginDependencyContexts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, PluginAssemblyLoadContext> PluginLoadContexts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object DependencyResolverRegistrationLock = new();
    private static bool _dependencyResolverRegistered;

    private readonly HashSet<string> _cultureFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "ar", "bg", "bs", "ca", "cs", "de", "el", "es", "fr", "hu", "it", "ja", "ko",
        "lv", "nl-nl", "pl", "pt", "pt-br", "ro", "ru", "sk", "tr", "uk", "uz-latn-uz",
        "vi", "zh-hans", "zh-hant", "tools"
    };

    /// <summary>
    /// Load a plugin from a DLL file asynchronously
    /// </summary>
    public async Task<IPlugin?> LoadFromFileAsync(string dllPath, IPluginSignatureValidator signatureValidator)
    {
        if (string.IsNullOrWhiteSpace(dllPath))
        {
            Log.Instance.Warning("LoadFromFileAsync: DLL path is empty");
            return null;
        }

        if (!File.Exists(dllPath))
        {
            Log.Instance.Warning($"LoadFromFileAsync: DLL file not found: {dllPath}");
            return null;
        }

        RegisteredPluginDependencyResolutionContext? registeredDependencyContext = null;
        var keepDependencyContext = false;

        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Attempting to load plugin from: {dllPath}");

            var normalizedDllPath = Path.GetFullPath(dllPath);
            var pluginDirectory = Path.GetDirectoryName(normalizedDllPath);

            // Register AssemblyResolve handler early to handle dependencies that may be loaded
            // during signature validation or assembly loading.
            if (!string.IsNullOrWhiteSpace(pluginDirectory))
            {
                registeredDependencyContext = RegisterPluginDependencyResolutionContext(normalizedDllPath, pluginDirectory, signatureValidator);
            }

            // Validate plugin signature before loading (security check)
            var signatureResult = await signatureValidator.ValidateAsync(dllPath).ConfigureAwait(false);
            if (!signatureResult.IsValid)
            {
                Log.Instance.Warning($"Plugin signature validation failed for {dllPath}. Status: {signatureResult.Status}, Error: {signatureResult.ErrorMessage}");
                return null;
            }

            // Load the main assembly from bytes to avoid file locking, but resolve plugin-local
            // dependencies through a dedicated AssemblyLoadContext.
            Assembly? assembly = null;
            PluginAssemblyLoadContext? pluginLoadContext = null;
            try
            {
                var assemblyBytes = await File.ReadAllBytesAsync(normalizedDllPath).ConfigureAwait(false);
                pluginLoadContext = new PluginAssemblyLoadContext(normalizedDllPath, pluginDirectory ?? string.Empty, signatureValidator);
                assembly = pluginLoadContext.LoadFromStream(new MemoryStream(assemblyBytes));
                registeredDependencyContext?.Context.SetPluginMainAssembly(assembly);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"Failed to load assembly from {dllPath}", ex);
                pluginLoadContext?.Unload();
                return null;
            }

            // Find all types that implement IPlugin
            Type[] pluginTypes;
            try
            {
                pluginTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Log.Instance.Warning($"Failed to get types from assembly {dllPath}. Loader exceptions:");
                if (ex.LoaderExceptions != null)
                {
                    foreach (var loaderEx in ex.LoaderExceptions)
                    {
                        Log.Instance.Warning($"  - {loaderEx?.Message}", loaderEx);
                    }
                }
                // Try to continue with successfully loaded types
                pluginTypes = ex.Types.Where(t => t != null).OfType<Type>().ToArray();
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"Error getting types from assembly {dllPath}", ex);
                pluginLoadContext?.Unload();
                return null;
            }

            var validPluginTypes = pluginTypes
                .Where(t => t != null
                    && IsPluginTypeCandidate(t)
                    && !t.IsInterface
                    && !t.IsAbstract
                    && t.GetConstructor(Type.EmptyTypes) != null)
                .ToList();

            if (validPluginTypes.Count == 0)
            {
                Log.Instance.Warning($"No valid plugin types found in {dllPath}");
                pluginLoadContext?.Unload();
                return null;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Found {validPluginTypes.Count} plugin type(s) in {dllPath}");

            // Return the first valid plugin instance
            foreach (var pluginType in validPluginTypes)
            {
                try
                {
                    var plugin = CreatePluginInstance(pluginType, dllPath);
                    if (plugin != null)
                    {
                        if (string.IsNullOrWhiteSpace(plugin.Id))
                        {
                            Log.Instance.Warning($"Plugin from {dllPath} has empty ID, skipping");
                            continue;
                        }

                        if (pluginLoadContext is not null)
                            PluginLoadContexts[plugin.Id] = pluginLoadContext;
                        if (registeredDependencyContext is not null)
                            PluginDependencyContexts[plugin.Id] = registeredDependencyContext.Context;
                        keepDependencyContext = true;
                        
                        Log.Instance.Info($"Successfully created plugin instance: {plugin.Id} ({plugin.Name}) from {dllPath}");
                        return plugin;
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Error($"Failed to create instance of plugin type {pluginType.Name} from {dllPath}", ex);
                }
            }

            pluginLoadContext?.Unload();
            return null;
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Failed to load plugin assembly from {dllPath}", ex);
            return null;
        }
        finally
        {
            if (!keepDependencyContext && registeredDependencyContext is { IsNew: true })
                RemovePluginDependencyResolutionContext(registeredDependencyContext.Context);
        }
    }

    public bool Unload(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        PluginLoadContexts.TryRemove(pluginId, out var loadContext);
        PluginDependencyContexts.TryRemove(pluginId, out var dependencyContext);

        if (loadContext is null && dependencyContext is null)
            return false;

        var success = true;

        try
        {
            if (dependencyContext is not null)
                RemovePluginDependencyResolutionContext(dependencyContext);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to remove plugin dependency resolution context for {pluginId}: {ex.Message}", ex);
            success = false;
        }

        try
        {
            loadContext?.Unload();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to unload plugin load context for {pluginId}: {ex.Message}", ex);
            success = false;
        }

        return success;
    }

    private static RegisteredPluginDependencyResolutionContext RegisterPluginDependencyResolutionContext(
        string pluginMainAssemblyPath,
        string pluginDirectory,
        IPluginSignatureValidator signatureValidator)
    {
        var normalizedMainAssemblyPath = Path.GetFullPath(pluginMainAssemblyPath);
        var normalizedPluginDirectory = Path.GetFullPath(pluginDirectory);

        var isNew = DependencyResolutionContexts.TryAdd(normalizedMainAssemblyPath,
            new PluginDependencyResolutionContext(
                normalizedMainAssemblyPath,
                normalizedPluginDirectory,
                signatureValidator));

        var context = DependencyResolutionContexts[normalizedMainAssemblyPath];

        lock (DependencyResolverRegistrationLock)
        {
            if (!_dependencyResolverRegistered)
            {
                AppDomain.CurrentDomain.AssemblyResolve += ResolvePluginDependencyAssembly;
                _dependencyResolverRegistered = true;
            }
        }

        return new RegisteredPluginDependencyResolutionContext(context, isNew);
    }

    private static void RemovePluginDependencyResolutionContext(PluginDependencyResolutionContext context)
    {
        DependencyResolutionContexts.TryRemove(context.PluginMainAssemblyPath, out _);

        lock (DependencyResolverRegistrationLock)
        {
            if (DependencyResolutionContexts.IsEmpty && _dependencyResolverRegistered)
            {
                AppDomain.CurrentDomain.AssemblyResolve -= ResolvePluginDependencyAssembly;
                _dependencyResolverRegistered = false;
            }
        }
    }

    private static Assembly? ResolvePluginDependencyAssembly(object? sender, ResolveEventArgs args)
    {
        var contexts = GetScopedDependencyResolutionContexts(args.RequestingAssembly);

        foreach (var context in contexts)
        {
            var assembly = ResolvePluginDependencyAssembly(
                args.Name,
                context.PluginMainAssemblyPath,
                context.PluginDirectory,
                context.SignatureValidator);
            if (assembly != null)
                return assembly;
        }

        return null;
    }

    private static Assembly? ResolvePluginDependencyAssembly(
        string requestedAssemblyFullName,
        string pluginMainAssemblyPath,
        string pluginDirectory,
        IPluginSignatureValidator signatureValidator)
    {
        try
        {
            var requestedAssemblyName = new AssemblyName(requestedAssemblyFullName);
            var assemblyName = requestedAssemblyName.Name;
            if (string.IsNullOrWhiteSpace(assemblyName) || !IsSafeAssemblyName(assemblyName))
                return null;

            // Try to find a version-compatible loaded assembly
            // Compare name, version, and public key token for proper binding
            var loadedAssembly = FindCompatibleLoadedAssembly(requestedAssemblyName, pluginDirectory);
            if (loadedAssembly != null)
                return loadedAssembly;

            var candidatePath = GetPluginAssemblyCandidatePath(requestedAssemblyName, pluginMainAssemblyPath, pluginDirectory);
            if (string.IsNullOrWhiteSpace(candidatePath) || !File.Exists(candidatePath))
                return null;

            // Note: AssemblyResolve event handlers must return synchronously.
            // We use GetAwaiter().GetResult() here because the event signature requires a synchronous return.
            // This is a known limitation of AppDomain.AssemblyResolve - the alternative would be
            // to skip signature validation for dependencies, which is a security risk.
            var signatureResult = signatureValidator.ValidateAsync(candidatePath).GetAwaiter().GetResult();
            if (!IsValidPluginDependencySignature(signatureResult, requestedAssemblyName, candidatePath))
            {
                Log.Instance.Warning($"Rejected plugin dependency due to invalid signature. [path={candidatePath}, status={signatureResult.Status}, error={signatureResult.ErrorMessage}]");
                return null;
            }

            return Assembly.LoadFrom(candidatePath);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to resolve plugin dependency assembly {requestedAssemblyFullName}.", ex);
            return null;
        }
    }

    private static bool IsValidPluginDependencySignature(
        PluginSignatureResult signatureResult,
        AssemblyName requestedAssemblyName,
        string candidatePath)
    {
        if (signatureResult.IsValid)
            return true;

        if (signatureResult.Status != PluginSignatureStatus.Expired ||
            signatureResult.Certificate == null ||
            !IsMicrosoftSignedStrongNamedDependency(signatureResult.Certificate, requestedAssemblyName, candidatePath))
        {
            return false;
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Allowing expired but trusted Microsoft-signed plugin dependency. [path={candidatePath}, expires={signatureResult.ExpirationDate:O}]");

        return true;
    }

    private static bool IsMicrosoftSignedStrongNamedDependency(
        X509Certificate2 certificate,
        AssemblyName requestedAssemblyName,
        string candidatePath)
    {
        var requestedPublicKeyToken = requestedAssemblyName.GetPublicKeyToken();
        if (requestedPublicKeyToken == null || requestedPublicKeyToken.Length == 0)
            return false;

        if (!IsCandidateAssemblyIdentityCompatible(candidatePath, requestedAssemblyName, requestedPublicKeyToken))
            return false;

        if (!CertificateLooksMicrosoftOwned(certificate))
            return false;

        return IsCertificateTrustedIgnoringExpiration(certificate);
    }

    private static bool IsCandidateAssemblyIdentityCompatible(
        string candidatePath,
        AssemblyName requestedAssemblyName,
        byte[] requestedPublicKeyToken)
    {
        try
        {
            var candidateAssemblyName = AssemblyName.GetAssemblyName(candidatePath);
            if (!string.Equals(candidateAssemblyName.Name, requestedAssemblyName.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            if (requestedAssemblyName.Version != null &&
                candidateAssemblyName.Version != null &&
                candidateAssemblyName.Version < requestedAssemblyName.Version)
            {
                return false;
            }

            var candidatePublicKeyToken = candidateAssemblyName.GetPublicKeyToken();
            return candidatePublicKeyToken != null &&
                   candidatePublicKeyToken.SequenceEqual(requestedPublicKeyToken);
        }
        catch
        {
            return false;
        }
    }

    private static bool CertificateLooksMicrosoftOwned(X509Certificate2 certificate)
    {
        return ContainsMicrosoftCorporation(certificate.Subject) ||
               ContainsMicrosoftCorporation(certificate.Issuer);
    }

    private static bool ContainsMicrosoftCorporation(string value) =>
        value.Contains("Microsoft Corporation", StringComparison.OrdinalIgnoreCase);

    private static bool IsCertificateTrustedIgnoringExpiration(X509Certificate2 certificate)
    {
        try
        {
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreNotTimeValid;
            chain.ChainPolicy.VerificationTime = DateTime.UtcNow;
            return chain.Build(certificate);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Find a loaded assembly that is compatible with the requested assembly name.
    /// Compares name, version (if specified), and public key token (if specified).
    /// </summary>
    private static Assembly? FindCompatibleLoadedAssembly(AssemblyName requestedName, string? pluginDirectory = null)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var requestedVersion = requestedName.Version;
        var requestedPublicKey = requestedName.GetPublicKeyToken();

        foreach (var assembly in assemblies)
        {
            var assemblyName = assembly.GetName();

            // Name must match (case-insensitive)
            if (!string.Equals(assemblyName.Name, requestedName.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            // If version is specified, check for version compatibility
            // Accept exact match or higher version (binding redirect behavior)
            if (requestedVersion != null && assemblyName.Version != null)
            {
                if (assemblyName.Version < requestedVersion)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Skipping loaded assembly {assemblyName.Name} v{assemblyName.Version} - requested version {requestedVersion} is higher");
                    continue;
                }
            }

            // If public key token is specified, it must match
            if (requestedPublicKey != null && requestedPublicKey.Length > 0)
            {
                var assemblyPublicKey = assemblyName.GetPublicKeyToken();
                if (assemblyPublicKey == null || !requestedPublicKey.SequenceEqual(assemblyPublicKey))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Skipping loaded assembly {assemblyName.Name} - public key token mismatch");
                    continue;
                }
            }

            if (!IsLoadedAssemblyCompatibleWithContext(assembly, pluginDirectory))
                continue;

            return assembly;
        }

        return null;
    }

    private static PluginDependencyResolutionContext[] GetScopedDependencyResolutionContexts(Assembly? requestingAssembly)
    {
        var contexts = DependencyResolutionContexts.Values.ToArray();

        if (contexts.Length == 0)
            return [];

        if (requestingAssembly == null)
            return contexts.Length == 1 ? contexts : [];

        return contexts
            .Where(context => IsRequestingAssemblyInContext(requestingAssembly, context))
            .ToArray();
    }

    private static bool IsRequestingAssemblyInContext(Assembly requestingAssembly, PluginDependencyResolutionContext context)
    {
        if (ReferenceEquals(requestingAssembly, context.PluginMainAssembly))
            return true;

        if (!string.IsNullOrWhiteSpace(context.PluginMainAssemblyFullName) &&
            string.Equals(requestingAssembly.FullName, context.PluginMainAssemblyFullName, StringComparison.Ordinal))
        {
            return true;
        }

        var requestingAssemblyLocation = GetAssemblyLocation(requestingAssembly);
        return !string.IsNullOrWhiteSpace(requestingAssemblyLocation) &&
               IsPathWithinDirectory(requestingAssemblyLocation, context.PluginDirectory);
    }

    private static bool IsLoadedAssemblyCompatibleWithContext(Assembly assembly, string? pluginDirectory)
    {
        if (string.IsNullOrWhiteSpace(pluginDirectory))
            return true;

        var location = GetAssemblyLocation(assembly);
        if (string.IsNullOrWhiteSpace(location))
            return IsContextOwnedLoadedAssemblyCompatibleWithContext(assembly, pluginDirectory);

        if (IsPathWithinDirectory(location, pluginDirectory))
            return true;

        var contexts = DependencyResolutionContexts.Values.ToArray();
        return !contexts.Any(context => IsPathWithinDirectory(location, context.PluginDirectory));
    }

    private static bool IsContextOwnedLoadedAssemblyCompatibleWithContext(Assembly assembly, string pluginDirectory)
    {
        var contexts = DependencyResolutionContexts.Values.ToArray();
        var owningContext = contexts.FirstOrDefault(context => ReferenceEquals(assembly, context.PluginMainAssembly));
        return owningContext == null ||
               string.Equals(owningContext.PluginDirectory, Path.GetFullPath(pluginDirectory), StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetAssemblyLocation(Assembly assembly)
    {
        try
        {
            return string.IsNullOrWhiteSpace(assembly.Location)
                ? null
                : Path.GetFullPath(assembly.Location);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetPluginAssemblyCandidatePath(AssemblyName requestedAssemblyName, string pluginMainAssemblyPath, string pluginDirectory)
    {
        var assemblyName = requestedAssemblyName.Name;
        if (string.IsNullOrWhiteSpace(assemblyName) || string.IsNullOrWhiteSpace(pluginDirectory))
            return null;

        var candidatePath = assemblyName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) &&
                            requestedAssemblyName.CultureInfo is { Name.Length: > 0 } cultureInfo
            ? Path.Combine(pluginDirectory, cultureInfo.Name, $"{assemblyName[..^".resources".Length]}.resources.dll")
            : Path.Combine(pluginDirectory, $"{assemblyName}.dll");

        var normalizedCandidatePath = Path.GetFullPath(candidatePath);
        if (string.Equals(normalizedCandidatePath, Path.GetFullPath(pluginMainAssemblyPath), StringComparison.OrdinalIgnoreCase))
            return null;

        return IsPathWithinDirectory(normalizedCandidatePath, pluginDirectory)
            ? normalizedCandidatePath
            : null;
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
        foreach (var c in assemblyName)
        {
            if (!(char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-'))
                return false;
        }

        return true;
    }

    private sealed class PluginDependencyResolutionContext(
        string pluginMainAssemblyPath,
        string pluginDirectory,
        IPluginSignatureValidator signatureValidator)
    {
        public string PluginMainAssemblyPath { get; } = pluginMainAssemblyPath;
        public string PluginDirectory { get; } = pluginDirectory;
        public IPluginSignatureValidator SignatureValidator { get; } = signatureValidator;
        public Assembly? PluginMainAssembly { get; private set; }
        public string? PluginMainAssemblyFullName { get; private set; }

        public void SetPluginMainAssembly(Assembly assembly)
        {
            PluginMainAssembly = assembly;
            PluginMainAssemblyFullName = assembly.FullName;
        }
    }

    private sealed record RegisteredPluginDependencyResolutionContext(
        PluginDependencyResolutionContext Context,
        bool IsNew);

    private sealed class PluginAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _pluginMainAssemblyPath;
        private readonly string _pluginDirectory;
        private readonly AssemblyDependencyResolver _resolver;
        private readonly IPluginSignatureValidator _signatureValidator;

        public PluginAssemblyLoadContext(string pluginMainAssemblyPath, string pluginDirectory, IPluginSignatureValidator signatureValidator)
            : base($"Plugin:{Path.GetFileNameWithoutExtension(pluginMainAssemblyPath)}", isCollectible: true)
        {
            _pluginMainAssemblyPath = Path.GetFullPath(pluginMainAssemblyPath);
            _pluginDirectory = string.IsNullOrWhiteSpace(pluginDirectory)
                ? Path.GetDirectoryName(_pluginMainAssemblyPath) ?? string.Empty
                : Path.GetFullPath(pluginDirectory);
            _resolver = new AssemblyDependencyResolver(_pluginMainAssemblyPath);
            _signatureValidator = signatureValidator;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblySimpleName = assemblyName.Name;
            if (string.IsNullOrWhiteSpace(assemblySimpleName))
                return null;

            // Share host contracts and host UI/runtime assemblies with the default context.
            if (ShouldShareDefaultContextAssembly(assemblySimpleName))
            {
                return ResolveSharedHostAssembly(assemblyName);
            }

            var candidatePath = _resolver.ResolveAssemblyToPath(assemblyName)
                               ?? GetPluginAssemblyCandidatePath(assemblyName, _pluginMainAssemblyPath, _pluginDirectory);

            if (string.IsNullOrWhiteSpace(candidatePath) || !File.Exists(candidatePath))
                return null;

            var normalizedCandidatePath = Path.GetFullPath(candidatePath);
            if (!IsPathWithinDirectory(normalizedCandidatePath, _pluginDirectory))
                return null;

            var signatureResult = _signatureValidator.ValidateAsync(normalizedCandidatePath).GetAwaiter().GetResult();
            if (!IsValidPluginDependencySignature(signatureResult, assemblyName, normalizedCandidatePath))
                return null;

            return LoadFromAssemblyPath(normalizedCandidatePath);
        }

        private static bool ShouldShareDefaultContextAssembly(string assemblySimpleName)
        {
            if (assemblySimpleName.StartsWith("Wpf.Ui", StringComparison.OrdinalIgnoreCase))
                return true;

            return assemblySimpleName.StartsWith("LenovoLegionToolkit", StringComparison.OrdinalIgnoreCase) &&
                   !assemblySimpleName.Equals("LenovoLegionToolkit.Plugins.SDK", StringComparison.OrdinalIgnoreCase) &&
                   !assemblySimpleName.Equals("LenovoLegionToolkit.Plugins.Shared", StringComparison.OrdinalIgnoreCase);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (!string.IsNullOrWhiteSpace(libraryPath) && File.Exists(libraryPath))
                return LoadUnmanagedDllFromPath(libraryPath);

            return IntPtr.Zero;
        }

        private static Assembly? ResolveSharedHostAssembly(AssemblyName assemblyName)
        {
            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly =>
                {
                    var loadedName = assembly.GetName();
                    return string.Equals(loadedName.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase);
                });

            if (loadedAssembly is not null)
                return loadedAssembly;

            var appBaseCandidate = Path.Combine(AppContext.BaseDirectory, $"{assemblyName.Name}.dll");
            if (!File.Exists(appBaseCandidate))
                return null;

            try
            {
                return Assembly.LoadFrom(Path.GetFullPath(appBaseCandidate));
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to load shared host assembly from app base: {appBaseCandidate}", ex);
                return null;
            }
        }
    }

    /// <summary>
    /// Create a plugin instance from a type
    /// </summary>
    private IPlugin? CreatePluginInstance(Type pluginType, string dllPath)
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

            // Check version compatibility
            if (!IsVersionCompatible(minimumHostVersion))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin {pluginType.Name} requires host version {minimumHostVersion} or higher. Current host version is incompatible. Skipping.");
                return null;
            }

            // Create an instance of the plugin
            IPlugin? plugin = null;
            try
            {
                plugin = (IPlugin?)Activator.CreateInstance(pluginType);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to create instance of plugin type {pluginType.Name}: {ex.Message}", ex);
                return null;
            }

            return plugin;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to create plugin instance from type {pluginType.Name}: {ex.Message}", ex);
            return null;
        }
    }

    private static bool IsPluginTypeCandidate(Type type)
    {
        return typeof(IPlugin).IsAssignableFrom(type);
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

    /// <summary>
    /// Check if a DLL file can be loaded as a plugin
    /// </summary>
    public bool CanLoad(string filePath, string? parentDirectoryName = null)
    {
        var fileName = Path.GetFileName(filePath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

        if (fileName.Equals("LenovoLegionToolkit.Plugins.SDK.dll", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("LenovoLegionToolkit.Plugins.Shared.dll", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (fileName.StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrWhiteSpace(parentDirectoryName))
            return false;

        var normalizedDllName = NormalizePluginToken(fileNameWithoutExtension);
        var normalizedParentName = NormalizePluginToken(parentDirectoryName);
        var normalizedParentShortName = NormalizePluginToken(parentDirectoryName.Replace("LenovoLegionToolkit.Plugins.", string.Empty, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(normalizedDllName))
            return false;

        return normalizedDllName.Equals(normalizedParentName, StringComparison.OrdinalIgnoreCase) ||
               normalizedDllName.Equals(normalizedParentShortName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalize a plugin token for comparison
    /// </summary>
    private static string NormalizePluginToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    /// <summary>
    /// Get culture folders that should be skipped during plugin scanning
    /// </summary>
    public HashSet<string> GetCultureFolders() => _cultureFolders;
}

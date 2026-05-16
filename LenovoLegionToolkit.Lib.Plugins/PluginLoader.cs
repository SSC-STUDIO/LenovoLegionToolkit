using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            // Note: X509Certificate.CreateFromSignedFile uses native APIs and typically doesn't
            // trigger managed assembly resolution, but we register early for defense in depth.
            if (!string.IsNullOrWhiteSpace(pluginDirectory))
            {
                registeredDependencyContext = RegisterPluginDependencyResolutionContext(normalizedDllPath, pluginDirectory, signatureValidator);
            }

            // Validate plugin signature before loading (security check)
            var signatureResult = await signatureValidator.ValidateAsync(dllPath);
            if (!signatureResult.IsValid)
            {
                Log.Instance.Warning($"Plugin signature validation failed for {dllPath}. Status: {signatureResult.Status}, Error: {signatureResult.ErrorMessage}");

                return null;
            }

            // Load the assembly from bytes to avoid file locking
            Assembly? assembly = null;
            try
            {
                var assemblyBytes = File.ReadAllBytes(normalizedDllPath);
                assembly = Assembly.Load(assemblyBytes);
                registeredDependencyContext?.Context.SetPluginMainAssembly(assembly);
                keepDependencyContext = true;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to load assembly from {dllPath}: {ex.Message}", ex);
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
                if (Log.Instance.IsTraceEnabled)
                {
                    Log.Instance.Trace($"Failed to get types from assembly {dllPath}. Loader exceptions:", ex);
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
                Log.Instance.Trace($"Found {validPluginTypes.Count} plugin type(s) in {dllPath}");

            // Return the first valid plugin instance
            foreach (var pluginType in validPluginTypes)
            {
                try
                {
                    var plugin = CreatePluginInstance(pluginType, dllPath);
                    if (plugin != null)
                        return plugin;
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to create instance of plugin type {pluginType.Name}: {ex.Message}", ex);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load plugin assembly from {dllPath}: {ex.Message}", ex);
            return null;
        }
        finally
        {
            if (!keepDependencyContext && registeredDependencyContext is { IsNew: true })
                RemovePluginDependencyResolutionContext(registeredDependencyContext.Context);
        }
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
                plugin = Activator.CreateInstance(pluginType) as IPlugin;
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

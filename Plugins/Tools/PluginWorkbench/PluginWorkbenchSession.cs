using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Plugins.SDK;

namespace PluginWorkbench;

internal sealed class PluginWorkbenchSession : IDisposable
{
    private const string PluginConfigurationRootEnvironmentVariable = "LLT_PLUGIN_CONFIG_ROOT";

    private readonly string? _temporaryDirectory;
    private readonly ResolveEventHandler _assemblyResolveHandler;
    private bool _disposed;

    private PluginWorkbenchSession(
        IPlugin plugin,
        string sourcePath,
        string pluginDirectory,
        bool isArchiveSource,
        string? temporaryDirectory)
    {
        Plugin = plugin;
        SourcePath = sourcePath;
        PluginDirectory = pluginDirectory;
        IsArchiveSource = isArchiveSource;
        _temporaryDirectory = temporaryDirectory;
        _assemblyResolveHandler = (_, args) => ResolveAssemblyFromPluginDirectory(args, PluginDirectory);
        AppDomain.CurrentDomain.AssemblyResolve += _assemblyResolveHandler;

        var pluginAttribute = plugin.GetType().GetCustomAttributes(typeof(PluginAttribute), inherit: false)
            .OfType<PluginAttribute>()
            .FirstOrDefault();
        PluginVersion = pluginAttribute?.Version ?? "1.0.0";
        MinimumHostVersion = pluginAttribute?.MinimumHostVersion ?? "1.0.0";

        try
        {
            FeaturePage = CreatePluginPage(GetPluginExtension(plugin, "GetFeatureExtension"));
            SettingsPage = CreatePluginPage(GetPluginExtension(plugin, "GetSettingsPage"));
            OptimizationCategory = GetOptimizationCategory(plugin);
        }
        catch
        {
            AppDomain.CurrentDomain.AssemblyResolve -= _assemblyResolveHandler;
            throw;
        }
    }

    public IPlugin Plugin { get; }
    public string SourcePath { get; }
    public string PluginDirectory { get; }
    public bool IsArchiveSource { get; }
    public string PluginVersion { get; }
    public string MinimumHostVersion { get; }
    public LenovoLegionToolkit.Lib.Plugins.IPluginPage? FeaturePage { get; }
    public LenovoLegionToolkit.Lib.Plugins.IPluginPage? SettingsPage { get; }
    public WindowsOptimizationCategoryDefinition? OptimizationCategory { get; }

    public static async Task<PluginWorkbenchSession> LoadFromBuildOutputAsync(string buildDirectory, LenovoLegionToolkit.Plugins.SDK.PluginHostMode mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(buildDirectory);

        var plugin = await LoadPluginAsync(buildDirectory).ConfigureAwait(false);
        var session = new PluginWorkbenchSession(plugin, buildDirectory, buildDirectory, false, null);
        session.Start(mode);
        return session;
    }

    public static async Task<PluginWorkbenchSession> LoadFromArchiveAsync(string archivePath, LenovoLegionToolkit.Plugins.SDK.PluginHostMode mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);

        var extractRoot = Path.Combine(
            Path.GetTempPath(),
            "llt-plugin-workbench",
            Path.GetFileNameWithoutExtension(archivePath),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(extractRoot);
        ZipFile.ExtractToDirectory(archivePath, extractRoot);

        var plugin = await LoadPluginAsync(extractRoot).ConfigureAwait(false);
        var session = new PluginWorkbenchSession(plugin, archivePath, extractRoot, true, extractRoot);
        session.Start(mode);
        return session;
    }

    public UIElement? CreateFeatureContent()
    {
        return CreateContent(FeaturePage);
    }

    public UIElement? CreateSettingsContent()
    {
        return CreateContent(SettingsPage);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            Plugin.Stop();
        }
        catch
        {
            // Best-effort session cleanup only.
        }

        try
        {
            Plugin.OnShutdown();
        }
        catch
        {
            // Best-effort session cleanup only.
        }

        AppDomain.CurrentDomain.AssemblyResolve -= _assemblyResolveHandler;

        if (string.IsNullOrWhiteSpace(_temporaryDirectory) || !Directory.Exists(_temporaryDirectory))
        {
            return;
        }

        try
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
        catch
        {
            // Temporary cleanup is best effort only.
        }
    }

    private static async Task<IPlugin> LoadPluginAsync(string pluginDirectory)
    {
        var loader = new PluginLoader();
        var validator = new PluginSignatureValidator(PluginSignatureSettings.Development);
        var pluginDllPath = ResolvePluginDllPath(pluginDirectory, loader);
        var plugin = await loader.LoadFromFileAsync(pluginDllPath, validator).ConfigureAwait(false);
        return plugin ?? throw new InvalidOperationException($"Failed to load plugin from '{pluginDllPath}'.");
    }

    private static string ResolvePluginDllPath(string pluginDirectory, PluginLoader loader)
    {
        var directoryName = new DirectoryInfo(pluginDirectory).Name;
        var candidates = Directory.EnumerateFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly)
            .Where(path => loader.CanLoad(path, directoryName))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new FileNotFoundException($"No plugin DLL candidates were found in '{pluginDirectory}'.");
        }

        var exactDirectoryMatch = candidates.FirstOrDefault(path =>
            string.Equals(Path.GetFileNameWithoutExtension(path), directoryName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(exactDirectoryMatch))
        {
            return exactDirectoryMatch;
        }

        var filteredCandidates = candidates
            .Where(path =>
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                return !string.Equals(fileName, "LenovoLegionToolkit.Plugins.Shared", StringComparison.OrdinalIgnoreCase)
                       && !string.Equals(fileName, "LenovoLegionToolkit.Plugins.SDK", StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        return filteredCandidates.Count > 0 ? filteredCandidates[0] : candidates[0];
    }

    private static Assembly? ResolveAssemblyFromPluginDirectory(ResolveEventArgs args, string pluginDirectory)
    {
        var assemblyName = new AssemblyName(args.Name).Name;
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return null;
        }

        var assemblyPath = Path.Combine(pluginDirectory, $"{assemblyName}.dll");
        return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
    }

    private void Start(LenovoLegionToolkit.Plugins.SDK.PluginHostMode mode)
    {
        EnsureInstalledState();

        if (mode != LenovoLegionToolkit.Plugins.SDK.PluginHostMode.RealRuntime)
        {
            return;
        }

        if (Plugin is LenovoLegionToolkit.Lib.Plugins.IAppStartupPlugin startupPlugin)
        {
            startupPlugin.OnAppStarted();
            return;
        }

        var onAppStarted = Plugin.GetType().GetMethod("OnAppStarted", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
        onAppStarted?.Invoke(Plugin, null);
    }

    private void EnsureInstalledState()
    {
        var markerPath = ResolveInstalledMarkerPath();
        if (string.IsNullOrWhiteSpace(markerPath))
        {
            Plugin.OnInstalled();
            return;
        }

        if (File.Exists(markerPath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(markerPath)!);
        Plugin.OnInstalled();
        File.WriteAllText(markerPath, DateTimeOffset.UtcNow.ToString("O"));
    }

    private string? ResolveInstalledMarkerPath()
    {
        var root = Environment.GetEnvironmentVariable(PluginConfigurationRootEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(root))
        {
            return null;
        }

        var safePluginId = string.Concat(Plugin.Id.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
        return Path.Combine(root, ".plugin-workbench", $"{safePluginId}.installed");
    }

    private static object? GetPluginExtension(IPlugin plugin, string methodName)
    {
        if (plugin is LenovoLegionToolkit.Lib.Plugins.PluginBase pluginBase)
        {
            return methodName switch
            {
                "GetFeatureExtension" => pluginBase.GetFeatureExtension(),
                "GetSettingsPage" => pluginBase.GetSettingsPage(),
                _ => null
            };
        }

        return plugin.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)?.Invoke(plugin, null);
    }

    private static WindowsOptimizationCategoryDefinition? GetOptimizationCategory(IPlugin plugin)
    {
        if (plugin is LenovoLegionToolkit.Lib.Plugins.PluginBase pluginBase)
        {
            return pluginBase.GetOptimizationCategory();
        }

        return plugin.GetType()
            .GetMethod("GetOptimizationCategory", BindingFlags.Instance | BindingFlags.Public)?
            .Invoke(plugin, null) as WindowsOptimizationCategoryDefinition;
    }

    private static LenovoLegionToolkit.Lib.Plugins.IPluginPage? CreatePluginPage(object? extension)
    {
        return extension as LenovoLegionToolkit.Lib.Plugins.IPluginPage;
    }

    private static UIElement? CreateContent(LenovoLegionToolkit.Lib.Plugins.IPluginPage? pluginPage)
    {
        if (pluginPage is null)
        {
            return null;
        }

        return pluginPage.CreatePage() as UIElement;
    }
}

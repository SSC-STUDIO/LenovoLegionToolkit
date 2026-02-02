using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Lib.Settings;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace LenovoLegionToolkit.Lib.Optimization;

public record WindowsOptimizationActionDefinition(
    string Key,
    string TitleResourceKey,
    string DescriptionResourceKey,
    Func<CancellationToken, Task> ExecuteAsync,
    bool Recommended = true,
    Func<CancellationToken, Task<bool>>? IsAppliedAsync = null);

public record WindowsOptimizationCategoryDefinition(
    string Key,
    string TitleResourceKey,
    string DescriptionResourceKey,
    IReadOnlyList<WindowsOptimizationActionDefinition> Actions,
    string? PluginId = null);

public class WindowsOptimizationService
{
    public const string CleanupCategoryKey = "cleanup";
    public const string CustomCleanupActionKey = "cleanup.custom";

    private readonly WindowsCleanupService _cleanupService;
    private readonly WindowsOptimizationCategoryProvider _categoryProvider;

    public WindowsOptimizationService(WindowsCleanupService cleanupService)
    {
        _cleanupService = cleanupService;
        _categoryProvider = new WindowsOptimizationCategoryProvider(this, cleanupService);
    }

    private IReadOnlyDictionary<string, WindowsOptimizationActionDefinition> GetActionsByKey()
    {
        return GetCategories()
            .SelectMany(category => category.Actions)
            .GroupBy(action => action.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<WindowsOptimizationCategoryDefinition> GetCategories()
    {
        var list = new List<WindowsOptimizationCategoryDefinition>(_categoryProvider.BuildCategories());
        
        try
        {
            var pluginManager = IoCContainer.Resolve<IPluginManager>();
            var installedPlugins = pluginManager.GetRegisteredPlugins()
                .Where(p => pluginManager.IsInstalled(p.Id));
            
            foreach (var plugin in installedPlugins)
            {
                try
                {
                    WindowsOptimizationCategoryDefinition? category = null;
                    
                    if (plugin is IOptimizationCategoryProvider provider)
                    {
                        category = provider.GetOptimizationCategory();
                    }
                    else if (plugin is PluginBase pluginBase)
                    {
                        category = pluginBase.GetOptimizationCategory();
                    }
                    
                    if (category != null)
                    {
                        if (string.IsNullOrEmpty(category.PluginId))
                        {
                            category = category with { PluginId = plugin.Id };
                        }
                        list.Add(category);
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to get optimization category from plugin {plugin.Id}: {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to get optimization categories from plugins", ex);
        }

        return list;
    }

    public async Task ApplyActionAsync(string actionKey, CancellationToken cancellationToken)
    {
        var actions = GetActionsByKey();
        if (actions.TryGetValue(actionKey, out var action))
        {
            await action.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<bool> IsActionAppliedAsync(string actionKey, CancellationToken cancellationToken)
    {
        var actions = GetActionsByKey();
        if (actions.TryGetValue(actionKey, out var action))
        {
            if (action.IsAppliedAsync is not null)
            {
                return await action.IsAppliedAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return false;
    }

    public async Task ExecuteActionsAsync(IEnumerable<string> actionKeys, CancellationToken cancellationToken)
    {
        if (actionKeys is null)
            return;

        var actionsByKey = GetActionsByKey();
        foreach (var key in actionKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!actionsByKey.TryGetValue(key, out var action))
                continue;

            try
            {
                await action.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Action execution failed. [key={key}]", ex);
                throw;
            }
        }
    }

    public Task ApplyPerformanceOptimizationsAsync(CancellationToken cancellationToken)
    {
        var keys = GetCategories()
            .Where(category => !string.Equals(category.Key, CleanupCategoryKey, StringComparison.OrdinalIgnoreCase))
            .SelectMany(category => category.Actions.Where(action => action.Recommended).Select(action => action.Key));

        return ExecuteActionsAsync(keys, cancellationToken);
    }

    public Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        var keys = GetCategories()
            .Where(category => category.Key.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase))
            .SelectMany(category => category.Actions.Where(action => action.Recommended).Select(action => action.Key));

        return ExecuteActionsAsync(keys, cancellationToken);
    }

    public Task<long> EstimateCleanupSizeAsync(IEnumerable<string> actionKeys, CancellationToken cancellationToken)
    {
        return _cleanupService.EstimateCleanupSizeAsync(actionKeys, cancellationToken);
    }

    public Task<long> EstimateActionSizeAsync(string actionKey, CancellationToken cancellationToken)
    {
        return _cleanupService.EstimateActionSizeAsync(actionKey, cancellationToken);
    }

    public Task<List<FileInfo>> GetLargeFilesAsync(long minSize, CancellationToken cancellationToken)
    {
        return _cleanupService.GetLargeFilesAsync(minSize, cancellationToken);
    }

    public async Task<bool?> TryGetActionAppliedAsync(string actionKey, CancellationToken cancellationToken)
    {
        var actionsByKey = GetActionsByKey();
        if (!actionsByKey.TryGetValue(actionKey, out var definition))
            return null;

        if (definition.IsAppliedAsync is null)
            return null;

        try
        {
            return await definition.IsAppliedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to evaluate optimization action state. [action={actionKey}]", ex);
            return null;
        }
    }

    internal WindowsOptimizationActionDefinition CreateRegistryAction(
        string key,
        string titleResourceKey,
        string descriptionResourceKey,
        IReadOnlyList<RegistryValueDefinition> tweaks,
        bool recommended = true) =>
        new(
            key,
            titleResourceKey,
            descriptionResourceKey,
            ct => ApplyRegistryTweaksAsync(ct, tweaks),
            recommended,
            ct => Task.FromResult(WindowsOptimizationHelper.AreRegistryTweaksApplied(tweaks)));

    internal WindowsOptimizationActionDefinition CreateServiceAction(
        string key,
        string titleResourceKey,
        string descriptionResourceKey,
        IReadOnlyList<string> services,
        bool recommended = true) =>
        new(
            key,
            titleResourceKey,
            descriptionResourceKey,
            ct => DisableServicesAsync(ct, services),
            recommended,
            ct => Task.FromResult(WindowsOptimizationHelper.AreServicesDisabled(services)));

    internal WindowsOptimizationActionDefinition CreateCommandAction(
        string key,
        string titleResourceKey,
        string descriptionResourceKey,
        IReadOnlyList<string> commands,
        bool recommended = true) =>
        new(
            key,
            titleResourceKey,
            descriptionResourceKey,
            ct => ExecuteCommandsSequentiallyAsync(ct, commands.ToArray()),
            recommended);

    private Task ApplyRegistryTweaksAsync(CancellationToken cancellationToken, IEnumerable<RegistryValueDefinition> tweaks)
    {
        foreach (var tweak in tweaks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WindowsOptimizationHelper.ApplyRegistryTweak(tweak);
        }

        return Task.CompletedTask;
    }

    private Task DisableServicesAsync(CancellationToken cancellationToken, IEnumerable<string> services)
    {
        foreach (var serviceName in services.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            WindowsOptimizationHelper.DisableService(serviceName);
        }

        return Task.CompletedTask;
    }

    private async Task ExecuteCommandsSequentiallyAsync(CancellationToken cancellationToken, params string[] commands)
    {
        foreach (var command in commands)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ExecuteCommandLineAsync(command, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteCommandLineAsync(string command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be null or empty.", nameof(command));

        try
        {
            var parts = command.Split(' ', 2);
            var fileName = parts[0];
            var arguments = parts.Length > 1 ? parts[1] : string.Empty;

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName.Equals("del", StringComparison.OrdinalIgnoreCase) || 
                               fileName.Equals("rd", StringComparison.OrdinalIgnoreCase) ||
                               fileName.Equals("powercfg", StringComparison.OrdinalIgnoreCase) ||
                               fileName.Equals("ipconfig", StringComparison.OrdinalIgnoreCase) ||
                               fileName.Equals("netsh", StringComparison.OrdinalIgnoreCase) ||
                               fileName.Equals("dism", StringComparison.OrdinalIgnoreCase) 
                               ? fileName : "cmd.exe",
                    Arguments = (fileName.Equals("del", StringComparison.OrdinalIgnoreCase) || 
                                 fileName.Equals("rd", StringComparison.OrdinalIgnoreCase))
                                 ? "/c " + command : arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            if (fileName.Equals("powercfg", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("ipconfig", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("netsh", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("dism", StringComparison.OrdinalIgnoreCase))
            {
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
            }
            else if (!fileName.Equals("cmd.exe", StringComparison.OrdinalIgnoreCase))
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c " + command;
            }

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await Task.WhenAll(process.WaitForExitAsync(cancellationToken), outputTask, errorTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to execute command. [command={command}]", ex);
        }
    }

    internal async Task ExecuteStartMenuDisableAsync(CancellationToken cancellationToken)
    {
        foreach (var tweak in WindowsOptimizationDefinitions.StartMenuDisableTweaks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WindowsOptimizationHelper.ApplyRegistryTweak(tweak);
        }

        NotifyExplorerSettingsChanged();
        RestartExplorer();
        await Task.CompletedTask;
    }

    internal bool AreStartMenuTweaksApplied()
    {
        return WindowsOptimizationHelper.AreRegistryTweaksApplied(WindowsOptimizationDefinitions.StartMenuDisableTweaks);
    }

    private static unsafe void NotifyExplorerSettingsChanged()
    {
        try
        {
            const string policy = "Policy";
            fixed (void* ptr = policy)
            {
                PInvoke.SendNotifyMessage(HWND.HWND_BROADCAST, PInvoke.WM_SETTINGCHANGE, 0, new IntPtr(ptr));
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to notify Explorer of settings change.", ex);
        }
    }

    private static void RestartExplorer()
    {
        try
        {
            var killInfo = new ProcessStartInfo
            {
                FileName = "taskkill.exe",
                Arguments = "/f /im explorer.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var killProcess = Process.Start(killInfo);
            killProcess?.WaitForExit(5000);

            var startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to restart Explorer.", ex);
        }
    }
}

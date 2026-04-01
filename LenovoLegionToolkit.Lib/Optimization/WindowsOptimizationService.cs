using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text.RegularExpressions;
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

/// <summary>
/// Service for executing Windows optimization commands with strict security validation.
/// Prevents command injection attacks through whitelist-based command validation.
/// </summary>
public class WindowsOptimizationService
{
    public const string CleanupCategoryKey = "cleanup";
    public const string CustomCleanupActionKey = "cleanup.custom";

    // Whitelist of allowed executables for command execution
    private static readonly HashSet<string> AllowedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "powercfg",      // Power configuration
        "ipconfig",      // Network configuration
        "netsh",         // Network shell
        "dism",          // Deployment Image Servicing and Management
        "del",           // Delete files (restricted)
        "rd",            // Remove directory (restricted)
        "cmd.exe",       // Command prompt (with validation)
        "reg",           // Registry operations
        "schtasks",      // Task scheduler
        "sc",            // Service control
        "wevtutil",      // Windows Event Utility
        "cleanmgr",      // Disk cleanup
    };

    // Commands that require special argument validation
    private static readonly HashSet<string> HighRiskCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "del",
        "rd",
        "cmd.exe",
        "reg"
    };

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
        // Validate action key to prevent injection
        if (!IsValidActionKey(actionKey))
            throw new ArgumentException("Invalid action key", nameof(actionKey));

        var actions = GetActionsByKey();
        if (actions.TryGetValue(actionKey, out var action))
        {
            await action.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<bool> IsActionAppliedAsync(string actionKey, CancellationToken cancellationToken)
    {
        // Validate action key to prevent injection
        if (!IsValidActionKey(actionKey))
            throw new ArgumentException("Invalid action key", nameof(actionKey));

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
            // Validate each action key
            if (!IsValidActionKey(key))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Skipping invalid action key: {key}");
                continue;
            }

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
        // Validate action key
        if (!IsValidActionKey(actionKey))
            throw new ArgumentException("Invalid action key", nameof(actionKey));

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

    /// <summary>
    /// Creates a command action with strict validation.
    /// Commands are validated against an allowlist before execution.
    /// </summary>
    internal WindowsOptimizationActionDefinition CreateCommandAction(
        string key,
        string titleResourceKey,
        string descriptionResourceKey,
        IReadOnlyList<string> commands,
        bool recommended = true)
    {
        // Validate all commands at creation time
        foreach (var command in commands)
        {
            if (!IsValidCommand(command))
                throw new ArgumentException($"Invalid or unsafe command: {command}", nameof(commands));
        }

        return new(
            key,
            titleResourceKey,
            descriptionResourceKey,
            ct => ExecuteCommandsSequentiallyAsync(ct, commands.ToArray()),
            recommended);
    }

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
            
            // Validate service name
            if (!IsValidServiceName(serviceName))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Skipping invalid service name: {serviceName}");
                continue;
            }
            
            WindowsOptimizationHelper.DisableService(serviceName);
        }

        return Task.CompletedTask;
    }

    private async Task ExecuteCommandsSequentiallyAsync(CancellationToken cancellationToken, params string[] commands)
    {
        foreach (var command in commands)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Validate command before execution
            if (!IsValidCommand(command))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Skipping invalid command: {command}");
                continue;
            }
            
            await ExecuteCommandLineAsync(command, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes a command line with strict security validation.
    /// Uses parameterized execution instead of string concatenation.
    /// </summary>
    private async Task ExecuteCommandLineAsync(string command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be null or empty.", nameof(command));

        // Validate command before execution
        if (!IsValidCommand(command))
        {
            throw new InvalidOperationException($"Command failed security validation: {command}");
        }

        try
        {
            // Parse command using proper argument parsing instead of simple split
            var (fileName, arguments) = ParseCommandLine(command);
            
            // Double-check the parsed command
            if (!IsAllowedExecutable(fileName))
            {
                throw new InvalidOperationException($"Executable not in allowlist: {fileName}");
            }

            // Build process start info with parameterized arguments
            var startInfo = BuildProcessStartInfo(fileName, arguments);

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await Task.WhenAll(process.WaitForExitAsync(cancellationToken), outputTask, errorTask).ConfigureAwait(false);
            
            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"Command executed successfully: {fileName}");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to execute command. [command={command}]", ex);
            throw;
        }
    }

    /// <summary>
    /// Parses a command line string into filename and arguments.
    /// Handles quoted paths correctly.
    /// </summary>
    private static (string fileName, string arguments) ParseCommandLine(string command)
    {
        command = command.Trim();
        
        string fileName;
        string arguments;

        if (command.StartsWith("\"", StringComparison.Ordinal))
        {
            // Quoted path
            var endQuote = command.IndexOf('\"', 1);
            if (endQuote == -1)
            {
                // No closing quote, treat entire command as filename
                fileName = command.Trim('\"');
                arguments = string.Empty;
            }
            else
            {
                fileName = command.Substring(1, endQuote - 1);
                arguments = command.Substring(endQuote + 1).Trim();
            }
        }
        else
        {
            // Unquoted - find first space separator
            var firstSpace = command.IndexOf(' ');
            if (firstSpace == -1)
            {
                fileName = command;
                arguments = string.Empty;
            }
            else
            {
                fileName = command.Substring(0, firstSpace);
                arguments = command.Substring(firstSpace + 1).Trim();
            }
        }

        return (fileName, arguments);
    }

    /// <summary>
    /// Builds ProcessStartInfo with security settings.
    /// </summary>
    private static ProcessStartInfo BuildProcessStartInfo(string fileName, string arguments)
    {
        var isHighRisk = HighRiskCommands.Contains(Path.GetFileNameWithoutExtension(fileName));
        
        // For high-risk commands, validate arguments more strictly
        if (isHighRisk && !string.IsNullOrEmpty(arguments))
        {
            ValidateHighRiskArguments(fileName, arguments);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Additional security: don't load user profile for high-risk commands
        if (isHighRisk)
        {
            startInfo.LoadUserProfile = false;
        }

        return startInfo;
    }

    /// <summary>
    /// Validates arguments for high-risk commands.
    /// </summary>
    private static void ValidateHighRiskArguments(string fileName, string arguments)
    {
        // Check for dangerous patterns in arguments
        if (CommandInjectionValidator.ContainsDangerousPatterns(arguments))
        {
            throw new InvalidOperationException($"Dangerous pattern detected in arguments for {fileName}");
        }

        // Additional validation for specific commands
        var executable = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        
        switch (executable)
        {
            case "del":
            case "rd":
                ValidateDeleteCommandArguments(arguments);
                break;
            case "reg":
                ValidateRegCommandArguments(arguments);
                break;
        }
    }

    /// <summary>
    /// Validates delete command arguments to prevent deletion of system files.
    /// </summary>
    private static void ValidateDeleteCommandArguments(string arguments)
    {
        // Block deletion of system directories
        var systemPaths = new[] 
        { 
            @"c:\windows", @"c:\program files", @"c:\programdata",
            @"c:\users\", @"c:\system volume information", @"c:\$recycle.bin"
        };

        var lowerArgs = arguments.ToLowerInvariant();
        
        foreach (var sysPath in systemPaths)
        {
            if (lowerArgs.Contains(sysPath))
            {
                throw new InvalidOperationException("Deletion of system paths is not allowed");
            }
        }

        // Block wildcards that could match system files
        if (arguments.Contains("*.*") && !arguments.Contains("?") && !arguments.Contains("\\temp"))
        {
            throw new InvalidOperationException("Wildcard deletion is restricted");
        }
    }

    /// <summary>
    /// Validates registry command arguments.
    /// </summary>
    private static void ValidateRegCommandArguments(string arguments)
    {
        var lowerArgs = arguments.ToLowerInvariant();
        
        // Block deletion of critical registry keys
        var criticalKeys = new[]
        {
            @"hkey_local_machine\system",
            @"hkey_local_machine\software\microsoft\windows",
            @"hkey_current_user\software\microsoft\windows"
        };

        foreach (var key in criticalKeys)
        {
            if (lowerArgs.Contains(key) && lowerArgs.Contains("delete"))
            {
                throw new InvalidOperationException("Deletion of critical registry keys is not allowed");
            }
        }
    }

    /// <summary>
    /// Validates if a command is safe to execute.
    /// </summary>
    public static bool IsValidCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        // Check for command injection patterns
        if (CommandInjectionValidator.ContainsDangerousPatterns(command))
            return false;

        // Parse and validate the executable
        var (fileName, _) = ParseCommandLine(command);
        
        if (!IsAllowedExecutable(fileName))
            return false;

        return true;
    }

    /// <summary>
    /// Checks if an executable is in the allowlist.
    /// </summary>
    private static bool IsAllowedExecutable(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // Get just the filename without path
        var name = Path.GetFileNameWithoutExtension(fileName);
        
        return AllowedCommands.Contains(name) || 
               AllowedCommands.Contains(fileName);
    }

    /// <summary>
    /// Validates action key format to prevent injection.
    /// </summary>
    private static bool IsValidActionKey(string actionKey)
    {
        if (string.IsNullOrWhiteSpace(actionKey))
            return false;

        // Only allow alphanumeric, dots, dashes, and underscores
        // Pattern: ^[a-zA-Z0-9._-]+$
        return Regex.IsMatch(actionKey, @"^[a-zA-Z0-9._-]+$");
    }

    /// <summary>
    /// Validates service name format.
    /// </summary>
    private static bool IsValidServiceName(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            return false;

        // Service names should be alphanumeric with limited special chars
        return Regex.IsMatch(serviceName, @"^[a-zA-Z0-9_-]+$");
    }

    internal async Task ExecuteStartMenuDisableAsync(CancellationToken cancellationToken)
    {
        foreach (var tweak in WindowsOptimizationDefinitions.StartMenuDisableTweaks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WindowsOptimizationHelper.ApplyRegistryTweak(tweak);
        }

        NotifyExplorerSettingsChanged();
        await ExplorerRestartHelper.RestartAsync().ConfigureAwait(false);
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
}

/// <summary>
/// Validator for detecting command injection attempts.
/// </summary>
public static class CommandInjectionValidator
{
    // Dangerous patterns that could indicate command injection
    private static readonly string[] DangerousPatterns = new[]
    {
        "&&",      // Command chaining
        "||",      // Command chaining
        "|",       // Pipe (check individually for non-redirection cases)
        ";",       // Command separator
        "`",       // PowerShell execution
        "$(",      // Command substitution
        "..",      // Directory traversal
        "../",     // Directory traversal
        "..\\",    // Directory traversal
        "%00",     // Null byte injection
        "${",      // Shell variable expansion
        "<(",      // Process substitution
    };

    // Regex patterns for more complex detection
    private static readonly Regex[] DangerousRegexPatterns = new[]
    {
        // PowerShell encoding/execution patterns
        new Regex(@"-[eE][nN][cC]?\s+", RegexOptions.Compiled),
        // Base64 encoded commands
        new Regex(@"-[eE][nN][cC]?\s+[a-zA-Z0-9+/]{100,}={0,2}", RegexOptions.Compiled),
        // Command substitution in PowerShell
        new Regex(@"\$\([^)]+\)", RegexOptions.Compiled),
        // IEX/Invoke-Expression patterns
        new Regex(@"[iI][eE][xX]|[iI]nvoke-[eE]xpression", RegexOptions.Compiled),
    };

    /// <summary>
    /// Checks if input contains dangerous patterns that could indicate command injection.
    /// </summary>
    public static bool ContainsDangerousPatterns(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // Check simple patterns
        foreach (var pattern in DangerousPatterns)
        {
            if (input.Contains(pattern, StringComparison.Ordinal))
                return true;
        }

        // Check regex patterns
        foreach (var regex in DangerousRegexPatterns)
        {
            if (regex.IsMatch(input))
                return true;
        }

        // Check for single ampersand (command separator, not redirection)
        if (ContainsUnescapedAmpersand(input))
            return true;

        return false;
    }

    /// <summary>
    /// Checks for command separator ampersands (not redirection patterns like 2>&1).
    /// </summary>
    private static bool ContainsUnescapedAmpersand(string input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '&')
            {
                // Skip escaped ampersands (^&)
                if (i > 0 && input[i - 1] == '^')
                    continue;

                // Check if this is part of a redirection pattern (>&1, >&2, 2>&1, 1>&2)
                bool isRedirection = false;

                // Check for >&N pattern
                if (i > 0 && input[i - 1] == '>')
                {
                    if (i + 1 < input.Length && (input[i + 1] == '1' || input[i + 1] == '2'))
                    {
                        isRedirection = true;
                    }
                }
                // Check for N>&M pattern
                else if (i > 0 && (input[i - 1] == '1' || input[i - 1] == '2') && i > 1 && input[i - 2] == '>')
                {
                    if (i + 1 < input.Length && (input[i + 1] == '1' || input[i + 1] == '2'))
                    {
                        isRedirection = true;
                    }
                }
                // Check for >& (implicit descriptor)
                else if (i > 0 && input[i - 1] == '>' && (i + 1 >= input.Length || char.IsWhiteSpace(input[i + 1]) || input[i + 1] == '1' || input[i + 1] == '2'))
                {
                    isRedirection = true;
                }

                if (!isRedirection)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sanitizes input by removing dangerous characters.
    /// </summary>
    public static string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sanitized = input;
        
        foreach (var pattern in DangerousPatterns)
        {
            sanitized = sanitized.Replace(pattern, string.Empty, StringComparison.Ordinal);
        }

        return sanitized;
    }
}

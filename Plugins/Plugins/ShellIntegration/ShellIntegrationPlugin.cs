using System;
using System.Diagnostics;
#nullable enable

using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Plugins.Shared;
using LenovoLegionToolkit.Plugins.ShellIntegration.Resources;
using LenovoLegionToolkit.Plugins.SDK;
using Microsoft.Win32;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

[Plugin(
    id: "shell-integration",
    name: "Shell Integration",
    version: "1.0.10",
    description: "Integrate Lenovo Legion Toolkit with Windows shell context menu",
    author: "SSC-STUDIO",
    MinimumHostVersion = "3.6.1",
    Icon = "Folder24"
)]
public class ShellIntegrationPlugin : LenovoLegionToolkit.Plugins.SDK.PluginBase
{
    private const string PluginId = "shell-integration";
    private const string ShellClsid = "{BAE3934B-8A6A-4BFB-81BD-3FC599A1BAF1}";
    private const string DisabledClsid = "{00000000-0000-0000-0000-000000000000}";
    private static readonly TimeSpan ShellCommandTimeout = TimeSpan.FromSeconds(Constants.ProcessTimeoutSeconds);
    private static readonly string GlobalLanguagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LenovoLegionToolkit", "lang");

    private static readonly string[] ShellExeCandidates =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Nilesoft Shell", "shell.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Nilesoft Shell", "shell.exe")
    ];

    private static readonly string[] ShellDllCandidates =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Nilesoft Shell", "shell.dll"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Nilesoft Shell", "shell.dll")
    ];

    private static readonly string[] ShellContextHandlerParentSubKeys =
    [
        @"*\shellex\ContextMenuHandlers",
        @"DesktopBackground\shellex\ContextMenuHandlers",
        @"Directory\background\shellex\ContextMenuHandlers",
        @"Directory\shellex\ContextMenuHandlers",
        @"Drive\shellex\ContextMenuHandlers",
        @"Folder\ShellEx\ContextMenuHandlers",
        @"LibraryFolder\background\shellex\ContextMenuHandlers",
        @"LibraryFolder\ShellEx\ContextMenuHandlers"
    ];

    private readonly ShellIntegrationConfigService _configService = new();

    public override string Id => PluginId;
    public override string Name => ShellIntegrationText.PluginName;
    public override string Description => ShellIntegrationText.PluginDescription;
    public override string Icon => "Folder24";
    public override bool IsSystemPlugin => true;

    public override object? GetSettingsPage()
    {
        return new ShellIntegrationSettingsPluginPage(this);
    }

    public override WindowsOptimizationCategoryDefinition? GetOptimizationCategory()
    {
        return new WindowsOptimizationCategoryDefinition(
            "shell.integration",
            "WindowsOptimization_Category_NilesoftShell_Title",
            "WindowsOptimization_Category_NilesoftShell_Description",
            new[]
            {
                new WindowsOptimizationActionDefinition(
                    "shell.integration.enable",
                    "WindowsOptimization_Action_NilesoftShell_Enable_Title",
                    "WindowsOptimization_Action_NilesoftShell_Enable_Description",
                    ct => EnableShellAsync(ct),
                    Recommended: true,
                    IsAppliedAsync: IsShellRegisteredAsync),
                new WindowsOptimizationActionDefinition(
                    "shell.integration.disable",
                    "WindowsOptimization_Action_NilesoftShell_Disable_Title",
                    "WindowsOptimization_Action_NilesoftShell_Disable_Description",
                    ct => DisableShellAsync(ct),
                    Recommended: false,
                    IsAppliedAsync: async ct => !await IsShellRegisteredAsync(ct).ConfigureAwait(false))
            },
            Id);
    }

    public bool IsShellInstalled()
    {
        return GetShellInstallPath() is not null;
    }

    public bool IsShellRegistered()
    {
        return IsShellRegisteredInMergedClasses();
    }

    public string? GetShellExePath()
    {
        var bundled = GetBundledShellExePath();
        if (!string.IsNullOrWhiteSpace(bundled))
            return bundled;

        foreach (var candidate in ShellExeCandidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    public string? GetShellDllPath()
    {
        var bundled = GetBundledShellDllPath();
        if (!string.IsNullOrWhiteSpace(bundled))
            return bundled;

        foreach (var candidate in ShellDllCandidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    public string? GetShellInstallPath()
    {
        return GetShellExePath() ?? GetShellDllPath();
    }

    public string? GetShellFolderPath()
    {
        var path = GetShellExePath() ?? GetShellDllPath();
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return Path.GetDirectoryName(path);
    }

    public string? GetShellConfigPath()
    {
        var folder = GetShellFolderPath();
        if (string.IsNullOrWhiteSpace(folder))
            return null;

        return Path.Combine(folder, "shell.nss");
    }

    public string? GetShellVersion()
    {
        var path = GetShellExePath() ?? GetShellDllPath();
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var info = FileVersionInfo.GetVersionInfo(path);
            return info.FileVersion ?? info.ProductVersion;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetBundledShellExePath()
    {
        var baseDir = GetPluginBaseDirectory();
        if (string.IsNullOrWhiteSpace(baseDir))
            return null;

        var candidate = Path.Combine(baseDir, "shell.exe");
        return File.Exists(candidate) ? candidate : null;
    }

    private static string? GetBundledShellDllPath()
    {
        var baseDir = GetPluginBaseDirectory();
        if (string.IsNullOrWhiteSpace(baseDir))
            return null;

        var candidate = Path.Combine(baseDir, "shell.dll");
        return File.Exists(candidate) ? candidate : null;
    }

    private static string? GetPluginBaseDirectory()
    {
        try
        {
            var location = typeof(ShellIntegrationPlugin).Assembly.Location;
            if (string.IsNullOrWhiteSpace(location))
                return null;

            return Path.GetDirectoryName(location);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> EnableShellAsync()
    {
        try
        {
            await EnableShellAsync(CancellationToken.None).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"ShellIntegration: Enable failed: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> DisableShellAsync()
    {
        try
        {
            await DisableShellAsync(CancellationToken.None).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"ShellIntegration: Disable failed: {ex.Message}", ex);
            return false;
        }
    }

    public void OpenStyleSettingsWindow()
    {
        var dialog = SDK.PluginHostContext.CreateHostWindow("LenovoLegionToolkit.WPF.Windows.Utils.MenuStyleSettingsWindow");
        if (dialog is not null && SDK.PluginHostContext.Current.ShowDialog(dialog, ShellIntegrationText.SettingsPageTitle))
            return;

        SDK.PluginHostContext.Current.ShowDialog(new ShellIntegrationStyleSettingsWindow(this), ShellIntegrationText.SettingsPageTitle);
    }

    public bool OpenShellFolder()
    {
        return TryOpenShellPath(GetShellFolderPath());
    }

    public bool OpenShellConfigFile()
    {
        return TryOpenShellPath(GetShellConfigPath());
    }

    private static bool TryOpenShellPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task EnableShellAsync(CancellationToken cancellationToken)
    {
        EnsureManagedConfigurationSynchronized();

        if (!string.IsNullOrWhiteSpace(GetShellExePath()))
        {
            await RunShellCommandAsync("-register -treat -restart", cancellationToken).ConfigureAwait(false);
            EnsureManagedConfigurationSynchronized();
            return;
        }

        await ApplyShellRegistryOverrideAsync(enable: true, cancellationToken).ConfigureAwait(false);
        EnsureManagedConfigurationSynchronized();
    }

    private async Task DisableShellAsync(CancellationToken cancellationToken)
    {
        EnsureManagedConfigurationSynchronized();

        if (!string.IsNullOrWhiteSpace(GetShellExePath()))
        {
            await RunShellCommandAsync("-unregister -restart", cancellationToken).ConfigureAwait(false);
            EnsureManagedConfigurationSynchronized();
            return;
        }

        await ApplyShellRegistryOverrideAsync(enable: false, cancellationToken).ConfigureAwait(false);
        EnsureManagedConfigurationSynchronized();
    }

    private async Task<bool> IsShellRegisteredAsync(CancellationToken cancellationToken)
    {
        if (!IsShellInstalled())
            return false;

        if (IsShellRegisteredInMergedClasses())
            return true;

        if (string.IsNullOrWhiteSpace(GetShellExePath()))
            return false;

        var commandResult = await RunShellCommandAsync("-query", cancellationToken, swallowErrors: true).ConfigureAwait(false);
        return ParseShellRegistrationStatus(commandResult) ?? IsShellRegisteredInMergedClasses();
    }

    private async Task<string> RunShellCommandAsync(string arguments, CancellationToken cancellationToken, bool swallowErrors = false)
    {
        var shellExePath = GetShellExePath();
        if (string.IsNullOrWhiteSpace(shellExePath))
        {
            if (swallowErrors)
                return string.Empty;

            throw new InvalidOperationException("shell.exe was not found.");
        }

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = shellExePath,
                    Arguments = arguments,
                    WorkingDirectory = Path.GetDirectoryName(shellExePath) ?? Environment.CurrentDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ShellCommandTimeout);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (swallowErrors)
            {
                TryTerminateProcess(process);
                return string.Empty;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                TryTerminateProcess(process);
                throw;
            }
            catch (OperationCanceledException)
            {
                TryTerminateProcess(process);
                throw new TimeoutException($"shell.exe command timed out after {ShellCommandTimeout.TotalSeconds:0} seconds.");
            }

            var output = await outputTask.ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);

            if (process.ExitCode != 0 && !swallowErrors)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "shell.exe command failed." : error);
            }

            return string.IsNullOrWhiteSpace(output) ? error : output;
        }
        catch when (swallowErrors)
        {
            return string.Empty;
        }
    }

    private static bool? ParseShellRegistrationStatus(string? commandResult)
    {
        if (string.IsNullOrWhiteSpace(commandResult))
            return null;

        var sawPositiveSignal = false;
        foreach (var rawLine in commandResult.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            var lowerLine = line.ToLowerInvariant();
            if (!ContainsRegistrationKeyword(lowerLine))
                continue;

            if (ContainsNegativeRegistrationState(lowerLine))
                return false;

            if (ContainsPositiveRegistrationState(lowerLine))
                sawPositiveSignal = true;
        }

        return sawPositiveSignal ? true : null;
    }

    private static bool ContainsRegistrationKeyword(string line)
    {
        return line.Contains("registered", StringComparison.Ordinal) ||
               line.Contains("registration", StringComparison.Ordinal) ||
               line.Contains("enabled", StringComparison.Ordinal) ||
               line.Contains("active", StringComparison.Ordinal);
    }

    private static bool ContainsNegativeRegistrationState(string line)
    {
        return line.Contains("not registered", StringComparison.Ordinal) ||
               line.Contains("unregistered", StringComparison.Ordinal) ||
               line.Contains("disabled", StringComparison.Ordinal) ||
               line.Contains("inactive", StringComparison.Ordinal) ||
               line.Contains("not active", StringComparison.Ordinal) ||
               line.Contains("not enabled", StringComparison.Ordinal) ||
               line.Contains(": false", StringComparison.Ordinal) ||
               line.Contains("= false", StringComparison.Ordinal) ||
               line.EndsWith(" false", StringComparison.Ordinal) ||
               line.Contains(": no", StringComparison.Ordinal) ||
               line.Contains("= no", StringComparison.Ordinal) ||
               line.EndsWith(" no", StringComparison.Ordinal) ||
               line.Contains(": off", StringComparison.Ordinal) ||
               line.Contains("= off", StringComparison.Ordinal) ||
               line.EndsWith(" off", StringComparison.Ordinal);
    }

    private static bool ContainsPositiveRegistrationState(string line)
    {
        return line.Contains("registered", StringComparison.Ordinal) ||
               line.Contains("enabled", StringComparison.Ordinal) ||
               line.Contains("active", StringComparison.Ordinal) ||
               line.Contains(": true", StringComparison.Ordinal) ||
               line.Contains("= true", StringComparison.Ordinal) ||
               line.EndsWith(" true", StringComparison.Ordinal) ||
               line.Contains(": yes", StringComparison.Ordinal) ||
               line.Contains("= yes", StringComparison.Ordinal) ||
               line.EndsWith(" yes", StringComparison.Ordinal) ||
               line.Contains(": on", StringComparison.Ordinal) ||
               line.Contains("= on", StringComparison.Ordinal) ||
               line.EndsWith(" on", StringComparison.Ordinal);
    }

    private static void TryTerminateProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best effort cleanup only.
        }
    }

    private static bool IsShellRegisteredInMergedClasses()
    {
        return ShellContextHandlerParentSubKeys.All(parentSubKey =>
        {
            using var key = OpenMergedHandlerKey(parentSubKey);
            var value = Convert.ToString(key?.GetValue(string.Empty)) ?? string.Empty;
            return value.Equals(ShellClsid, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static Task ApplyShellRegistryOverrideAsync(bool enable, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string classesRoot = @"Software\Classes";

        foreach (var parentSubKey in ShellContextHandlerParentSubKeys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (enable)
            {
                DeleteUserOverrideKeyIfExists($@"{classesRoot}\{parentSubKey}\ @nilesoft.shell");
                DeleteUserOverrideKeyIfExists($@"{classesRoot}\{parentSubKey}\@nilesoft.shell");
            }
            else
            {
                SetUserOverrideValue($@"{classesRoot}\{parentSubKey}\ @nilesoft.shell", DisabledClsid);
                SetUserOverrideValue($@"{classesRoot}\{parentSubKey}\@nilesoft.shell", DisabledClsid);
            }
        }

        return Task.CompletedTask;
    }

    private static RegistryKey? OpenMergedHandlerKey(string parentSubKey)
    {
        return Registry.ClassesRoot.OpenSubKey($@"{parentSubKey}\ @nilesoft.shell", false)
               ?? Registry.ClassesRoot.OpenSubKey($@"{parentSubKey}\@nilesoft.shell", false);
    }

    private static void SetUserOverrideValue(string userSubKey, string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(userSubKey, true)
                       ?? throw new InvalidOperationException($"Failed to create registry key: HKCU\\{userSubKey}");
        key.SetValue(string.Empty, value, RegistryValueKind.String);
    }

    private static void DeleteUserOverrideKeyIfExists(string userSubKey)
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(userSubKey, false);
        }
        catch (ArgumentException)
        {
            // Ignore missing keys when enabling.
        }
    }

    public bool SyncManagedConfiguration()
    {
        var shellInstallPath = GetShellInstallPath();
        if (string.IsNullOrWhiteSpace(shellInstallPath))
            return false;

        if (!_configService.TryLoadProfile(out var profile, out var errorMessage))
        {
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"ShellIntegration: Failed to load profile: {errorMessage}");
            return false;
        }

        try
        {
            return _configService.ApplyProfile(shellInstallPath, profile, ResolveManagedCulture()) is not null;
        }
        catch (Exception ex)
        {
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"ShellIntegration: Failed to sync managed configuration: {ex.Message}", ex);
            return false;
        }
    }

    private void EnsureManagedConfigurationSynchronized()
    {
        if (!SyncManagedConfiguration())
            throw new InvalidOperationException("Failed to synchronize managed shell configuration.");
    }

    private static CultureInfo? ResolveManagedCulture()
    {
        if (Resource.Culture is not null)
            return Resource.Culture;

        try
        {
            if (!File.Exists(GlobalLanguagePath))
                return null;

            var name = File.ReadAllText(GlobalLanguagePath).Trim();
            return string.IsNullOrWhiteSpace(name) ? null : new CultureInfo(name);
        }
        catch
        {
            return null;
        }
    }
}

public class ShellIntegrationSettingsPluginPage : LenovoLegionToolkit.Plugins.SDK.IPluginPage
{
    private readonly ShellIntegrationPlugin _plugin;

    public ShellIntegrationSettingsPluginPage(ShellIntegrationPlugin plugin)
    {
        _plugin = plugin;
    }

    public string PageTitle => ShellIntegrationText.SettingsPageTitle;
    public string? PageIcon => "Settings24";

    public object CreatePage()
    {
        return new ShellIntegrationSettingsControl(_plugin);
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Plugins.SDK;
using LenovoLegionToolkit.WPF.Windows.Utils;
using Microsoft.Win32;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

[Plugin(
    id: "shell-integration",
    name: "Shell Integration",
    version: "1.0.3",
    description: "Integrate Lenovo Legion Toolkit with Windows shell context menu",
    author: "LenovoLegionToolkit Team",
    MinimumHostVersion = "3.6.1",
    Icon = "Folder24"
)]
public class ShellIntegrationPlugin : LenovoLegionToolkit.Plugins.SDK.PluginBase
{
    private const string ShellClsid = "{BAE3934B-8A6A-4BFB-81BD-3FC599A1BAF1}";
    private const string DisabledClsid = "{00000000-0000-0000-0000-000000000000}";

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

    public override string Id => "shell-integration";
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

    public string? GetShellExePath()
    {
        foreach (var candidate in ShellExeCandidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    public string? GetShellDllPath()
    {
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

    public async Task<bool> EnableShellAsync()
    {
        try
        {
            await EnableShellAsync(CancellationToken.None).ConfigureAwait(false);
            return true;
        }
        catch
        {
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
        catch
        {
            return false;
        }
    }

    public void OpenStyleSettingsWindow()
    {
        if (Application.Current?.Dispatcher == null)
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            var window = new MenuStyleSettingsWindow();
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
                window.Owner = mainWindow;
            window.ShowDialog();
        });
    }

    private async Task EnableShellAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(GetShellExePath()))
        {
            await RunShellCommandAsync("-register -treat -restart", cancellationToken).ConfigureAwait(false);
            return;
        }

        await ApplyShellRegistryOverrideAsync(enable: true, cancellationToken).ConfigureAwait(false);
    }

    private async Task DisableShellAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(GetShellExePath()))
        {
            await RunShellCommandAsync("-unregister -restart", cancellationToken).ConfigureAwait(false);
            return;
        }

        await ApplyShellRegistryOverrideAsync(enable: false, cancellationToken).ConfigureAwait(false);
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
        if (string.IsNullOrWhiteSpace(commandResult))
            return IsShellRegisteredInMergedClasses();

        return commandResult.Contains("register", StringComparison.OrdinalIgnoreCase) ||
               commandResult.Contains("enabled", StringComparison.OrdinalIgnoreCase) ||
               commandResult.Contains("active", StringComparison.OrdinalIgnoreCase);
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

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
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

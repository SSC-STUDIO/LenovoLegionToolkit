using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Microsoft.Win32;

namespace UniversalDeviceToolkit.Installer;

internal sealed class EngineProgress
{
    public double? Percent { get; init; }
    public required string Status { get; init; }
}

internal sealed class InstallOptions
{
    public required string InstallDir { get; init; }
    public bool CreateDesktopShortcut { get; init; }
    public bool LaunchAfterInstall { get; init; }
}

internal sealed class UninstallOptions
{
    public required string InstallDir { get; init; }
    public bool DeleteAppData { get; init; }
}

internal static class InstallerEngine
{
    private const string EmbeddedPayloadResourceName = "UniversalDeviceToolkit.Installer.payload.zip";

    /// <summary>True for the Full (offline) flavor with the payload zip embedded.</summary>
    public static bool HasEmbeddedPayload =>
        typeof(InstallerEngine).Assembly.GetManifestResourceInfo(EmbeddedPayloadResourceName) is not null;

    // ---------- .NET Desktop Runtime ----------

    public static bool TryGetDesktopRuntime(out Version? version)
    {
        version = null;
        var dotnetPath = FindDotNetHost();
        if (dotnetPath is null)
            return false;

        try
        {
            var psi = new ProcessStartInfo(dotnetPath, "--list-runtimes")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(psi);
            if (process is null)
                return false;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(15000);

            Version? best = null;
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                // "Microsoft.WindowsDesktop.App 10.0.7 [C:\...\shared\...]"
                if (!line.StartsWith(InstallerConstants.DotNetRuntimeName + " ", StringComparison.Ordinal))
                    continue;

                var versionText = line[InstallerConstants.DotNetRuntimeName.Length..].TrimStart();
                var space = versionText.IndexOf(' ');
                if (space > 0)
                    versionText = versionText[..space];

                if (Version.TryParse(versionText, out var parsed) &&
                    parsed.Major == InstallerConstants.DotNetRuntimeMajor &&
                    (best is null || parsed > best))
                {
                    best = parsed;
                }
            }

            version = best;
            return best is not null && best >= InstallerConstants.DotNetRuntimeMinimum;
        }
        catch
        {
            return false;
        }
    }

    public static async Task InstallDesktopRuntimeAsync(IProgress<EngineProgress> progress, CancellationToken ct)
    {
        var installerPath = Path.Combine(Path.GetTempPath(), "windowsdesktop-runtime-win-x64.exe");
        await Downloader.DownloadFileAsync(InstallerConstants.DotNetRuntimeInstallerUrl, installerPath, ct)
            .ConfigureAwait(false);

        var exitCode = await RunProcessAsync(installerPath, InstallerConstants.DotNetRuntimeInstallerArgs, hidden: false, ct)
            .ConfigureAwait(false);
        if (exitCode is not 0 and not 3010) // 3010 = success, reboot required
            throw new InvalidOperationException($"Runtime installer exited with code {exitCode}.");
    }

    private static string? FindDotNetHost()
    {
        var pathDirs = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var dir in pathDirs)
        {
            var candidate = Path.Combine(dir, "dotnet.exe");
            if (File.Exists(candidate))
                return candidate;
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var fallback = Path.Combine(programFiles, "dotnet", "dotnet.exe");
        return File.Exists(fallback) ? fallback : null;
    }

    // ---------- Legacy Inno detection / removal ----------

    public static string? FindLegacyInnoUninstallString()
    {
        foreach (var (hive, view) in new[] { (RegistryHive.CurrentUser, RegistryView.Registry64),
                                            (RegistryHive.LocalMachine, RegistryView.Registry64) })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var key = baseKey.OpenSubKey(
                    $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{InstallerConstants.LegacyInnoUninstallKeyName}");
                var uninstall = key?.GetValue("UninstallString") as string;
                if (!string.IsNullOrWhiteSpace(uninstall))
                    return uninstall;
            }
            catch
            {
                // Access denied on HKLM is fine — we simply cannot clean that entry.
            }
        }

        return null;
    }

    private static void RemoveLegacyInnoRegistryKeys()
    {
        foreach (var (hive, view) in new[] { (RegistryHive.CurrentUser, RegistryView.Registry64),
                                            (RegistryHive.LocalMachine, RegistryView.Registry64) })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                baseKey.DeleteSubKeyTree(
                    $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{InstallerConstants.LegacyInnoUninstallKeyName}",
                    throwOnMissingSubKey: false);
            }
            catch
            {
                // Best effort — HKLM requires elevation.
            }
        }
    }

    // ---------- Install ----------

    public static async Task InstallAsync(
        InstallOptions options,
        IProgress<EngineProgress> progress,
        CancellationToken ct)
    {
        var installDir = Path.GetFullPath(options.InstallDir);
        InstallerLog.Info($"Install started -> '{installDir}' (embedded payload: {HasEmbeddedPayload}).");

        progress.Report(new EngineProgress { Status = Strings.Get("StatusCheckingRuntime") });
        if (!TryGetDesktopRuntime(out var runtimeVersion))
        {
            InstallerLog.Info("Desktop runtime missing; installing…");
            await InstallDesktopRuntimeAsync(progress, ct).ConfigureAwait(false);
            if (!TryGetDesktopRuntime(out _))
                throw new InvalidOperationException("Microsoft .NET Desktop Runtime 10 is still missing after installation.");
        }
        else
        {
            InstallerLog.Info($"Desktop runtime OK ({runtimeVersion}).");
        }

        KillRunningAppProcesses();

        var legacyUninstall = FindLegacyInnoUninstallString();
        if (legacyUninstall is not null)
        {
            progress.Report(new EngineProgress { Status = Strings.Get("StatusRemovingOld") });
            await RunLegacyUninstallerAsync(legacyUninstall, ct).ConfigureAwait(false);
            RemoveLegacyInnoRegistryKeys();
        }

        progress.Report(new EngineProgress { Percent = 0, Status = Strings.Get("StatusExtracting") });
        Directory.CreateDirectory(installDir);
        string? downloadedPayload = null;
        try
        {
            if (HasEmbeddedPayload)
            {
                using var stream = typeof(InstallerEngine).Assembly
                    .GetManifestResourceStream(EmbeddedPayloadResourceName)!;
                await ExtractPayloadAsync(stream, installDir, progress, ct).ConfigureAwait(false);
            }
            else
            {
                downloadedPayload = await DownloadPayloadAsync(progress, ct).ConfigureAwait(false);
                using var stream = new FileStream(downloadedPayload, FileMode.Open, FileAccess.Read, FileShare.Read);
                await ExtractPayloadAsync(stream, installDir, progress, ct).ConfigureAwait(false);
            }
        }
        finally
        {
            if (downloadedPayload is not null)
            {
                try { File.Delete(downloadedPayload); } catch { /* best effort */ }
            }
        }

        ExtractBundledTextResource("LICENSE", installDir);
        ExtractBundledTextResource("NOTICE", installDir);

        progress.Report(new EngineProgress { Percent = 90, Status = Strings.Get("StatusShortcuts") });
        CopySelfAsUninstaller(installDir);
        CreateShortcut(InstallerConstants.StartMenuShortcutPath, Path.Combine(installDir, InstallerConstants.MainExeName));
        if (options.CreateDesktopShortcut)
            CreateShortcut(InstallerConstants.DesktopShortcutPath, Path.Combine(installDir, InstallerConstants.MainExeName));

        progress.Report(new EngineProgress { Percent = 95, Status = Strings.Get("StatusFinishing") });
        WriteUninstallRegistryEntry(installDir);

        progress.Report(new EngineProgress { Percent = 100, Status = Strings.Get("DoneTitle") });

        if (options.LaunchAfterInstall)
        {
            try
            {
                Process.Start(new ProcessStartInfo(Path.Combine(installDir, InstallerConstants.MainExeName))
                {
                    UseShellExecute = true,
                });
            }
            catch
            {
                // Launch failure must not fail the install.
            }
        }
    }

    private static async Task<string> DownloadPayloadAsync(IProgress<EngineProgress> progress, CancellationToken ct)
    {
        var downloadProgress = new Progress<DownloadProgress>(p => progress.Report(new EngineProgress
        {
            Percent = p.Percent * 0.8, // download covers 0..80 % of the overall bar
            Status = p.Status,
        }));
        return await Downloader.DownloadToTempFileAsync(
                PayloadManifest.Urls, PayloadManifest.Sha256, downloadProgress, ct)
            .ConfigureAwait(false);
    }

    private static async Task ExtractPayloadAsync(
        Stream zipStream,
        string installDir,
        IProgress<EngineProgress> progress,
        CancellationToken ct)
    {
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        var entries = archive.Entries;

        for (var i = 0; i < entries.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var entry = entries[i];
            var destinationPath = Path.GetFullPath(Path.Combine(installDir, entry.FullName));
            if (!destinationPath.StartsWith(installDir, StringComparison.OrdinalIgnoreCase))
                continue; // zip-slip guard

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            using var entryStream = entry.Open();
            using var target = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await entryStream.CopyToAsync(target, ct).ConfigureAwait(false);

            var percent = HasEmbeddedPayload
                ? (i + 1) * 90.0 / entries.Count      // offline: extraction owns 0..90 %
                : 80d + (i + 1) * 10.0 / entries.Count; // online: download owned 0..80 %
            progress.Report(new EngineProgress { Percent = percent, Status = Strings.Get("StatusExtracting") });
        }
    }

    private static void ExtractBundledTextResource(string name, string installDir)
    {
        try
        {
            var assembly = typeof(InstallerEngine).Assembly;
            var resourceName = $"UniversalDeviceToolkit.Installer.{name}";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
                return;

            using var reader = new StreamReader(stream);
            File.WriteAllText(Path.Combine(installDir, name), reader.ReadToEnd());
        }
        catch
        {
            // Legal files are nice-to-have; never fail the install over them.
        }
    }

    private static void CopySelfAsUninstaller(string installDir)
    {
        var self = Environment.ProcessPath;
        if (string.IsNullOrEmpty(self) || !File.Exists(self))
            throw new InvalidOperationException("Cannot locate the running installer executable.");

        var destination = Path.Combine(installDir, InstallerConstants.UninstallerExeName);
        if (string.Equals(Path.GetFullPath(self), destination, StringComparison.OrdinalIgnoreCase))
            return; // already running as the installed uninstaller

        File.Copy(self, destination, overwrite: true);
    }

    private static void CreateShortcut(string shortcutPath, string targetPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell is unavailable.");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
        shortcut.IconLocation = targetPath;
        shortcut.Save();
    }

    private static void WriteUninstallRegistryEntry(string installDir)
    {
        using var key = Registry.CurrentUser.CreateSubKey(
            $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{InstallerConstants.UninstallKeyName}",
            writable: true) ?? throw new InvalidOperationException("Cannot create the uninstall registry key.");

        var uninstallerPath = Path.Combine(installDir, InstallerConstants.UninstallerExeName);
        key.SetValue("DisplayName", InstallerConstants.AppName);
        key.SetValue("DisplayVersion", PayloadManifest.Version);
        key.SetValue("Publisher", InstallerConstants.Publisher);
        key.SetValue("URLInfoAbout", InstallerConstants.AppUrl);
        key.SetValue("DisplayIcon", Path.Combine(installDir, InstallerConstants.MainExeName));
        key.SetValue("InstallLocation", installDir);
        key.SetValue("UninstallString", $"\"{uninstallerPath}\" --uninstall");
        key.SetValue("QuietUninstallString", $"\"{uninstallerPath}\" --uninstall /silent");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    // ---------- Uninstall ----------

    public static async Task UninstallAsync(
        UninstallOptions options,
        IProgress<EngineProgress> progress,
        CancellationToken ct)
    {
        var installDir = Path.GetFullPath(options.InstallDir);
        var runningInsideInstallDir = IsRunningFrom(installDir);

        progress.Report(new EngineProgress { Percent = 5, Status = Strings.Get("StatusUnregisterShell") });
        KillRunningAppProcesses();
        await UnregisterShellAsync(installDir, ct).ConfigureAwait(false);

        progress.Report(new EngineProgress { Percent = 25, Status = Strings.Get("StatusRemovingTasks") });
        DeleteScheduledTask(InstallerConstants.AutorunTaskNameNew);
        DeleteScheduledTask(InstallerConstants.AutorunTaskNameLegacy);

        progress.Report(new EngineProgress { Percent = 40, Status = Strings.Get("StatusRemovingFiles") });
        TryDeleteFile(InstallerConstants.StartMenuShortcutPath);
        TryDeleteFile(InstallerConstants.DesktopShortcutPath);

        if (options.DeleteAppData)
        {
            progress.Report(new EngineProgress { Percent = 55, Status = Strings.Get("StatusRemovingData") });
            TryDeleteDirectory(InstallerConstants.AppDataDir);
        }

        RemoveUninstallRegistryEntry();
        RemoveLegacyInnoRegistryKeys();

        progress.Report(new EngineProgress { Percent = 70, Status = Strings.Get("StatusRemovingFiles") });
        if (runningInsideInstallDir)
        {
            // We cannot delete our own exe; delete everything else and schedule
            // the directory removal right after this process exits.
            DeleteDirectoryContentsExcept(installDir, Environment.ProcessPath);
            ScheduleDirectoryDeletion(installDir);
        }
        else
        {
            TryDeleteDirectory(installDir);
        }

        progress.Report(new EngineProgress { Percent = 100, Status = Strings.Get("UninstallDoneTitle") });
    }

    private static bool IsRunningFrom(string installDir)
    {
        var self = Environment.ProcessPath;
        return !string.IsNullOrEmpty(self) &&
               Path.GetFullPath(self).StartsWith(
                   installDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static async Task UnregisterShellAsync(string installDir, CancellationToken ct)
    {
        var shellExe = Path.Combine(installDir, "Shell.exe");
        if (!File.Exists(shellExe))
            return;

        try
        {
            // Mirrors the Inno uninstall: unregister Nilesoft Shell (restarts Explorer),
            // then wait ~7 s for file locks to be released.
            await RunProcessAsync(shellExe, "-unregister -treat -restart -silent", hidden: true, ct)
                .ConfigureAwait(false);
            await Task.Delay(7000, ct).ConfigureAwait(false);
        }
        catch
        {
            await Task.Delay(3000, ct).ConfigureAwait(false);
        }
    }

    private static void DeleteScheduledTask(string taskName)
    {
        try
        {
            var psi = new ProcessStartInfo("schtasks", $"/Delete /TN \"{taskName}\" /F")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            using var process = Process.Start(psi);
            process?.WaitForExit(15000);
        }
        catch
        {
            // Task may not exist — fine.
        }
    }

    private static void RemoveUninstallRegistryEntry()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{InstallerConstants.UninstallKeyName}",
                throwOnMissingSubKey: false);
        }
        catch
        {
            // Best effort.
        }
    }

    private static void DeleteDirectoryContentsExcept(string dir, string keepFile)
    {
        var keep = Path.GetFullPath(keepFile);
        foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            if (string.Equals(Path.GetFullPath(file), keep, StringComparison.OrdinalIgnoreCase))
                continue;
            TryDeleteFile(file);
        }

        foreach (var subDir in Directory.EnumerateDirectories(dir, "*", SearchOption.AllDirectories)
                     .OrderByDescending(d => d.Length))
        {
            TryDeleteDirectory(subDir);
        }
    }

    private static void ScheduleDirectoryDeletion(string dir)
    {
        try
        {
            // Retry for ~10 s: our own exe may still be releasing the directory handle.
            var psi = new ProcessStartInfo("cmd.exe",
                $"/c for /l %i in (1,1,10) do (rd /s /q \"{dir}\" 2>nul & if not exist \"{dir}\" exit /b 0 & ping 127.0.0.1 -n 2 >nul)")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                // The helper must not sit inside the directory it deletes —
                // an inherited CWD would keep the root folder alive forever.
                WorkingDirectory = Path.GetTempPath(),
            };
            Process.Start(psi);
        }
        catch
        {
            // If scheduling fails the user can delete the leftover folder manually.
        }
    }

    // ---------- Shared helpers ----------

    private static void KillRunningAppProcesses()
    {
        foreach (var name in new[] { "Universal Device Toolkit", "udt-cli" })
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                try
                {
                    process.Kill(entireProcessTree: false);
                    process.WaitForExit(10000);
                }
                catch
                {
                    // Already gone — fine.
                }
            }
        }
    }

    private static async Task RunLegacyUninstallerAsync(string uninstallString, CancellationToken ct)
    {
        try
        {
            var (file, args) = SplitCommandLine(uninstallString);
            if (file is null || !File.Exists(file))
                return;

            await RunProcessAsync(file, $"{args} /VERYSILENT /SUPPRESSMSGBOXES /NORESTART".Trim(), hidden: true, ct)
                .ConfigureAwait(false);
        }
        catch
        {
            // Continue with an overwrite install even if the legacy uninstaller fails.
        }
    }

    private static (string? File, string Args) SplitCommandLine(string commandLine)
    {
        var trimmed = commandLine.Trim();
        if (trimmed.StartsWith('"'))
        {
            var end = trimmed.IndexOf('"', 1);
            if (end > 1)
                return (trimmed[1..end], trimmed[(end + 1)..].Trim());
        }

        var space = trimmed.IndexOf(' ');
        return space > 0 ? (trimmed[..space], trimmed[(space + 1)..].Trim()) : (trimmed, "");
    }

    private static async Task<int> RunProcessAsync(string fileName, string arguments, bool hidden, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = !hidden,
            CreateNoWindow = hidden,
            WindowStyle = hidden ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal,
        };
        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start '{fileName}'.");
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        return process.ExitCode;
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best effort */ }
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch { /* best effort */ }
    }
}

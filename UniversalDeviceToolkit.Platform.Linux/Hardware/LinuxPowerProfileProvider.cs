using System.Diagnostics;
using UniversalDeviceToolkit.Abstractions.Hardware;

namespace UniversalDeviceToolkit.Platform.Linux.Hardware;

/// <summary>
/// Linux implementation of <see cref="IPowerProfileProvider"/>.
/// Supports powerprofilesctl (power-profiles-daemon) and tuned-adm.
/// Falls back to a stub when neither is available.
/// </summary>
public sealed class LinuxPowerProfileProvider : IPowerProfileProvider
{
    private enum Backend { None, PowerProfilesCtl, TunedAdm }

    private static Backend DetectBackend()
    {
        if (File.Exists("/usr/bin/powerprofilesctl")) return Backend.PowerProfilesCtl;
        if (File.Exists("/usr/sbin/tuned-adm")) return Backend.TunedAdm;
        return Backend.None;
    }

    /// <inheritdoc />
    public bool IsAvailable => OperatingSystem.IsLinux() && DetectBackend() != Backend.None;

    /// <inheritdoc />
    public IReadOnlyList<string> GetAvailableProfiles()
    {
        return DetectBackend() switch
        {
            Backend.PowerProfilesCtl => ParsePowerProfilesList(),
            Backend.TunedAdm => ParseTunedAdmList(),
            _ => Array.Empty<string>()
        };
    }

    /// <inheritdoc />
    public string? GetActiveProfile()
    {
        return DetectBackend() switch
        {
            Backend.PowerProfilesCtl =>
                RunCli("/usr/bin/powerprofilesctl", "get")?.Trim(),
            Backend.TunedAdm =>
                ParseTunedAdmActive(RunCli("/usr/sbin/tuned-adm", "active")),
            _ => null
        };
    }

    /// <inheritdoc />
    public async Task SetActiveProfileAsync(string profileName)
    {
        switch (DetectBackend())
        {
            case Backend.PowerProfilesCtl:
                await RunCliAsync("/usr/bin/powerprofilesctl", "set", profileName).ConfigureAwait(false);
                break;
            case Backend.TunedAdm:
                await RunCliAsync("/usr/sbin/tuned-adm", "profile", profileName).ConfigureAwait(false);
                break;
            default:
                throw new NotSupportedException("No supported power profile backend is available.");
        }
    }

    private static IReadOnlyList<string> ParsePowerProfilesList()
    {
        var output = RunCli("/usr/bin/powerprofilesctl", "list");
        if (output is null) return Array.Empty<string>();

        var profiles = new List<string>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // Lines like "  performance:" or "* balanced:"
            var cleaned = line.TrimStart(' ', '*', '\t').TrimEnd(':').Trim();
            if (!string.IsNullOrWhiteSpace(cleaned))
                profiles.Add(cleaned);
        }

        return profiles.Distinct().ToArray();
    }

    private static IReadOnlyList<string> ParseTunedAdmList()
    {
        var output = RunCli("/usr/sbin/tuned-adm", "list");
        if (output is null) return Array.Empty<string>();

        var profiles = new List<string>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("Available profiles:", StringComparison.OrdinalIgnoreCase))
                continue;
            if (line.StartsWith("- ", StringComparison.Ordinal))
                profiles.Add(line[2..].Trim());
        }

        return profiles;
    }

    private static string? ParseTunedAdmActive(string? output)
    {
        if (output is null) return null;
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("Current active profile:", StringComparison.OrdinalIgnoreCase))
            {
                var colon = line.IndexOf(':');
                return colon >= 0 ? line[(colon + 1)..].Trim() : null;
            }
        }
        return null;
    }

    private static string? RunCli(string executable, params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo(executable, string.Join(' ', args))
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process is null) return null;
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);
            return string.IsNullOrWhiteSpace(output) ? null : output;
        }
        catch
        {
            return null;
        }
    }

    private static async Task RunCliAsync(string executable, params string[] args)
    {
        var psi = new ProcessStartInfo(executable, string.Join(' ', args))
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi);
        if (process is not null)
            await process.WaitForExitAsync().ConfigureAwait(false);
    }
}

using System.Diagnostics;
using UniversalDeviceToolkit.Abstractions.Lifecycle;

namespace UniversalDeviceToolkit.Platform.Linux.Lifecycle;

/// <summary>
/// Linux implementation of <see cref="IAutorunManager"/>.
/// Manages autostart via a systemd user service unit.
/// </summary>
public sealed class LinuxAutorunManager : IAutorunManager
{
    private static readonly string ServiceDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "systemd", "user");

    private static readonly string ServicePath = Path.Combine(ServiceDir, "udt.service");

    private const string ServiceUnit = """
        [Unit]
        Description=Universal Device Toolkit

        [Service]
        Type=simple
        ExecStart=/usr/local/bin/udt
        Restart=on-failure
        RestartSec=5

        [Install]
        WantedBy=default.target
        """;

    /// <inheritdoc />
    public Task<bool> IsEnabledAsync()
    {
        return Task.FromResult(File.Exists(ServicePath));
    }

    /// <inheritdoc />
    public async Task EnableAsync()
    {
        Directory.CreateDirectory(ServiceDir);
        await File.WriteAllTextAsync(ServicePath, ServiceUnit).ConfigureAwait(false);
        await RunSystemdAsync("daemon-reload").ConfigureAwait(false);
        await RunSystemdAsync("enable", "udt.service").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DisableAsync()
    {
        await RunSystemdAsync("disable", "udt.service").ConfigureAwait(false);
        if (File.Exists(ServicePath))
            File.Delete(ServicePath);
        await RunSystemdAsync("daemon-reload").ConfigureAwait(false);
    }

    private static async Task RunSystemdAsync(string command, string? unit = null)
    {
        try
        {
            var args = $"--user {command}";
            if (unit is not null)
                args += $" {unit}";

            var psi = new ProcessStartInfo("systemctl", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            if (process is not null)
                await process.WaitForExitAsync().ConfigureAwait(false);
        }
        catch
        {
            // systemctl may not be available in all environments
        }
    }
}

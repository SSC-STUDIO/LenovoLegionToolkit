using System.Diagnostics;
using UniversalDeviceToolkit.Abstractions.Hardware;

namespace UniversalDeviceToolkit.Platform.Linux.Hardware;

/// <summary>
/// Linux implementation of <see cref="IGpuBackend"/>.
/// Uses nvidia-smi (NVIDIA) or rocm-smi (AMD) CLI tools to query GPU information.
/// </summary>
public sealed class LinuxGpuBackend : IGpuBackend
{
    private static readonly string NvidiaSmi = "/usr/bin/nvidia-smi";
    private static readonly string RocmSmi = "/usr/bin/rocm-smi";

    private enum GpuVendor { Unknown, Nvidia, Amd }

    private GpuVendor DetectVendor()
    {
        if (File.Exists(NvidiaSmi)) return GpuVendor.Nvidia;
        if (File.Exists(RocmSmi)) return GpuVendor.Amd;
        return GpuVendor.Unknown;
    }

    /// <inheritdoc />
    public bool IsAvailable => OperatingSystem.IsLinux() && DetectVendor() != GpuVendor.Unknown;

    /// <inheritdoc />
    public string? GetGpuName() => DetectVendor() switch
    {
        GpuVendor.Nvidia => RunCli(NvidiaSmi, "--query-gpu=name", "--format=csv,noheader"),
        GpuVendor.Amd => RunCli(RocmSmi, "--showproductname", "--csv")
            ?.Split('\n').Skip(1).FirstOrDefault()?.Split(',').Skip(1).FirstOrDefault()?.Trim(),
        _ => null
    };

    /// <inheritdoc />
    public int? GetUsagePercent() => DetectVendor() switch
    {
        GpuVendor.Nvidia => ParseInt(RunCli(NvidiaSmi, "--query-gpu=utilization.gpu", "--format=csv,noheader,nounits")),
        _ => null
    };

    /// <inheritdoc />
    public int? GetTemperatureCelsius() => DetectVendor() switch
    {
        GpuVendor.Nvidia => ParseInt(RunCli(NvidiaSmi, "--query-gpu=temperature.gpu", "--format=csv,noheader")),
        _ => null
    };

    /// <inheritdoc />
    public int? GetCurrentClockMhz() => DetectVendor() switch
    {
        GpuVendor.Nvidia => ParseInt(RunCli(NvidiaSmi, "--query-gpu=clocks.sm", "--format=csv,noheader,nounits")),
        _ => null
    };

    /// <inheritdoc />
    public int? GetBoostClockMhz() => DetectVendor() switch
    {
        GpuVendor.Nvidia => ParseInt(RunCli(NvidiaSmi, "--query-gpu=clocks.max.sm", "--format=csv,noheader,nounits")),
        _ => null
    };

    /// <inheritdoc />
    public int? GetMemoryUsedMb() => DetectVendor() switch
    {
        GpuVendor.Nvidia => ParseInt(RunCli(NvidiaSmi, "--query-gpu=memory.used", "--format=csv,noheader,nounits")),
        _ => null
    };

    /// <inheritdoc />
    public int? GetMemoryTotalMb() => DetectVendor() switch
    {
        GpuVendor.Nvidia => ParseInt(RunCli(NvidiaSmi, "--query-gpu=memory.total", "--format=csv,noheader,nounits")),
        _ => null
    };

    private static int? ParseInt(string? value) =>
        int.TryParse(value?.Trim(), out var result) ? result : null;

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
            process.WaitForExit(3000);
            return string.IsNullOrWhiteSpace(output) ? null : output;
        }
        catch
        {
            return null;
        }
    }
}

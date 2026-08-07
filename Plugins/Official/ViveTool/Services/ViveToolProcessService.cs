using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Plugins.Core;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Services;

/// <summary>
/// Handles process execution for ViVeTool commands.
/// </summary>
public class ViveToolProcessService
{
    private readonly ProcessRunner _processRunner;

    /// <summary>
    /// Initializes a new instance of the ViveToolProcessService class.
    /// </summary>
    public ViveToolProcessService()
    {
        _processRunner = new ProcessRunner();
    }

    /// <summary>
    /// Executes a ViVeTool command and returns the result.
    /// </summary>
    public async Task<(bool Success, string? Output, string? Error)> ExecuteCommandAsync(
        string viveToolPath,
        string arguments)
    {
        try
        {
            var result = await _processRunner.RunProcessAsync(
                viveToolPath,
                arguments,
                timeoutSeconds: Constants.DefaultTimeoutSeconds).ConfigureAwait(false);

            return (result.Success, result.Output, result.Error);
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ViveTool: Error executing command: {ex.Message}", ex);
            return (false, null, ex.Message);
        }
    }
}

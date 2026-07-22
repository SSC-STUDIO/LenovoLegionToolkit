using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Utils;

// ReSharper disable StringLiteralTypo

namespace UniversalDeviceToolkit.Lib.System.Management;

public static partial class WMI
{
    /// <summary>
    /// Invokes a Lenovo GameZone WMI method through a short-lived powershell.exe running
    /// CIM cmdlets (Microsoft.Management.Infrastructure channel). Some Legion WMI providers
    /// return empty out-parameters to classic System.Management clients (probed on
    /// Y9000P IRX9: ReturnValue missing, Data null); the MMI channel used by the CIM
    /// cmdlets marshals them correctly. Returns the method's integer "Data" out-parameter,
    /// or 0 when unavailable.
    /// </summary>
    internal static async Task<int> InvokeGameZoneMethodViaCimProcessAsync(string methodName, int? dataParam = null, CancellationToken cancellationToken = default)
    {
        var arguments = dataParam.HasValue ? $" -Arguments @{{Data={dataParam.Value}}}" : string.Empty;
        var script = string.Concat(
            "$i=@(Get-CimInstance -Namespace root\\WMI -ClassName LENOVO_GAMEZONE_DATA -ErrorAction Stop);",
            "if($i.Count -eq 0){exit 2};",
            $"$r=Invoke-CimMethod -InputObject $i[0] -MethodName {methodName}{arguments} -ErrorAction Stop;",
            "if($null -ne $r.Data){[int]$r.Data}");

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var output = (await outputTask.ConfigureAwait(false)).Trim();

            if (process.ExitCode != 0)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"CIM process call failed (exit {process.ExitCode}): {(await errorTask.ConfigureAwait(false)).Trim()} [method={methodName}]");
                return 0;
            }

            return int.TryParse(output, out var value) ? value : 0;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"CIM process call failed [method={methodName}]", ex);
            return 0;
        }
    }
}

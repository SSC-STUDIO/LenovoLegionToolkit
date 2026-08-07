using UniversalDeviceToolkit.Shared.Utils;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Command-line startup switches for the Avalonia host, mirroring the WPF
/// <c>UniversalDeviceToolkit.WPF.Flags</c> contract so both shells honor the
/// same automation / recovery / proxy switches.
/// </summary>
public sealed class AvaloniaStartupFlags
{
    public const string DisableUpdateCheckerSwitch = "--disable-update-checker";
    public const string SafeStartSwitch = "--safe-start";
    public const string ResetHardwareStateSwitch = "--reset-hardware-state";
    public const string ResetNetworkStateSwitch = "--reset-network-state";
    public const string RestoreProcessorMinStateSwitch = "--restore-processor-min-state";

    /// <summary>
    /// Flags parsed from the current process command line. Program.cs populates
    /// this before the Avalonia app starts; the App shell reads it for startup
    /// behavior. Tests may replace it with a fixed instance.
    /// </summary>
    public static AvaloniaStartupFlags Current { get; set; } = new([]);

    public bool IsTraceEnabled { get; }
    public bool Minimized { get; }
    public bool DisableTrayTooltip { get; }
    public bool AllowAllPowerModesOnBattery { get; }
    public bool ForceDisableRgbKeyboardSupport { get; }
    public bool ForceDisableSpectrumKeyboardSupport { get; }
    public bool ForceDisableLenovoLighting { get; }
    public bool ExperimentalGPUWorkingMode { get; }
    public Uri? ProxyUrl { get; }
    public string? ProxyUsername { get; }
    public string? ProxyPassword { get; }
    public bool ProxyAllowAllCerts { get; }
    public bool DisableUpdateChecker { get; }
    public bool SafeStart { get; }
    public bool ResetHardwareState { get; }
    public bool ResetNetworkState { get; }
    public bool RestoreProcessorMinState { get; }

    public AvaloniaStartupFlags(IEnumerable<string> startupArgs)
    {
        var args = startupArgs.Concat(LoadExternalArgs()).ToArray();

        IsTraceEnabled = Has(args, "--trace");
        Minimized = Has(args, "--minimized");
        DisableTrayTooltip = Has(args, "--disable-tray-tooltip");
        AllowAllPowerModesOnBattery = Has(args, "--allow-all-power-modes-on-battery");
        ForceDisableRgbKeyboardSupport = Has(args, "--force-disable-rgbkb");
        ForceDisableSpectrumKeyboardSupport = Has(args, "--force-disable-spectrumkb");
        ForceDisableLenovoLighting = Has(args, "--force-disable-lenovolighting");
        ExperimentalGPUWorkingMode = Has(args, "--experimental-gpu-working-mode");
        ProxyUrl = Uri.TryCreate(StringValue(args, "--proxy-url"), UriKind.Absolute, out var uri) ? uri : null;
        ProxyUsername = StringValue(args, "--proxy-username");
        ProxyPassword = StringValue(args, "--proxy-password");
        ProxyAllowAllCerts = Has(args, "--proxy-allow-all-certs");
        DisableUpdateChecker = Has(args, DisableUpdateCheckerSwitch);
        SafeStart = Has(args, SafeStartSwitch);
        ResetHardwareState = Has(args, ResetHardwareStateSwitch);
        ResetNetworkState = Has(args, ResetNetworkStateSwitch);
        RestoreProcessorMinState = Has(args, RestoreProcessorMinStateSwitch);
    }

    public static AvaloniaStartupFlags Parse(IEnumerable<string> args) => new(args);

    private static string[] LoadExternalArgs()
    {
        try
        {
            var argsFile = Path.Combine(Folders.AppData, "args.txt");
            return !File.Exists(argsFile) ? [] : File.ReadAllLines(argsFile);
        }
        catch
        {
            return [];
        }
    }

    private static bool Has(IEnumerable<string> values, string key) =>
        values.Contains(key, StringComparer.OrdinalIgnoreCase);

    private static string? StringValue(IEnumerable<string> values, string key)
    {
        var args = values.ToArray();

        for (var i = 0; i < args.Length; i++)
        {
            var value = args[i];
            if (value.Equals(key, StringComparison.OrdinalIgnoreCase))
                return i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                    ? args[i + 1]
                    : null;

            if (value.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                return value[(key.Length + 1)..];
        }

        return null;
    }
}

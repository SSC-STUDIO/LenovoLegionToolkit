using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalDeviceToolkit.Lib.Utils;

// ReSharper disable StringLiteralTypo

namespace UniversalDeviceToolkit.Avalonia;

public class Flags
{
    public const string DisableUpdateCheckerSwitch = "--disable-update-checker";
    public const string SafeStartSwitch = "--safe-start";
    public const string ResetHardwareStateSwitch = "--reset-hardware-state";
    public const string ResetNetworkStateSwitch = "--reset-network-state";
    /// <summary>
    /// Optional companion to <see cref="ResetHardwareStateSwitch"/>. When set,
    /// recovery also writes Processor power management "Minimum processor state"
    /// on the <em>currently active</em> Windows power plan (AC+DC). This does
    /// modify the user's active plan; leave unset to avoid plan edits.
    /// </summary>
    public const string RestoreProcessorMinStateSwitch = "--restore-processor-min-state";

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
    /// <summary>
    /// When true with <see cref="ResetHardwareState"/>, also restore processor
    /// min state on the active Windows power plan (see switch docs).
    /// </summary>
    public bool RestoreProcessorMinState { get; }

    public Flags(IEnumerable<string> startupArgs)
    {
        var args = startupArgs.Concat(LoadExternalArgs()).ToArray();

        IsTraceEnabled = BoolValue(args, "--trace");
        Minimized = BoolValue(args, "--minimized");
        DisableTrayTooltip = BoolValue(args, "--disable-tray-tooltip");
        AllowAllPowerModesOnBattery = BoolValue(args, "--allow-all-power-modes-on-battery");
        ForceDisableRgbKeyboardSupport = BoolValue(args, "--force-disable-rgbkb");
        ForceDisableSpectrumKeyboardSupport = BoolValue(args, "--force-disable-spectrumkb");
        ForceDisableLenovoLighting = BoolValue(args, "--force-disable-lenovolighting");
        ExperimentalGPUWorkingMode = BoolValue(args, "--experimental-gpu-working-mode");
        ProxyUrl = Uri.TryCreate(StringValue(args, "--proxy-url"), UriKind.Absolute, out var uri) ? uri : null;
        ProxyUsername = StringValue(args, "--proxy-username");
        ProxyPassword = StringValue(args, "--proxy-password");
        ProxyAllowAllCerts = BoolValue(args, "--proxy-allow-all-certs");
        DisableUpdateChecker = BoolValue(args, DisableUpdateCheckerSwitch);
        SafeStart = BoolValue(args, SafeStartSwitch);
        ResetHardwareState = BoolValue(args, ResetHardwareStateSwitch);
        ResetNetworkState = BoolValue(args, ResetNetworkStateSwitch);
        RestoreProcessorMinState = BoolValue(args, RestoreProcessorMinStateSwitch);
    }

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

    private static bool BoolValue(IEnumerable<string> values, string key) => values.Contains(key);

    internal static string? StringValue(IEnumerable<string> values, string key)
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

    public override string ToString() =>
        $"{nameof(IsTraceEnabled)}: {IsTraceEnabled}," +
        $" {nameof(Minimized)}: {Minimized}," +
        $" {nameof(DisableTrayTooltip)}: {DisableTrayTooltip}," +
        $" {nameof(AllowAllPowerModesOnBattery)}: {AllowAllPowerModesOnBattery}," +
        $" {nameof(ForceDisableRgbKeyboardSupport)}: {ForceDisableRgbKeyboardSupport}," +
        $" {nameof(ForceDisableSpectrumKeyboardSupport)}: {ForceDisableSpectrumKeyboardSupport}," +
        $" {nameof(ForceDisableLenovoLighting)}: {ForceDisableLenovoLighting}," +
        $" {nameof(ExperimentalGPUWorkingMode)}: {ExperimentalGPUWorkingMode}," +
        $" {nameof(ProxyUrl)}: {ProxyUrl}," +
        $" {nameof(ProxyUsername)}: {ProxyUsername}," +
        $" {nameof(ProxyPassword)}: [REDACTED]," +
        $" {nameof(ProxyAllowAllCerts)}: {ProxyAllowAllCerts}," +
        $" {nameof(DisableUpdateChecker)}: {DisableUpdateChecker}," +
        $" {nameof(SafeStart)}: {SafeStart}," +
        $" {nameof(ResetHardwareState)}: {ResetHardwareState}," +
        $" {nameof(ResetNetworkState)}: {ResetNetworkState}," +
        $" {nameof(RestoreProcessorMinState)}: {RestoreProcessorMinState}";
}

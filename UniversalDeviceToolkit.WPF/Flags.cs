using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LenovoLegionToolkit.Lib.Utils;

// ReSharper disable StringLiteralTypo

namespace UniversalDeviceToolkit.WPF;

public class Flags
{
    public const string DisableUpdateCheckerSwitch = "--disable-update-checker";
    public const string SingleInstanceKeySwitch = "--single-instance-key";
    public const string IpcPipeNameSwitch = "--ipc-pipe-name";

    public bool IsTraceEnabled { get; }
    public bool Minimized { get; }
    public bool SkipCompatibilityCheck { get; }
    public bool DisableTrayTooltip { get; }
    public bool AllowAllPowerModesOnBattery { get; }
    public bool ForceDisableRgbKeyboardSupport { get; }
    public bool ForceDisableSpectrumKeyboardSupport { get; }
    public bool ForceDisableLenovoLighting { get; }
    public bool ExperimentalGPUWorkingMode { get; }
    public bool EnableHybridModeAutomation { get; }
    public Uri? ProxyUrl { get; }
    public string? ProxyUsername { get; }
    public string? ProxyPassword { get; }
    public bool ProxyAllowAllCerts { get; }
    public bool DisableUpdateChecker { get; }
    public bool DisableConflictingSoftwareWarning { get; }
    public string? SingleInstanceKey { get; }
    public string? IpcPipeName { get; }

    public Flags(IEnumerable<string> startupArgs)
    {
        var args = startupArgs.Concat(LoadExternalArgs()).ToArray();

        IsTraceEnabled = BoolValue(args, "--trace");
        Minimized = BoolValue(args, "--minimized");
        SkipCompatibilityCheck = BoolValue(args, "--skip-compat-check");
        DisableTrayTooltip = BoolValue(args, "--disable-tray-tooltip");
        AllowAllPowerModesOnBattery = BoolValue(args, "--allow-all-power-modes-on-battery");
        ForceDisableRgbKeyboardSupport = BoolValue(args, "--force-disable-rgbkb");
        ForceDisableSpectrumKeyboardSupport = BoolValue(args, "--force-disable-spectrumkb");
        ForceDisableLenovoLighting = BoolValue(args, "--force-disable-lenovolighting");
        ExperimentalGPUWorkingMode = BoolValue(args, "--experimental-gpu-working-mode");
        EnableHybridModeAutomation = BoolValue(args, "--enable-hybrid-mode-automation");
        ProxyUrl = Uri.TryCreate(StringValue(args, "--proxy-url"), UriKind.Absolute, out var uri) ? uri : null;
        ProxyUsername = StringValue(args, "--proxy-username");
        ProxyPassword = StringValue(args, "--proxy-password");
        ProxyAllowAllCerts = BoolValue(args, "--proxy-allow-all-certs");
        DisableUpdateChecker = BoolValue(args, DisableUpdateCheckerSwitch);
        DisableConflictingSoftwareWarning = BoolValue(args, "--disable-conflicting-software-warning");
        SingleInstanceKey = StringValue(args, SingleInstanceKeySwitch);
        IpcPipeName = StringValue(args, IpcPipeNameSwitch);
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
        $" {nameof(SkipCompatibilityCheck)}: {SkipCompatibilityCheck}," +
        $" {nameof(DisableTrayTooltip)}: {DisableTrayTooltip}," +
        $" {nameof(AllowAllPowerModesOnBattery)}: {AllowAllPowerModesOnBattery}," +
        $" {nameof(ForceDisableRgbKeyboardSupport)}: {ForceDisableRgbKeyboardSupport}," +
        $" {nameof(ForceDisableSpectrumKeyboardSupport)}: {ForceDisableSpectrumKeyboardSupport}," +
        $" {nameof(ForceDisableLenovoLighting)}: {ForceDisableLenovoLighting}," +
        $" {nameof(ExperimentalGPUWorkingMode)}: {ExperimentalGPUWorkingMode}," +
        $" {nameof(EnableHybridModeAutomation)}: {EnableHybridModeAutomation}," +
        $" {nameof(ProxyUrl)}: {ProxyUrl}," +
        $" {nameof(ProxyUsername)}: {ProxyUsername}," +
        $" {nameof(ProxyPassword)}: [REDACTED]," +
        $" {nameof(ProxyAllowAllCerts)}: {ProxyAllowAllCerts}," +
        $" {nameof(DisableUpdateChecker)}: {DisableUpdateChecker}, " +
        $" {nameof(DisableConflictingSoftwareWarning)}: {DisableConflictingSoftwareWarning}," +
        $" {nameof(SingleInstanceKey)}: {SingleInstanceKey}," +
        $" {nameof(IpcPipeName)}: {IpcPipeName}";
}

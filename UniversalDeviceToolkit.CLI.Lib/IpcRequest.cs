namespace UniversalDeviceToolkit.CLI.Lib;

public class IpcRequest
{
    public enum OperationType
    {
        Unknown,
        ListFeatures,
        ListFeatureValues,
        ListQuickActions,
        GetFeatureValue,
        SetFeatureValue,
        GetSpectrumProfile,
        SetSpectrumProfile,
        GetSpectrumBrightness,
        SetSpectrumBrightness,
        GetRGBPreset,
        SetRGBPreset,
        QuickAction,
        IsShellRegistered,
        IsShellInstalled,
        InstallShell,
        UninstallShell,
        GetAppStatus,
    }

    public OperationType? Operation { get; init; }

    public string? Name { get; init; }

    public string? Value { get; init; }
}

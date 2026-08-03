namespace UniversalDeviceToolkit.Installer;

/// <summary>
/// Online payload descriptor. The Release workflow regenerates this file before
/// publishing the Online flavor (version, asset name, SHA-256 and mirror URLs).
/// Defaults point at the latest published release so local builds stay testable.
/// An empty <see cref="Sha256"/> skips hash verification (development only).
/// </summary>
internal static class PayloadManifest
{
    public const string Version = "5.0.1";
    public const string Sha256 = "724A5E171240184760996EA3D2BBF7F3372B9C294B173AC82EA3C249BB18F8F1";

    public static readonly string[] Urls =
    [
        "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/v5.0.1/UniversalDeviceToolkit_v5.0.1_Online_win-x64.zip",
        "https://gh-proxy.com/https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/v5.0.1/UniversalDeviceToolkit_v5.0.1_Online_win-x64.zip",
        "https://ghfast.top/https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/v5.0.1/UniversalDeviceToolkit_v5.0.1_Online_win-x64.zip",
    ];
}

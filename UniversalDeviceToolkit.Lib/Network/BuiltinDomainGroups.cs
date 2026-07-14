using System.Collections.Generic;

namespace UniversalDeviceToolkit.Lib.Network;

/// <summary>
/// Audited built-in domain groups (Steam, GitHub, etc.).
/// Disabled by default — user must explicitly enable groups before they affect PAC/proxy.
/// No remote script download; static lists only.
/// </summary>
public static class BuiltinDomainGroups
{
    public static List<NetworkDomainGroup> CreateDefaults() =>
    [
        new NetworkDomainGroup
        {
            Id = "steam",
            DisplayName = "Steam",
            Enabled = false,
            Domains =
            [
                "steampowered.com",
                "steamcommunity.com",
                "steamstatic.com",
                "steamserver.net",
                "steamcontent.com",
                "steam-api.com",
                "steamstat.us"
            ]
        },
        new NetworkDomainGroup
        {
            Id = "github",
            DisplayName = "GitHub",
            Enabled = false,
            Domains =
            [
                "github.com",
                "githubusercontent.com",
                "githubassets.com",
                "github.io",
                "ghcr.io",
                "npm.pkg.github.com",
                "api.github.com"
            ]
        },
        new NetworkDomainGroup
        {
            Id = "custom",
            DisplayName = "Custom",
            Enabled = false,
            Domains = []
        }
    ];
}

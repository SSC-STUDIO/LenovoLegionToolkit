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
            Description = "Steam store, community and CDN resources",
            IconKey = "SteamLogo",
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
            ],
            SubItems =
            [
                new() { Id = "steam-store",    DisplayName = "Steam Store",    Domain = "store.steampowered.com" },
                new() { Id = "steam-community", DisplayName = "Steam Community", Domain = "steamcommunity.com" },
                new() { Id = "steam-images",   DisplayName = "Steam Images",   Domain = "steamcdn-a.akamaihd.net" },
                new() { Id = "steam-static",   DisplayName = "Steam Static",   Domain = "community.steamstatic.com" },
                new() { Id = "steam-update",   DisplayName = "Steam Update",   Domain = "media.steampowered.com", Enabled = false },
                new() { Id = "steam-cdn-fix",  DisplayName = "Steam CDN Fix",  Domain = "*.st.dl.eccdnx.com", Enabled = false, IsBeta = true },
            ]
        },
        new NetworkDomainGroup
        {
            Id = "github",
            DisplayName = "GitHub",
            Description = "GitHub code hosting and package registry",
            IconKey = "GitHubLogo",
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
            ],
            SubItems =
            [
                new() { Id = "github-main",    DisplayName = "GitHub Main",    Domain = "github.com" },
                new() { Id = "github-api",     DisplayName = "GitHub API",     Domain = "api.github.com" },
                new() { Id = "github-assets",  DisplayName = "GitHub Assets",  Domain = "githubassets.com" },
                new() { Id = "github-pages",   DisplayName = "GitHub Pages",   Domain = "github.io" },
                new() { Id = "github-packages", DisplayName = "GitHub Packages", Domain = "npm.pkg.github.com" },
            ]
        },
        new NetworkDomainGroup
        {
            Id = "twitch",
            DisplayName = "Twitch",
            Description = "Twitch live streaming and related services",
            IconKey = "TwitchLogo",
            Enabled = false,
            Domains =
            [
                "twitch.tv",
                "ttvnw.net",
                "jtvnw.net"
            ],
            SubItems =
            [
                new() { Id = "twitch-chat",    DisplayName = "Twitch Chat",    Domain = "irc-ws.chat.twitch.tv", Enabled = false },
                new() { Id = "twitch-login",   DisplayName = "Twitch Login",   Domain = "passport.twitch.tv", Enabled = false },
                new() { Id = "twitch-assets",  DisplayName = "Twitch Assets",  Domain = "assets.twitch.tv", Enabled = false },
                new() { Id = "twitch-drops",   DisplayName = "Twitch Drops",   Domain = "pubsub-edge.twitch.tv", Enabled = false },
                new() { Id = "twitch-service", DisplayName = "Twitch Service", Domain = "supervisor.ext-twitch.tv", Enabled = false },
            ]
        },
        new NetworkDomainGroup
        {
            Id = "public-cdn",
            DisplayName = "Public CDN",
            Description = "Common public CDN and translation services",
            IconKey = "CdnLogo",
            Enabled = false,
            Domains =
            [
                "translate.googleapis.com",
                "open.spotify.com"
            ],
            SubItems =
            [
                new() { Id = "google-translate", DisplayName = "Google Translate", Domain = "translate.googleapis.com", Enabled = false },
                new() { Id = "spotify",          DisplayName = "Spotify",          Domain = "open.spotify.com", Enabled = false },
            ]
        },
        new NetworkDomainGroup
        {
            Id = "roblox",
            DisplayName = "Roblox",
            Description = "Roblox game platform and CDN",
            IconKey = "RobloxLogo",
            Enabled = false,
            Domains =
            [
                "roblox.com",
                "rbxcdn.com",
                "roblox.plus"
            ],
            SubItems =
            [
                new() { Id = "roblox-main", DisplayName = "Roblox Main", Domain = "www.roblox.com", Enabled = false },
                new() { Id = "roblox-cdn",  DisplayName = "Roblox CDN",  Domain = "thumbnails.roblox.com", Enabled = false },
            ]
        },
        new NetworkDomainGroup
        {
            Id = "custom",
            DisplayName = "Custom",
            Description = "User-defined domains",
            IconKey = "CustomLogo",
            Enabled = false,
            Domains = []
        }
    ];
}

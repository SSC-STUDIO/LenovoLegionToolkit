using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.PackageDownloader.Detectors.Rules;

internal readonly partial struct BiosPackageRule : IPackageRule
{
    [GeneratedRegex("^[A-Z0-9]{4}")]
    private static partial Regex PrefixRegex();

    [GeneratedRegex("[0-9]{2}")]
    private static partial Regex VersionRegex();

    private string[] Levels { get; init; }

    public static bool TryCreate(XmlNode? node, out BiosPackageRule value)
    {
        var levels = node?.SelectNodes("Level")?
            .OfType<XmlNode>()
            .Select(n => n.InnerText)
            .ToArray() ?? [];

        if (levels.IsEmpty())
        {
            value = default;
            return false;
        }

        value = new BiosPackageRule { Levels = levels };
        return true;
    }

    public async Task<bool> CheckDependenciesSatisfiedAsync(List<DriverInfo> driverInfoCache, HttpClient httpClient, CancellationToken token)
    {
        var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
        var currentBios = mi.BiosVersion;

        var result = Levels.Any((global::System.Func<string, bool>)(level =>
            TryParseLevel(level, out var levelPrefix, out var levelVersion) &&
            currentBios.HasValue &&
            levelPrefix == currentBios.Value.Prefix &&
            levelVersion == currentBios.Value.Version));

        return result;
    }

    public async Task<bool> DetectInstallNeededAsync(List<DriverInfo> _1, HttpClient _2, CancellationToken _3)
    {
        var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
        var currentBios = mi.BiosVersion;

        var parsedLevels = Levels
            .Select(level => TryParseLevel(level, out var prefix, out var version)
                ? (IsValid: true, Prefix: prefix, Version: version)
                : (IsValid: false, Prefix: string.Empty, Version: 0))
            .Where(level => level.IsValid)
            .ToArray();

        var result = parsedLevels.Length != 0 && parsedLevels.All((global::System.Func<(bool IsValid, string Prefix, int Version), bool>)(level =>
            currentBios.HasValue &&
            level.Prefix == currentBios.Value.Prefix &&
            level.Version > currentBios.Value.Version));

        return result;
    }

    internal static bool TryParseLevel(string? level, out string prefix, out int version)
    {
        prefix = string.Empty;
        version = 0;

        if (string.IsNullOrWhiteSpace(level))
            return false;

        var prefixMatch = PrefixRegex().Match(level);
        var versionMatch = VersionRegex().Match(level);
        if (!prefixMatch.Success || !versionMatch.Success)
            return false;

        if (!int.TryParse(versionMatch.Value, out version))
            return false;

        prefix = prefixMatch.Value;
        return true;
    }
}

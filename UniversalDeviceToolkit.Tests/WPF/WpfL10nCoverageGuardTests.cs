using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

/// <summary>
/// CI gate: every WPF satellite must cover base Resource.resx keys, and priority
/// UI strings must not remain English copies (except short technical tokens).
/// </summary>
[Trait("Category", TestCategories.Unit)]
public sealed class WpfL10nCoverageGuardTests
{
    private static readonly string[] PriorityPrefixes =
    [
        "NetworkAcceleration",
        "PluginExtensions",
        "DeviceSetup",
        "CrashReport",
        "AppNotification",
        "SensorsControl",
        "WindowsOptimization",
        "MainWindow_Plugin",
        "NotificationsSettings",
        "FanCurve",
    ];

    private static readonly string[] RequiredCultures =
    [
        "ar", "bg", "cs", "de", "el", "en", "es", "fr", "hu", "it", "ja", "lv",
        "nl-nl", "pl", "pt", "pt-br", "ro", "ru", "sk", "tr", "uk", "uz-latn-uz",
        "vi", "zh-hans", "zh-hant",
    ];

    // Loanwords kept identical on purpose in many locales (not a missing translation).
    private static readonly Regex TechToken = new(
        @"^(DNS|DoH|Hosts|ms|KB/s|—|–|-|CPU|GPU|HDR|OSD|PAC|TLS|UDT|FPS|°C|°F|GB|GHz|1% Low|Nilesoft Shell|HWiNFO64|Over Drive|Microphone|Notifications|Exception|Diagnostics|Maximum: \{0\}|\{0\}%?|\{0\})$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [Fact]
    public void RequiredCultureSatellites_ShouldExist()
    {
        var dir = ResourcesDirectory();
        foreach (var culture in RequiredCultures)
        {
            File.Exists(Path.Combine(dir, $"Resource.{culture}.resx"))
                .Should().BeTrue($"Languages / shipping list requires Resource.{culture}.resx");
        }
    }

    [Fact]
    public void AllSatellites_ShouldContainEveryBaseKey()
    {
        var dir = ResourcesDirectory();
        var baseKeys = LoadKeys(Path.Combine(dir, "Resource.resx"));
        baseKeys.Count.Should().BeGreaterThan(1000);

        var failures = new List<string>();
        foreach (var path in Directory.EnumerateFiles(dir, "Resource.*.resx").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var keys = LoadKeys(path);
            var missing = baseKeys.Where(k => !keys.Contains(k)).Take(12).ToArray();
            if (missing.Length > 0)
            {
                failures.Add($"{Path.GetFileName(path)}: missing {baseKeys.Count(k => !keys.Contains(k))} keys (e.g. {string.Join(", ", missing)})");
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void PriorityUiStrings_ShouldNotRemainEnglish_OnNonEnglishCultures()
    {
        var dir = ResourcesDirectory();
        var baseMap = LoadMap(Path.Combine(dir, "Resource.resx"));
        var failures = new List<string>();

        foreach (var path in Directory.EnumerateFiles(dir, "Resource.*.resx").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var culture = Path.GetFileNameWithoutExtension(path)["Resource.".Length..];
            if (culture.Equals("en", StringComparison.OrdinalIgnoreCase))
                continue;

            var map = LoadMap(path);
            var leftovers = new List<string>();
            foreach (var (key, value) in map)
            {
                if (!IsPriority(key) || !baseMap.TryGetValue(key, out var english))
                    continue;
                if (!string.Equals(value, english, StringComparison.Ordinal))
                    continue;

                var stripped = english.Trim();
                if (TechToken.IsMatch(stripped) || stripped.Length <= 8)
                    continue;
                if (!Regex.IsMatch(stripped, "[A-Za-z]{4}"))
                    continue;

                leftovers.Add(key);
            }

            if (leftovers.Count > 0)
            {
                failures.Add(
                    $"{Path.GetFileName(path)}: {leftovers.Count} priority string(s) still English " +
                    $"(e.g. {string.Join(", ", leftovers.Take(6))})");
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void AssertWpfL10nCoverageScript_ShouldExistAndReferenceResourcesPath()
    {
        var script = Path.Combine(FindRepositoryRoot(), "Scripts", "Assert-WpfL10nCoverage.ps1");
        File.Exists(script).Should().BeTrue();
        var text = File.ReadAllText(script);
        text.Should().Contain("UniversalDeviceToolkit.WPF");
        text.Should().Contain("Resource.resx");
        text.Should().Contain("english");
    }

    private static bool IsPriority(string key) =>
        PriorityPrefixes.Any(p => key.StartsWith(p, StringComparison.Ordinal));

    private static string ResourcesDirectory() =>
        Path.Combine(FindRepositoryRoot(), "UniversalDeviceToolkit.WPF", "Resources");

    private static HashSet<string> LoadKeys(string path)
    {
        // Prefer regex over XDocument: some historical resx contain characters that
        // trip strict XML parsers while still loading fine via ResXResourceReader.
        var raw = File.ReadAllText(path);
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in Regex.Matches(raw, "<data name=\"([^\"]+)\""))
            set.Add(match.Groups[1].Value);
        return set;
    }

    private static Dictionary<string, string> LoadMap(string path)
    {
        var raw = File.ReadAllText(path);
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in Regex.Matches(
                     raw,
                     "<data name=\"([^\"]+)\"[^>]*>\\s*<value>([\\s\\S]*?)</value>"))
        {
            map[match.Groups[1].Value] = match.Groups[2].Value;
        }

        return map;
    }

    private static string FindRepositoryRoot()
    {
        var overrideRoot = Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT");
        if (!string.IsNullOrWhiteSpace(overrideRoot) &&
            File.Exists(Path.Combine(overrideRoot, "UniversalDeviceToolkit.sln")))
        {
            return Path.GetFullPath(overrideRoot);
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}

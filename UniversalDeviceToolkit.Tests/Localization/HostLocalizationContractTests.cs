using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.CLI;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Localization;

[Collection(TestCollections.Localization)]
[Trait("Category", TestCategories.Unit)]
public sealed class HostLocalizationContractTests
{
    private static readonly HostResource[] Hosts =
    [
        new("UniversalDeviceToolkit.WPF/Resources", "Resource.resx", "Resource.*.resx"),
        new("UniversalDeviceToolkit.CLI/Resources", "CLI.Resources.resx", "CLI.Resources.*.resx"),
        new("UniversalDeviceToolkit.CrossPlatform/Resources", "Resource.resx", "Resource.*.resx"),
        new("Tools/Installer/Resources", "Resource.resx", "Resource.*.resx"),
    ];

    [Fact]
    public void HostResourceFiles_ShouldBeWellFormedAndKeepKeysAndPlaceholdersAligned()
    {
        var root = RepositoryPaths.FindRoot();

        foreach (var host in Hosts)
        {
            var directory = Path.Combine(root, host.RelativeDirectory.Replace('/', Path.DirectorySeparatorChar));
            var neutralPath = Path.Combine(directory, host.NeutralFileName);
            var neutral = ReadValues(neutralPath);

            neutral.Should().NotBeEmpty($"{host.RelativeDirectory} must define a neutral resource set");
            neutral.Keys.Should().OnlyHaveUniqueItems();
            neutral.Values.Should().OnlyContain(value => !string.IsNullOrWhiteSpace(value));

            foreach (var satellitePath in Directory.EnumerateFiles(directory, host.SatellitePattern))
            {
                var satelliteName = Path.GetFileNameWithoutExtension(satellitePath);
                var prefix = host.NeutralFileName.StartsWith("CLI.", StringComparison.Ordinal)
                    ? "CLI.Resources."
                    : "Resource.";
                var cultureName = satelliteName[prefix.Length..];

                LocalizationCatalog.SupportedCultures
                    .Should().Contain(culture => culture.Name.Equals(cultureName, StringComparison.OrdinalIgnoreCase),
                        $"{satellitePath} must use a culture from the shared catalog");

                var localized = ReadValues(satellitePath);
                localized.Keys.Should().Contain(neutral.Keys,
                    $"{satellitePath} must contain every neutral resource key");

                foreach (var key in neutral.Keys)
                {
                    localized[key].Should().NotBeNullOrWhiteSpace($"{satellitePath}:{key}");
                    FormatIndexes(localized[key]).Should().BeEquivalentTo(FormatIndexes(neutral[key]),
                        $"{satellitePath}:{key} must preserve format placeholders");
                }
            }
        }
    }

    [Fact]
    public void EverySupportedCulture_ShouldResolveThroughCompiledHostResources()
    {
        var probes = new[]
        {
            new ResourceProbe(
                "WPF",
                new ResourceManager(
                    "UniversalDeviceToolkit.WPF.Resources.Resource",
                    typeof(UniversalDeviceToolkit.WPF.Resources.Resource).Assembly),
                "AboutPage_Build"),
            new ResourceProbe(
                "Lib",
                new ResourceManager(
                    "UniversalDeviceToolkit.Lib.Resources.Resource",
                    typeof(UniversalDeviceToolkit.Lib.Resources.Resource).Assembly),
                "AccentColorSource_Custom"),
            new ResourceProbe(
                "Automation",
                new ResourceManager(
                    "UniversalDeviceToolkit.Lib.Automation.Resources.Resource",
                    typeof(UniversalDeviceToolkit.Lib.Automation.Resources.Resource).Assembly),
                "ACAdapterConnectedAutomationPipelineTrigger_DisplayName"),
            new ResourceProbe(
                "Macro",
                new ResourceManager(
                    "UniversalDeviceToolkit.Lib.Macro.Resources.Resource",
                    typeof(UniversalDeviceToolkit.Lib.Macro.Resources.Resource).Assembly),
                "MacroSource_Keyboard"),
            new ResourceProbe(
                "Plugins",
                new ResourceManager(
                    "UniversalDeviceToolkit.Lib.Plugins.Resources.Resource",
                    typeof(UniversalDeviceToolkit.Lib.Plugins.Resources.Resource).Assembly),
                "Plugin_Error_DependencyResolution_Circular"),
            new ResourceProbe(
                "CLI",
                new ResourceManager(
                    "UniversalDeviceToolkit.CLI.Resources.CLI.Resources",
                    typeof(Strings).Assembly),
                "CLI_Header_RootCommandDescription"),
        };

        foreach (var culture in LocalizationCatalog.SupportedCultures)
        {
            foreach (var probe in probes)
            {
                var value = LocalizationCatalog.GetString(probe.Manager, probe.Key, string.Empty, culture);

                value.Should().NotBeNullOrWhiteSpace(
                    $"{probe.Name} must resolve '{probe.Key}' for {culture.Name}");
                if (!LocalizationCatalog.IsChinese(culture))
                    value.Should().NotContain("设备", $"{probe.Name} must not leak Chinese fallback for {culture.Name}");
            }
        }
    }

    private static Dictionary<string, string> ReadValues(string path)
    {
        var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        return document.Root!
            .Elements("data")
            .ToDictionary(
                element => (string)element.Attribute("name")!,
                element => element.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);
    }

    private static int[] FormatIndexes(string value) =>
        Regex.Matches(value, @"\{(\d+)(?:[,}:])")
            .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
            .OrderBy(index => index)
            .ToArray();

    private sealed record HostResource(string RelativeDirectory, string NeutralFileName, string SatellitePattern);

    private sealed record ResourceProbe(string Name, ResourceManager Manager, string Key);
}

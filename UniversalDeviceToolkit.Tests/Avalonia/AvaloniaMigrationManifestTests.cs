using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

/// <summary>
/// Guards the migration inventory before the desktop visual audit is enabled.
/// These checks intentionally compare source manifests and resource catalogs,
/// not runtime screenshots, so a missing Avalonia route or localization key
/// fails before visual evidence is collected.
/// </summary>
[Trait("Category", TestCategories.Guard)]
public sealed class AvaloniaMigrationManifestTests
{
    private static readonly string[] MainRoutes =
    [
        "dashboard",
        "keyboardBacklight",
        "automation",
        "macro",
        "windowsOptimization",
        "pluginExtensions",
        "settings",
        "about",
    ];

    private static readonly string[] SettingsRoutes =
    [
        "Appearance",
        "Application",
        "SmartKeys",
        "Display",
        "Update",
        "Power",
        "Integrations",
    ];

    [Fact]
    public void MainNavigation_ContainsEveryWpfPageRoute()
    {
        var root = RepositoryPaths.FindRoot();
        var wpfMarkup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.WPF",
            "Windows",
            "MainWindow.xaml"));
        var avaloniaSource = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "MainNavigation.cs"));
        var avaloniaMarkup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "MainWindow.axaml.cs"));

        foreach (var route in MainRoutes)
        {
            wpfMarkup.Should().Contain($"PageTag=\"{route}\"");
            var normalized = route.ToLowerInvariant();
            avaloniaSource.Should().Contain(normalized);
            avaloniaMarkup.Should().Contain("MainNavigation");
        }
    }

    [Fact]
    public void SettingsNavigation_ContainsEveryWpfSettingsCapability()
    {
        var root = RepositoryPaths.FindRoot();
        var wpfMarkup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.WPF",
            "Pages",
            "SettingsPage.xaml.cs"));
        var avaloniaSource = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "SettingsPageViewModel.cs"));

        foreach (var route in SettingsRoutes)
        {
            wpfMarkup.Should().Contain(route);
            avaloniaSource.Should().Contain(route);
        }

        avaloniaSource.Should().NotContain("BuildPlaceholderView");
    }

    [Fact]
    public void AvaloniaResources_ContainEveryWpfResourceKeyForEveryCulture()
    {
        var root = RepositoryPaths.FindRoot();
        var wpfDirectory = Path.Combine(root, "UniversalDeviceToolkit.WPF", "Resources");
        var avaloniaDirectory = Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Resources");

        foreach (var wpfFile in Directory.EnumerateFiles(wpfDirectory, "Resource*.resx"))
        {
            var avaloniaFile = Path.Combine(avaloniaDirectory, Path.GetFileName(wpfFile));
            File.Exists(avaloniaFile).Should().BeTrue($"Avalonia resource catalog {Path.GetFileName(wpfFile)} should exist");

            var wpfKeys = ReadResourceKeys(wpfFile);
            var avaloniaKeys = ReadResourceKeys(avaloniaFile);
            avaloniaKeys.Should().Contain(wpfKeys, $"Avalonia should preserve every WPF key in {Path.GetFileName(wpfFile)}");
        }
    }

    private static IReadOnlySet<string> ReadResourceKeys(string path) =>
        XDocument.Load(path)
            .Root?
            .Elements("data")
            .Select(element => (string?)element.Attribute("name"))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal)
        ?? new HashSet<string>(StringComparer.Ordinal);
}

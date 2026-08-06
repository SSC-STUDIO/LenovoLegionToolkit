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

    [Fact]
    public void OfficialPlugins_ExposeAvaloniaPagesAndSharedCoreRuntime()
    {
        var root = RepositoryPaths.FindRoot();
        var pluginsRoot = Path.Combine(root, "Plugins", "Official");
        var pluginNames = new[] { "CustomMouse", "ShellIntegration", "ViveTool" };

        foreach (var pluginName in pluginNames)
        {
            var pluginDirectory = Path.Combine(pluginsRoot, pluginName);
            var projectPath = Directory.EnumerateFiles(pluginDirectory, "*.csproj")
                .Single(path => !path.Contains(".Tests", StringComparison.OrdinalIgnoreCase));
            var project = File.ReadAllText(projectPath);
            project.Should().Contain("UniversalDeviceToolkit.Plugins.Shared.Core.csproj");
            project.Should().Contain("<PackageReference Include=\"Avalonia\" />");

            var source = string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(pluginDirectory, "*.cs", SearchOption.AllDirectories)
                    .Where(path => !path.Split(Path.DirectorySeparatorChar)
                        .Any(segment => segment.Equals("obj", StringComparison.OrdinalIgnoreCase)
                            || segment.Equals("bin", StringComparison.OrdinalIgnoreCase)))
                    .Select(File.ReadAllText));
            source.Should().Contain("CreateAvaloniaPage", $"{pluginName} must expose an Avalonia page factory");

            var manifest = File.ReadAllText(Path.Combine(pluginDirectory, "plugin.manifest.json"));
            manifest.Should().Contain("UniversalDeviceToolkit.Plugins.Shared.Core.dll");
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

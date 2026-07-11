using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class DesignTokenGuardTests
{
    private static readonly Regex LiteralCornerRadius = new(
        "CornerRadius=\\\"(?:4|6|8|10|12|8,8,0,0|0,0,8,8)\\\"",
        RegexOptions.Compiled);

    [Fact]
    public void ApplicationXaml_ShouldUseSemanticCornerRadiusTokens()
    {
        var repositoryRoot = FindRepositoryRoot();
        var wpfRoot = Path.Combine(repositoryRoot, "UniversalDeviceToolkit.WPF");
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine(wpfRoot, "Windows", "Osd", "OsdBarWindow.xaml"),
            Path.Combine(wpfRoot, "Windows", "Osd", "OsdPanelWindow.xaml")
        };

        var violations = Directory.EnumerateFiles(wpfRoot, "*.xaml", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                           !allowed.Contains(path))
            .SelectMany(path => File.ReadLines(path).Select((line, index) => new { path, line, index }))
            .Where(item => LiteralCornerRadius.IsMatch(item.line))
            .Select(item => $"{item.path}:{item.index + 1}: {item.line.Trim()}")
            .ToArray();

        violations.Should().BeEmpty();
    }

    [Fact]
    public void DesignTokens_ShouldExposeModernRadiusHierarchy()
    {
        var repositoryRoot = FindRepositoryRoot();
        var tokens = File.ReadAllText(Path.Combine(repositoryRoot, "UniversalDeviceToolkit.WPF", "Styles", "DesignTokens.xaml"));
        tokens.Should().Contain("x:Key=\"CornerRadiusCompact\">8<");
        tokens.Should().Contain("x:Key=\"CornerRadiusControl\">12<");
        tokens.Should().Contain("x:Key=\"CornerRadiusCard\">18<");
        tokens.Should().Contain("x:Key=\"CornerRadiusSurface\">20<");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "UniversalDeviceToolkit.sln")))
            current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("Could not find repository root.");
    }
}

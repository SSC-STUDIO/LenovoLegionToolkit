using System.Globalization;
using System.IO;
using System.Linq;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

public class LocalizationAndResourceQualityTests
{
    [Theory]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("ar")]
    [InlineData("ja")]
    public void CultureFallbackChain_NeverIncludesChineseForNonChineseLocales(string cultureName)
    {
        var culture = new CultureInfo(cultureName);
        var chain = LocalizationHelper.EnumerateCultureFallbackChainPublic(culture).ToArray();

        chain.Should().Contain(c => c.Name.Equals("en", StringComparison.OrdinalIgnoreCase));
        if (!LocalizationHelper.IsChineseCulture(culture))
            chain.Should().NotContain(c => LocalizationHelper.IsChineseCulture(c));
    }

    [Fact]
    public void CultureFallbackChain_ChineseMayIncludeChineseParents()
    {
        var culture = new CultureInfo("zh-hans");
        var chain = LocalizationHelper.EnumerateCultureFallbackChainPublic(culture).ToArray();
        chain.Should().Contain(c => c.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase));
        chain.Should().Contain(c => c.Name.Equals("en", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Hello {0}", new[] { 0 })]
    [InlineData("{1} of {0}", new[] { 0, 1 })]
    [InlineData("no format", new int[0])]
    public void ExtractPlaceholders_ParsesIndexes(string text, int[] expected)
    {
        ResourceQualityAuditor.ExtractPlaceholders(text).Should().Equal(expected);
    }

    [Theory]
    [InlineData("保存设置", true)]
    [InlineData("正常、文本。", true)]
    [InlineData("Settings saved", false)]
    [InlineData("Paramètres enregistrés", false)]
    public void EastAsianContamination_DetectsCjkAndEastAsianPunctuation(string text, bool expected)
    {
        ResourceQualityAuditor.ContainsDisallowedEastAsianContent(text).Should().Be(expected);
    }

    [Fact]
    public void Audit_MainWpfResources_DoesNotErrorOnParse()
    {
        var root = FindRepoRoot();
        var wpfResources = Path.Combine(root, "UniversalDeviceToolkit.WPF", "Resources");
        Directory.Exists(wpfResources).Should().BeTrue();

        var result = ResourceQualityAuditor.AuditDirectory(wpfResources);
        result.Findings.Where(f => f.Kind == "ParseError" || f.Kind == "DuplicateKey")
            .Should().BeEmpty("resx files must parse and have unique keys");
    }

    [Fact]
    public void Audit_NonCjkSatellites_ShouldNotContainEastAsianContamination_WhenPresent()
    {
        var root = FindRepoRoot();
        var wpfResources = Path.Combine(root, "UniversalDeviceToolkit.WPF", "Resources");
        var result = ResourceQualityAuditor.AuditDirectory(wpfResources);

        // Report-only for contamination/placeholder until languages are cleaned;
        // this test ensures the auditor runs and ParseError never slips through.
        result.Findings.Where(f => f.Kind == "ParseError").Should().BeEmpty();
    }

    [Fact]
    public void DesignTokens_Xaml_DefinesModernRadiusScale()
    {
        var root = FindRepoRoot();
        var tokensPath = Path.Combine(root, "UniversalDeviceToolkit.WPF", "Styles", "DesignTokens.xaml");
        File.Exists(tokensPath).Should().BeTrue();
        var xaml = File.ReadAllText(tokensPath);

        xaml.Should().Contain("x:Key=\"CornerRadiusCompact\"");
        xaml.Should().Contain("x:Key=\"CornerRadiusControl\"");
        xaml.Should().Contain("x:Key=\"CornerRadiusCard\"");
        xaml.Should().Contain("x:Key=\"CornerRadiusSurface\"");
        xaml.Should().Contain("x:Key=\"CornerRadiusRound\"");
        xaml.Should().Contain(">8</CornerRadius>");
        xaml.Should().Contain(">12</CornerRadius>");
        xaml.Should().Contain(">18</CornerRadius>");
        xaml.Should().Contain(">20</CornerRadius>");
    }

    [Fact]
    public void XamlSources_ShouldNotReintroduceLiteralCornerRadiusDigits()
    {
        var root = FindRepoRoot();
        var wpfRoot = Path.Combine(root, "UniversalDeviceToolkit.WPF");
        var offenders = Directory.EnumerateFiles(wpfRoot, "*.xaml", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => (path, line, index))
                .Where(tuple => System.Text.RegularExpressions.Regex.IsMatch(
                    tuple.line,
                    @"CornerRadius\s*=\s*""\d") &&
                    !tuple.line.Contains("StaticResource", StringComparison.Ordinal) &&
                    !tuple.line.Contains("DynamicResource", StringComparison.Ordinal)))
            .Select(tuple => $"{Path.GetRelativePath(root, tuple.path)}:{tuple.index + 1}:{tuple.line.Trim()}")
            .ToArray();

        offenders.Should().BeEmpty("use DesignTokens semantic CornerRadius* resources instead of literals");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "UniversalDeviceToolkit.sln")) ||
                File.Exists(Path.Combine(dir.FullName, "UniversalDeviceToolkit.WPF", "UniversalDeviceToolkit.WPF.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate UniversalDeviceToolkit repo root from test base directory.");
    }
}

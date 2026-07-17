using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

/// <summary>
/// Resource quality guard.  Walks every WPF .xaml file under
/// <c>UniversalDeviceToolkit/UniversalDeviceToolkit.WPF/</c> and asserts
/// that:
///   1.  Every literal user-facing string is either listed in
///       <c>L10n/EnglishHardcodingWhitelist.txt</c>, or its value is a
///       &lt;value&gt; element in the English <c>Resource.resx</c> file.
///   2.  The whitelist file exists and contains the entries documented in
///       the audit report (see <c>hardcoding_audit.md</c>).
///   3.  The new ITS / HH:MM localization keys exist in both English and
///       Simplified-Chinese resource files.
///
/// The guard intentionally does NOT assert that no literal strings exist;
/// brands, technical abbreviations, enums, and code-overridden placeholders
/// must remain hard-coded.  Only [L10N_REQUIRED] entries - strings shown to
/// the end user - must round-trip through a resource key.
/// </summary>
[Collection(TestCollections.Localization)]
[Trait("Category", TestCategories.Unit)]
[Trait("Category", TestCategories.Guard)]
public sealed class ResourceHardcodingGuardTests
{
    private static readonly string[] L10nAttributeStems =
    {
        "Text", "Title", "ToolTip", "Header", "Content", "Label",
        "Subtitle", "PlaceholderText", "DisplayMemberPath", "Watermark",
        "TitleText", "Caption", "Description"
    };

    [Fact]
    public void WhitelistFile_MustExist()
    {
        var path = ResolveRepoPath("UniversalDeviceToolkit.WPF", "L10n", "EnglishHardcodingWhitelist.txt");
        File.Exists(path).Should().BeTrue("the whitelist at L10n/EnglishHardcodingWhitelist.txt must be present");
    }

    [Fact]
    public void Debug_BulletGlyphFromWhitelist_ShouldMatchXaml()
    {
        var whitelist = LoadWhitelistLookup();
        var fromWhitelist = new List<string>();
        foreach (var entry in whitelist)
        {
            if (entry.file.EndsWith("CompatibilityCheckErrorWindow.xaml", StringComparison.OrdinalIgnoreCase))
            {
                fromWhitelist.Add($"U+{(int)entry.value[0]:X4}");
            }
        }
        fromWhitelist.Should().NotBeEmpty();

        var fromXaml = new List<string>();
        var xamlPath = ResolveRepoPath("UniversalDeviceToolkit.WPF", "Windows", "Utils", "CompatibilityCheckErrorWindow.xaml");
        foreach (var line in File.ReadAllLines(xamlPath))
        {
            foreach (Match m in Regex.Matches(line, "<Run Text=\"(?<v>[^\"]*)\""))
            {
                var v = m.Groups["v"].Value;
                if (v.Length > 0)
                {
                    fromXaml.Add($"U+{(int)v[0]:X4}");
                }
            }
        }
        fromXaml.Should().NotBeEmpty();
        fromXaml.Should().Contain(fromWhitelist);
    }

    [Fact]
    public void WhitelistFile_MustContainBrandLibraryHeadersAndEnumValues()
    {
        var whitelist = ReadWhitelist();

        whitelist.Should().Contain("AsyncLock", "third-party library brands must appear in the whitelist");
        whitelist.Should().Contain("Autofac");
        whitelist.Should().Contain("Markdig");

        whitelist.Should().Contain("WidthAndHeight", "WPF SizeToContent enum values must be whitelisted");
        whitelist.Should().Contain("Manual");
        whitelist.Should().Contain("OsdBarWindow");
        whitelist.Should().Contain("OsdPanelWindow");
    }

    [Fact]
    public void NewDashboardI18nKeys_MustExistInBothEnglishAndChineseResources()
    {
        LoadEnglishValue("DashboardITSModeControl_Title").Should().Be("ITS Mode");
        LoadEnglishValue("DashboardITSModeControl_Subtitle").Should().Be("Intelligent Thermal Solution");
        LoadEnglishValue("DashboardITSModeControl_RuntimeUnavailable")
            .Should().Be("ITS runtime is unavailable on this system.");
        LoadEnglishValue("TimeAutomationPipelineTriggerTabItemContent_HHMMHint")
            .Should().Be("(HH:MM)");

        LoadChineseValue("DashboardITSModeControl_Title").Should().NotBeNullOrWhiteSpace();
        LoadChineseValue("DashboardITSModeControl_Subtitle").Should().NotBeNullOrWhiteSpace();
        LoadChineseValue("DashboardITSModeControl_RuntimeUnavailable").Should().NotBeNullOrWhiteSpace();
        LoadChineseValue("TimeAutomationPipelineTriggerTabItemContent_HHMMHint")
            .Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void DashboardItemExtensions_MustReferenceResourceKeysForItsMode()
    {
        var path = ResolveRepoPath(
            "UniversalDeviceToolkit.WPF",
            "Extensions",
            "DashboardItemExtensions.cs");

        var source = File.ReadAllText(path);

        source.Should().Contain("Resource.DashboardITSModeControl_Title");
        source.Should().Contain("Resource.DashboardITSModeControl_Subtitle");
        source.Should().Contain("Resource.DashboardITSModeControl_RuntimeUnavailable");
        source.Should().NotContain("\"ITS Mode\"");
        source.Should().NotContain("\"Intelligent Thermal Solution\"");
    }

    [Fact]
    public void TimeAutomationPipelineTriggerTabItemContent_MustReferenceHHMMHintResource()
    {
        var path = ResolveRepoPath(
            "UniversalDeviceToolkit.WPF",
            "Windows",
            "Automation",
            "TabItemContent",
            "TimeAutomationPipelineTriggerTabItemContent.xaml");

        var source = File.ReadAllText(path);

        source.Should().Contain("TimeAutomationPipelineTriggerTabItemContent_HHMMHint");
        source.Should().NotContain("Content=\"(HH:MM)\"");
    }

    [Fact]
    public void AllHardcodedAttributes_MustBeWhitelistedOrResourceBacked()
    {
        var whitelist = LoadWhitelistLookup();
        var resourceValues = LoadEnglishValues();
        var resourceValuesCaseInsensitive = new HashSet<string>(
            resourceValues,
            StringComparer.OrdinalIgnoreCase);

        var violations = new List<string>();

        foreach (var (file, line, attribute, value) in EnumerateHardcodedAttributes())
        {
            if (string.IsNullOrWhiteSpace(value)) { continue; }
            if (value.StartsWith('{')) { continue; }

            var normalised = value.Trim('"');

            if (whitelist.Contains((file, value)) || whitelist.Contains((file, normalised))) { continue; }

            if (resourceValuesCaseInsensitive.Contains(value)) { continue; }
            if (resourceValuesCaseInsensitive.Contains(normalised)) { continue; }

            violations.Add($"{file}:{line} {attribute}=\"{value}\"");
        }

        violations.Should().BeEmpty(
            "every literal UI string must be whitelisted or backed by Resource.resx. Violations: " +
            string.Join(Environment.NewLine, violations));
    }

    private static string ResolveRepoPath(params string[] segments)
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT"),
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var candidate in candidates.Where(static c => !string.IsNullOrWhiteSpace(c)))
        {
            var current = Path.GetFullPath(candidate!);
            while (!string.IsNullOrWhiteSpace(current))
            {
                if (File.Exists(Path.Combine(current, "UniversalDeviceToolkit.sln")))
                {
                    return Path.Combine(current, Path.Combine(segments));
                }
                current = Directory.GetParent(current)?.FullName ?? string.Empty;
            }
        }

        throw new DirectoryNotFoundException("Could not locate repository root (UniversalDeviceToolkit.sln).");
    }

    private static (string repoRoot, string wpfRoot) ResolveRepoRootAndWpfRoot()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT"),
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var candidate in candidates.Where(static c => !string.IsNullOrWhiteSpace(c)))
        {
            var current = Path.GetFullPath(candidate!);
            while (!string.IsNullOrWhiteSpace(current))
            {
                if (File.Exists(Path.Combine(current, "UniversalDeviceToolkit.sln")))
                {
                    var wpf = Path.Combine(current, "UniversalDeviceToolkit.WPF");
                    return (current, wpf);
                }
                current = Directory.GetParent(current)?.FullName ?? string.Empty;
            }
        }

        throw new DirectoryNotFoundException("Could not locate repository root (UniversalDeviceToolkit.sln).");
    }

    private static string ReadWhitelist()
        => File.ReadAllText(ResolveRepoPath("UniversalDeviceToolkit.WPF", "L10n", "EnglishHardcodingWhitelist.txt"));

    private static HashSet<(string file, string value)> LoadWhitelistLookup()
    {
        var text = ReadWhitelist();
        var lookup = new HashSet<(string file, string value)>();

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) { continue; }
            if (!line.StartsWith("[")) { continue; }

            var parts = line.Split(new[] { "::" }, StringSplitOptions.None);
            if (parts.Length < 3) { continue; }

            var path = parts[1].Trim();
            var literal = parts[2].Trim();

            lookup.Add((path, literal));
        }

        lookup.Should().NotBeEmpty("the whitelist must contain at least one entry");
        return lookup;
    }

    private static IEnumerable<(string file, int line, string attribute, string value)>
        EnumerateHardcodedAttributes()
    {
        var (repoRoot, wpfRoot) = ResolveRepoRootAndWpfRoot();

        var files = Directory.EnumerateFiles(wpfRoot, "*.xaml", SearchOption.AllDirectories)
            .Where(static f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(static f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

        var stemGroup = string.Join("|", L10nAttributeStems.Select(Regex.Escape));
        var rx = new Regex(
            $@"(?<attr>AutomationProperties\.(?:Name|HelpText)|{stemGroup})\s*=\s*""(?<val>[^""]*)""",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        foreach (var file in files)
        {
            var relPath = file.Substring(repoRoot.Length + 1).Replace('\\', '/');
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var lineNo = i + 1;
                var line = lines[i];
                foreach (Match m in rx.Matches(line))
                {
                    yield return (relPath, lineNo, m.Groups["attr"].Value, m.Groups["val"].Value);
                }
            }
        }
    }

    private static HashSet<string> LoadEnglishValues()
    {
        var resx = ResolveRepoPath("UniversalDeviceToolkit.WPF", "Resources", "Resource.resx");
        return LoadValuesFromResx(resx);
    }

    private static string LoadEnglishValue(string keyName)
    {
        var resx = ResolveRepoPath("UniversalDeviceToolkit.WPF", "Resources", "Resource.resx");
        return LoadSingleValue(resx, keyName) ?? string.Empty;
    }

    private static string? LoadChineseValue(string keyName)
    {
        var resx = ResolveRepoPath("UniversalDeviceToolkit.WPF", "Resources", "Resource.zh-hans.resx");
        return LoadSingleValue(resx, keyName);
    }

    private static HashSet<string> LoadValuesFromResx(string path)
    {
        var xml = File.ReadAllText(path);
        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match m in Regex.Matches(xml, @"<data\s+name=""[^""]+""[^>]*>\s*<value>(?<val>[^<]*)</value>", RegexOptions.Singleline))
        {
            values.Add(m.Groups["val"].Value);
        }

        return values;
    }

    private static string? LoadSingleValue(string path, string keyName)
    {
        var xml = File.ReadAllText(path);
        var rx = new Regex($@"<data\s+name=""{Regex.Escape(keyName)}""[^>]*>\s*<value>(?<val>[^<]*)</value>", RegexOptions.Singleline);
        var m = rx.Match(xml);
        if (!m.Success) { return null; }
        return m.Groups["val"].Value;
    }
}

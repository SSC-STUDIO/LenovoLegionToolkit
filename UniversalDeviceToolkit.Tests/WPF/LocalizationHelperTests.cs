using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Utils;
using Xunit;
using Xunit.Abstractions;

namespace UniversalDeviceToolkit.Tests.WPF;

/// <summary>
/// Verifies the <see cref="LocalizationHelper"/> runtime behaviour plus the static
/// shape of the WPF resource catalog.  File-based cross-language consistency
/// (key set parity, placeholder parity, CJK purity in non-CJK languages) lives in
/// <see cref="ResourceQualityTests"/>; this class intentionally keeps a minimal
/// surface that does NOT duplicate that work and instead covers:
///   * <see cref="SetLanguageAsync_ShouldPersistCultureNameToLangFile"/> — runtime
///     behaviour of the language picker.
///   * <see cref="NeutralResource_ShouldHaveNonEmptyValuesForEveryKey"/> — every
///     neutral <c>Resource.resx</c> entry carries a non-empty value.
///   * <see cref="NeutralResource_ShouldExposeStableKeyCount"/> — the catalog is
///     non-trivially sized.
///   * <see cref="EnglishResource_ShouldNotEmbedCjkCharacters"/> — the English
///     neutral file is CJK-free (CJK in non-CJK languages is enforced by
///     <see cref="ResourceQualityTests.NonEastAsianLanguages_ShouldNotContainCjkCharacters"/>).
/// All file-based checks skip when the resource tree is not present.
/// </summary>
[Collection(TestCollections.Localization)]
[Trait("Category", TestCategories.Unit)]
public sealed class LocalizationHelperTests : IDisposable
{
    private static readonly Regex CjkCharacterRegex = new(@"[\u3000-\u303F\u3400-\u4DBF\u4E00-\u9FFF]", RegexOptions.Compiled);

    private readonly string _tempAppData;
    private readonly string? _previousAppDataOverride;
    private readonly ITestOutputHelper _output;

    public LocalizationHelperTests(ITestOutputHelper output)
    {
        _output = output;
        _tempAppData = Path.Combine(Path.GetTempPath(), $"udt-lang-helper-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempAppData);
        _previousAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, _tempAppData);
    }

    [Fact]
    public async Task SetLanguageAsync_ShouldPersistCultureNameToLangFile()
    {
        await LocalizationHelper.SetLanguageAsync(new CultureInfo("de"));

        var langPath = Path.Combine(Folders.AppData, "lang");
        File.Exists(langPath).Should().BeTrue();
        (await File.ReadAllTextAsync(langPath)).Trim().Should().Be("de");
        CultureInfo.CurrentUICulture.Name.Should().Be("de");
        LocalizationRuntime.CurrentCulture.Name.Should().Be("de");
    }

    [Theory]
    [InlineData("en-US", "en")]
    [InlineData("zh-TW", "zh-Hant")]
    public async Task SetLanguageAsync_ShouldPersistCanonicalCultureName(string requested, string expected)
    {
        await LocalizationHelper.SetLanguageAsync(new CultureInfo(requested));

        var langPath = Path.Combine(Folders.AppData, "lang");
        (await File.ReadAllTextAsync(langPath)).Trim().Should().Be(expected);
        CultureInfo.CurrentUICulture.Name.Should().Be(expected);
        LocalizationRuntime.CurrentCulture.Name.Should().Be(expected);
    }

    [SkippableFact]
    public void NeutralResource_ShouldHaveNonEmptyValuesForEveryKey()
    {
        var values = TryReadNeutralResourceValues();
        Skip.If(values is null, "neutral Resource.resx not found");

        values.Should().NotBeEmpty();
        foreach (var (key, value) in values!)
        {
            value.Should().NotBeNullOrWhiteSpace($"resource key '{key}' has empty value");
        }
    }

    [SkippableFact]
    public void NeutralResource_ShouldExposeStableKeyCount()
    {
        var values = TryReadNeutralResourceValues();
        Skip.If(values is null, "neutral Resource.resx not found");

        values!.Count.Should().BeGreaterThan(100, "the WPF resource catalog must contain a non-trivial number of keys");
    }

    [SkippableFact]
    public void EnglishResource_ShouldNotEmbedCjkCharacters()
    {
        var english = TryReadEnglishResourceValues();
        Skip.If(english is null, "English Resource.en.resx not found");

        var violations = new List<string>();
        foreach (var (key, value) in english!)
        {
            if (ContainsCjk(value))
            {
                violations.Add($"{key}: {value}");
            }
        }

        if (violations.Count > 0)
        {
            _output.WriteLine("English (en) resource contains CJK characters:");
            foreach (var violation in violations.Take(25))
                _output.WriteLine($"  - {violation}");
        }

        violations.Should().BeEmpty("the English neutral resource must not contain CJK characters");
    }

    public void Dispose()
    {
        // SetLanguageAsync mutates process-wide UI culture and Resource.Culture statics.
        // Restore English so parallel unit tests that assert English plugin/host strings
        // cannot observe a leaked "de" (or other) culture after this fixture runs.
        try
        {
            var english = CultureInfo.GetCultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentCulture = english;
            System.Threading.Thread.CurrentThread.CurrentUICulture = english;
            CultureInfo.DefaultThreadCurrentCulture = english;
            CultureInfo.DefaultThreadCurrentUICulture = english;
            LocalizationRuntime.SetCultureAsync("en", persist: false).GetAwaiter().GetResult();
            LocalizationHelper.ApplyCoreResourceCultures(english);
            UnitTestBase.ForceKnownResourceCultures(english);
        }
        catch
        {
            // best-effort; tests may run without full WPF stack
        }

        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, _previousAppDataOverride);

        try
        {
            if (Directory.Exists(_tempAppData))
                Directory.Delete(_tempAppData, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    private static bool ContainsCjk(string value) =>
        !string.IsNullOrEmpty(value) && CjkCharacterRegex.IsMatch(value);

    private static Dictionary<string, string>? TryReadNeutralResourceValues()
    {
        var path = FindResourceFile("Resource.resx");
        return path is null ? null : ReadResourceValues(path);
    }

    private static Dictionary<string, string>? TryReadEnglishResourceValues()
    {
        var path = FindResourceFile("Resource.en.resx") ?? FindResourceFile("Resource.resx");
        return path is null ? null : ReadResourceValues(path);
    }

    private static string? FindResourceFile(string fileName)
    {
        var roots = new[]
        {
            Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT"),
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var root in roots.Where(static root => !string.IsNullOrWhiteSpace(root)))
        {
            var directory = new DirectoryInfo(Path.GetFullPath(root!));
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "UniversalDeviceToolkit.WPF", "Resources", fileName);
                if (File.Exists(candidate))
                    return candidate;

                directory = directory.Parent;
            }
        }

        return null;
    }

    private static Dictionary<string, string> ReadResourceValues(string file)
    {
        var content = File.ReadAllText(file);
        var regex = new Regex(@"<data\s+name=""(?<name>[^""]+)""[^>]*>\s*<value>(?<value>.*?)</value>", RegexOptions.Singleline);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in regex.Matches(content))
        {
            result[match.Groups["name"].Value] = match.Groups["value"].Value;
        }
        return result;
    }
}

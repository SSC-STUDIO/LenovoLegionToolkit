using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FluentAssertions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class StartupWindowsGuardTests
{
    [Fact]
    public void LanguageSelectorWindow_ShouldUseCompactContentLayout()
    {
        var xaml = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Windows", "Utils", "LanguageSelectorWindow.xaml");
        var contentGridStart = xaml.IndexOf("<Grid Grid.Row=\"1\" Margin=\"20,18,20,20\">", StringComparison.Ordinal);
        var contentGridEnd = xaml.IndexOf("<Grid.ColumnDefinitions>", contentGridStart, StringComparison.Ordinal);
        contentGridStart.Should().BeGreaterThanOrEqualTo(0);
        contentGridEnd.Should().BeGreaterThan(contentGridStart);
        var contentRows = xaml[contentGridStart..contentGridEnd];

        xaml.Should().Contain("Height=\"300\"");
        contentRows.Should().NotContain("Height=\"*\" />");
        xaml.Should().Contain("HorizontalAlignment=\"Right\"");
        xaml.Should().Contain("Content=\"{x:Static resources:Resource.Continue}\"");
    }

    [Fact]
    public void DeviceSetupWindow_ShouldLocalizeStartupText()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Windows", "Utils", "DeviceSetupWindow.xaml.cs");
        var xaml = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Windows", "Utils", "DeviceSetupWindow.xaml");
        var zhResources = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Resources", "Resource.zh.resx");
        var zhHansResources = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Resources", "Resource.zh-hans.resx");

        source.Should().Contain("DeviceSetupWindow_BasicModeSummary");
        source.Should().Contain("DeviceSetupWindow_MatchingPackSummary");
        source.Should().Contain("DeviceSetupWindow_SelectPackLabel");
        source.Should().Contain("DeviceSetupWindow_RecommendedPackFormat");
        source.Should().Contain("DeviceSetupWindow_Preparing");
        source.Should().Contain("DeviceSetupWindow_SkipButton");
        source.Should().Contain("DeviceSetupWindow_ConfirmButton");
        source.Should().Contain("DeviceSetupWindow_Title");
        source.Should().Contain("BuildPackOptions");
        xaml.Should().Contain("DeviceSetupPackComboBox");
        // Must not misuse hybrid-mode restart labels on this window.
        xaml.Should().NotContain("Resource.RestartLater");
        xaml.Should().NotContain("Resource.RestartNow");

        zhResources.Should().Contain("<data name=\"DeviceSetupWindow_MatchingPackSummary\"");
        zhResources.Should().Contain("<value>设备包：{0}</value>");
        zhResources.Should().Contain("DeviceSetupWindow_SelectPackLabel");
        zhHansResources.Should().Contain("<data name=\"DeviceSetupWindow_MatchingPackSummary\"");
        zhHansResources.Should().Contain("<value>设备包：{0}</value>");
        zhHansResources.Should().Contain("DeviceSetupWindow_SkipButton");
        zhHansResources.Should().Contain("暂时跳过");
        zhHansResources.Should().Contain("DeviceSetupWindow_SelectPackLabel");
        zhHansResources.Should().Contain("设备配置文件");
        zhHansResources.Should().NotContain("detected a matching device pack");
    }

    [Fact]
    public void DeviceSetupWindow_zhHansResources_ShouldNotFallBackToEnglish()
    {
        var culture = new CultureInfo("zh-Hans");

        var summary = LocalizationHelper.GetStringOrEnglish(
            Resource.ResourceManager,
            "DeviceSetupWindow_MatchingPackSummary",
            "fallback",
            culture);
        var packFormat = LocalizationHelper.GetStringOrEnglish(
            Resource.ResourceManager,
            "DeviceSetupWindow_DevicePackFormat",
            "fallback",
            culture);
        var selectLabel = LocalizationHelper.GetStringOrEnglish(
            Resource.ResourceManager,
            "DeviceSetupWindow_SelectPackLabel",
            "fallback",
            culture);

        summary.Should().NotBe("fallback");
        summary.Should().NotContain("detected a matching device pack");
        summary.Should().NotContain("found a matching device pack");
        packFormat.Should().Be("设备包：{0}");
        selectLabel.Should().Contain("设备配置文件");

        var skip = LocalizationHelper.GetStringOrEnglish(
            Resource.ResourceManager,
            "DeviceSetupWindow_SkipButton",
            "fallback",
            culture);
        skip.Should().Be("暂时跳过");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    public void ResolveSupportedLanguage_EnglishVariants_MapToEn(string name)
    {
        var resolved = LocalizationHelper.ResolveSupportedLanguage(new CultureInfo(name));
        resolved.Should().NotBeNull();
        resolved!.Name.Should().Be("en");
    }

    [Fact]
    public void GetStringOrEnglish_EnglishCulture_NavigationItemsAreEnglish()
    {
        var culture = new CultureInfo("en");
        var dashboard = LocalizationHelper.GetStringOrEnglish(
            Resource.ResourceManager,
            "MainWindow_NavigationItem_Dashboard",
            "Dashboard",
            culture);
        var settings = LocalizationHelper.GetStringOrEnglish(
            Resource.ResourceManager,
            "MainWindow_NavigationItem_Settings",
            "Settings",
            culture);

        dashboard.Should().Be("Dashboard");
        settings.Should().Be("Settings");
        dashboard.Should().NotMatchRegex(@"[\u4e00-\u9fff]");
        settings.Should().NotMatchRegex(@"[\u4e00-\u9fff]");
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var expectedRelativePath = Path.Combine(pathParts);
        foreach (var candidateRoot in GetRepositoryRootCandidates())
        {
            var path = Path.Combine(candidateRoot, expectedRelativePath);
            if (File.Exists(path))
                return File.ReadAllText(path);
        }

        throw new DirectoryNotFoundException($"Could not locate repository file '{expectedRelativePath}'.");
    }

    private static IEnumerable<string> GetRepositoryRootCandidates()
    {
        var roots = new[]
        {
            Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT"),
            Environment.CurrentDirectory,
            AppContext.BaseDirectory,
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."))
        };

        foreach (var root in roots.Where(static root => !string.IsNullOrWhiteSpace(root)))
        {
            var directory = new DirectoryInfo(root!);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
                    yield return directory.FullName;

                directory = directory.Parent;
            }
        }
    }
}

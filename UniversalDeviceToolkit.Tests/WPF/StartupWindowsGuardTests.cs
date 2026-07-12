using System;
using System.Globalization;
using System.IO;
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
        var zhResources = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Resources", "Resource.zh.resx");
        var zhHansResources = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Resources", "Resource.zh-hans.resx");

        source.Should().Contain("DeviceSetupWindow_BasicModeSummary");
        source.Should().Contain("DeviceSetupWindow_MatchingPackSummary");
        source.Should().Contain("DeviceSetupWindow_DevicePackFormat");
        source.Should().Contain("DeviceSetupWindow_Preparing");
        source.Should().NotContain("_summaryText.Text = \"Universal Device Toolkit detected");
        source.Should().NotContain("_packText.Text = \"Device pack:");
        source.Should().NotContain("_statusText.Text = \"Preparing device setup");

        zhResources.Should().Contain("<data name=\"DeviceSetupWindow_MatchingPackSummary\"");
        zhResources.Should().Contain("<value>设备包：{0}</value>");
        zhHansResources.Should().Contain("<data name=\"DeviceSetupWindow_MatchingPackSummary\"");
        zhHansResources.Should().Contain("<value>设备包：{0}</value>");
        zhHansResources.Should().Contain("检测到匹配的设备包");
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

        summary.Should().Contain("检测到匹配的设备包");
        summary.Should().NotContain("detected a matching device pack");
        packFormat.Should().Be("设备包：{0}");
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

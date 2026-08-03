using System;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Guard)]
[Trait("Category", TestCategories.Unit)]
public sealed class ExceptionResourceGuardTests
{
    private static readonly Regex LiteralExceptionMessage = new(
        "throw\\s+new\\s+\\w*Exception\\s*\\(\\s*[\\\"']",
        RegexOptions.Compiled);

    [Fact]
    public void KnownUserFacingExceptionSites_ShouldUseResourceBackedMessages()
    {
        AssertResourceBacked(
            ["UniversalDeviceToolkit.Lib", "Settings", "SettingsBackupService.cs"],
            "Resource.SettingsBackup_");
        AssertResourceBacked(
            ["UniversalDeviceToolkit.WPF", "Controls", "AbstractComboBoxFeatureCardControl.cs"],
            "Resource.ComboBox_UnsupportedType");
        AssertResourceBacked(
            ["UniversalDeviceToolkit.WPF", "Controls", "Automation", "AbstractComboBoxAutomationStepControl.cs"],
            "Resource.AutomationStep_CreationFailed");
        AssertResourceBacked(
            ["UniversalDeviceToolkit.WPF", "CLI", "Features", "FeatureRegistration.cs"],
            "Resource.FeatureRegistration_");
    }

    [Fact]
    public void KnownUserFacingExceptionSites_ShouldNotReintroduceLiteralMessages()
    {
        foreach (var path in KnownExceptionSites)
        {
            var source = RepositoryPaths.ReadFile(path);
            LiteralExceptionMessage.IsMatch(source).Should().BeFalse(
                $"{string.Join('/', path)} should keep user-facing exception text in Resource");
        }
    }

    private static readonly string[][] KnownExceptionSites =
    [
        ["UniversalDeviceToolkit.Lib", "Settings", "SettingsBackupService.cs"],
        ["UniversalDeviceToolkit.WPF", "Controls", "AbstractComboBoxFeatureCardControl.cs"],
        ["UniversalDeviceToolkit.WPF", "Controls", "Automation", "AbstractComboBoxAutomationStepControl.cs"],
        ["UniversalDeviceToolkit.WPF", "CLI", "Features", "FeatureRegistration.cs"]
    ];

    private static void AssertResourceBacked(string[] path, string resourceMarker)
    {
        var source = RepositoryPaths.ReadFile(path);
        source.Should().Contain(resourceMarker, string.Join('/', path));
    }
}

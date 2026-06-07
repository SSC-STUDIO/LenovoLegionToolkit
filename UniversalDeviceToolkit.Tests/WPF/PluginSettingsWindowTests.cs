using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using UniversalDeviceToolkit.WPF.Windows.Settings;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public sealed class PluginSettingsWindowTests
{
    [Fact]
    public void CanShowPluginSettings_WhenManifestOnlyPluginHasSettingsPage_ShouldReturnTrue()
    {
        var manifest = new PluginManifest
        {
            Id = "user-feedback",
            Name = "User Feedback",
            Contributes = new PluginManifestContributions
            {
                SettingsPage = new PluginManifestPageContribution
                {
                    Class = "UserFeedback.Settings",
                    Title = "Feedback"
                }
            }
        };

        PluginSettingsWindow.CanShowPluginSettings(plugin: null, manifest)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void CanShowPluginSettings_WhenManifestOnlyPluginHasNoSettingsPage_ShouldReturnFalse()
    {
        var manifest = new PluginManifest
        {
            Id = "metadata-only",
            Name = "Metadata Only"
        };

        PluginSettingsWindow.CanShowPluginSettings(plugin: null, manifest)
            .Should()
            .BeFalse();
    }
}

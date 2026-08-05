using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Unit)]
public sealed class AvaloniaBackdropContractTests
{
    [Fact]
    public void MainWindow_ShouldApplyBackdropWithPlatformFallbacks()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "MainWindow.axaml.cs"));

        source.Should().Contain("public void ApplyWindowBackdrop()");
        source.Should().Contain("WindowTransparencyLevel.Mica");
        source.Should().Contain("WindowTransparencyLevel.AcrylicBlur");
        source.Should().Contain("WindowTransparencyLevel.Blur");
        source.Should().Contain("WindowTransparencyLevel.None");
        source.Should().Contain("TransparencyBackgroundFallback");
        source.Should().Contain("ApplyWindowBackdrop();");
    }

    [Fact]
    public void DisplayBackdropSelection_ShouldRefreshTheCurrentWindow()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "SettingsCapabilityView.axaml.cs"));

        source.Should().Contain("_pageKey == \"Display\"");
        source.Should().Contain("option.Key == \"WindowBackdrop\"");
        source.Should().Contain("mainWindow.ApplyWindowBackdrop();");
    }
}

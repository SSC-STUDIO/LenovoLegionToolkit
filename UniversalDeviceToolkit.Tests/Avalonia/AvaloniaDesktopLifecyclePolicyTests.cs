using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Unit)]
public sealed class AvaloniaDesktopLifecyclePolicyTests
{
    [Theory]
    [InlineData(false, false, false, (int)MainWindowCloseAction.ExitApplication)]
    [InlineData(false, false, true, (int)MainWindowCloseAction.HideToTray)]
    [InlineData(false, true, false, (int)MainWindowCloseAction.Minimize)]
    [InlineData(false, true, true, (int)MainWindowCloseAction.Minimize)]
    [InlineData(true, true, true, (int)MainWindowCloseAction.AllowClose)]
    public void CloseAction_UsesTheSamePrecedenceForTrayAndControlledShutdown(
        bool isExiting,
        bool minimizeOnClose,
        bool minimizeToTray,
        int expected)
    {
        AvaloniaDesktopLifecyclePolicy.ResolveCloseAction(
            isExiting,
            minimizeOnClose,
            minimizeToTray).Should().Be((MainWindowCloseAction)expected);
    }

    [Fact]
    public void AppCloseHandler_UsesLifecyclePolicyBeforeDesktopCanCloseTheLastWindow()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "App.axaml.cs"));

        source.Should().Contain("AvaloniaDesktopLifecyclePolicy.ResolveCloseAction(");
        source.Should().Contain("case MainWindowCloseAction.ExitApplication:");
        source.Should().Contain("e.Cancel = true;");
        source.Should().Contain("ExitApplication();");
        source.Should().Contain("case MainWindowCloseAction.AllowClose:");
    }
}

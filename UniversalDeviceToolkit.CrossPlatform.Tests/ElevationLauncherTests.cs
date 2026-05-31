using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.CrossPlatform.Tests;

public sealed class ElevationLauncherTests
{
    [Fact]
    public void Launch_WhenNoCommandIsProvided_ShouldReturnUsage()
    {
        var result = new ElevationLauncher().Launch([]);

        result.Succeeded.Should().BeFalse();
        result.Detail.Should().Contain("Usage: udt elevate");
    }

    [Fact]
    public void Launch_WhenNotWindows_ShouldExplainPlatformElevation()
    {
        if (OperatingSystem.IsWindows())
            return;

        var result = new ElevationLauncher().Launch(["set", "cpu-governor", "performance"]);

        result.Succeeded.Should().BeFalse();
        result.Detail.Should().Contain("UAC");
        result.Detail.Should().Contain("sudo");
        result.Detail.Should().Contain("polkit");
    }
}

using System.Reflection;
using FluentAssertions;
using LenovoLegionToolkit.WPF.Controls.Packages;
using Xunit;

namespace LenovoLegionToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class PackageControlTests
{
    [Fact]
    public void GetStatusForInstallerExitCode_ShouldTreatZeroAsCompleted()
    {
        var method = typeof(PackageControl).GetMethod("GetStatusForInstallerExitCode", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        var status = method!.Invoke(null, [0]);

        status.Should().Be(PackageControl.PackageStatus.Completed);
    }

    [Fact]
    public void GetInstallerExitFailureMessage_ShouldIncludeExitCode()
    {
        var method = typeof(PackageControl).GetMethod("GetInstallerExitFailureMessage", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        var message = method!.Invoke(null, [1603]) as string;

        message.Should().Contain("1603");
    }

    [Fact]
    public void GetInstallerExitFailureMessage_ShouldReturnNullForSuccessExitCode()
    {
        var method = typeof(PackageControl).GetMethod("GetInstallerExitFailureMessage", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        var message = method!.Invoke(null, [0]);

        message.Should().BeNull();
    }
}

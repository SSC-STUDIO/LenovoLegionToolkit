using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
[Trait("Category", TestCategories.Guard)]
public sealed class AppNotificationLayoutGuardTests
{
    [Fact]
    public void NotificationCards_ShouldNotRenderASeverityAccentBar()
    {
        var root = RepositoryPaths.FindRoot();
        var xaml = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.WPF",
            "Controls",
            "Shell",
            "AppNotificationHost.xaml"));

        xaml.Should().NotContain("Width=\"4\"");
        xaml.Should().NotContain("Margin=\"8,12\"");
        xaml.Should().Contain("<Grid Margin=\"12,10,12,10\">");
    }
}

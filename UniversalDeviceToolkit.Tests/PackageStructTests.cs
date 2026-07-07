using System;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class PackageStructTests
{
    #region Package Index Tests

    [Fact]
    public void Package_Index_ShouldConcatenateTitleVersionCategory()
    {
        var pkg = new Package
        {
            Id = "pkg-1",
            Title = "Test Package",
            Description = "A test",
            Version = "1.0.0",
            Category = "BIOS",
            FileName = "test.exe",
            FileSize = "10 MB",
            FileCrc = "abc123",
            ReleaseDate = new DateTime(2025, 1, 1),
            Readme = "readme",
            FileLocation = @"C:\test",
            IsUpdate = true,
            Reboot = RebootType.NotRequired
        };

        var index = pkg.Index;
        index.Should().Contain("Test Package");
        index.Should().Contain("1.0.0");
        index.Should().Contain("BIOS");
        index.Should().Contain("test.exe");
    }

    [Fact]
    public void Package_Index_ShouldBeCached()
    {
        var pkg = new Package { Title = "A", Description = "B", Version = "1", Category = "C", FileName = "D" };
        var first = pkg.Index;
        var second = pkg.Index;
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void Package_DefaultValues_ShouldWork()
    {
        var pkg = new Package();
        pkg.Id.Should().BeNull();
        pkg.Title.Should().BeNull();
        pkg.IsUpdate.Should().BeFalse();
        pkg.Reboot.Should().Be(RebootType.NotRequired);
        pkg.ReleaseDate.Should().Be(default);
    }

    #endregion

    #region WindowsPowerPlan Tests

    [Fact]
    public void WindowsPowerPlan_Constructor_ShouldSetAllFields()
    {
        var guid = Guid.NewGuid();
        var plan = new WindowsPowerPlan(guid, "High Performance", true);
        plan.Guid.Should().Be(guid);
        plan.Name.Should().Be("High Performance");
        plan.IsActive.Should().BeTrue();
    }

    [Fact]
    public void WindowsPowerPlan_IsActiveFalse_ShouldWork()
    {
        var plan = new WindowsPowerPlan(Guid.NewGuid(), "Balanced", false);
        plan.IsActive.Should().BeFalse();
    }

    [Fact]
    public void WindowsPowerPlan_ToString_ShouldContainAllFields()
    {
        var guid = Guid.NewGuid();
        var plan = new WindowsPowerPlan(guid, "Power Saver", false);
        var text = plan.ToString();
        text.Should().Contain(guid.ToString());
        text.Should().Contain("Power Saver");
        text.Should().Contain("False");
    }

    #endregion
}

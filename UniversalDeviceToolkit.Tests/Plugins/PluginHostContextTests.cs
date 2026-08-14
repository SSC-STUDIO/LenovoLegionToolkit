using System;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Unit)]
public class PluginHostContextTests : IDisposable
{
    private readonly IPluginHostContext _originalContext = PluginHostContext.Current;

    public void Dispose() => PluginHostContext.SetCurrent(_originalContext);

    #region PluginHostMode Enum Tests

    [Fact]
    public void PluginHostMode_Has2Values()
    {
        Enum.GetValues<PluginHostMode>().Should().HaveCount(2);
    }

    [Theory]
    [InlineData(PluginHostMode.Preview)]
    [InlineData(PluginHostMode.RealRuntime)]
    public void PluginHostMode_AllValues_ShouldBeDefined(PluginHostMode mode)
    {
        Enum.IsDefined(mode).Should().BeTrue();
    }

    [Fact]
    public void PluginHostMode_Preview_ShouldBeZero()
    {
        ((int)PluginHostMode.Preview).Should().Be(0);
    }

    [Fact]
    public void PluginHostMode_RealRuntime_ShouldBeOne()
    {
        ((int)PluginHostMode.RealRuntime).Should().Be(1);
    }

    #endregion

    #region PluginHostContext Default Tests

    [Fact]
    public void PluginHostContext_Current_ShouldNotBeNull()
    {
        PluginHostContext.Current.Should().NotBeNull();
    }

    [Fact]
    public void PluginHostContext_Current_DefaultMode_ShouldBePreview()
    {
        PluginHostContext.Current.Mode.Should().Be(PluginHostMode.Preview);
    }

    [Fact]
    public void PluginHostContext_Current_DefaultAllowSystemActions_ShouldBeFalse()
    {
        PluginHostContext.Current.AllowSystemActions.Should().BeFalse();
    }

    [Fact]
    public void PluginHostContext_Current_DefaultOwnerWindow_ShouldBeNull()
    {
        PluginHostContext.Current.OwnerWindow.Should().BeNull();
    }

    [Fact]
    public void PluginHostContext_Current_OpenPluginSettings_ShouldReturnFalse()
    {
        PluginHostContext.Current.OpenPluginSettings("any").Should().BeFalse();
    }

    [Fact]
    public void PluginHostContext_Current_ShowDialog_ShouldReturnNull()
    {
        PluginHostContext.Current.ShowDialog(new object()).Should().BeNull();
    }

    [Fact]
    public void Current_ShouldExposeNoOpPreviewContextByDefault()
    {
        // Arrange
        PluginHostContext.Reset();

        // Act
        var context = PluginHostContext.Current;

        // Assert
        context.Mode.Should().Be(PluginHostMode.Preview);
        context.AllowSystemActions.Should().BeFalse();
        context.OwnerWindow.Should().BeNull();
        context.OpenPluginSettings("test-plugin").Should().BeFalse();
        context.ShowDialog(new object()).Should().BeNull();
    }

    #endregion

    #region PluginHostContext SetCurrent/Reset Tests

    [Fact]
    public void PluginHostContext_SetCurrent_CustomContext_ShouldSetIt()
    {
        var custom = new TestPluginHostContext { Mode = PluginHostMode.RealRuntime };
        PluginHostContext.SetCurrent(custom);
        try
        {
            PluginHostContext.Current.Should().BeSameAs(custom);
            PluginHostContext.Current.Mode.Should().Be(PluginHostMode.RealRuntime);
        }
        finally
        {
            PluginHostContext.Reset();
        }
    }

    [Fact]
    public void PluginHostContext_SetCurrent_Null_ShouldRestoreDefault()
    {
        PluginHostContext.SetCurrent(new TestPluginHostContext());
        PluginHostContext.SetCurrent(null);
        PluginHostContext.Current.Mode.Should().Be(PluginHostMode.Preview);
    }

    [Fact]
    public void PluginHostContext_Reset_ShouldRestoreDefault()
    {
        PluginHostContext.SetCurrent(new TestPluginHostContext { Mode = PluginHostMode.RealRuntime });
        PluginHostContext.Reset();
        PluginHostContext.Current.Mode.Should().Be(PluginHostMode.Preview);
        PluginHostContext.Current.AllowSystemActions.Should().BeFalse();
    }

    [Fact]
    public void SetCurrent_ShouldExposeSuppliedContext()
    {
        // Arrange
        var expected = new TestPluginHostContext(PluginHostMode.RealRuntime, ownerWindow: "owner");

        // Act
        PluginHostContext.SetCurrent(expected);

        // Assert
        PluginHostContext.Current.Should().BeSameAs(expected);
        PluginHostContext.Current.AllowSystemActions.Should().BeTrue();
        PluginHostContext.Current.OwnerWindow.Should().Be("owner");
        PluginHostContext.Current.OpenPluginSettings("test-plugin").Should().BeTrue();
        PluginHostContext.Current.ShowDialog("dialog").Should().BeTrue();
    }

    [Fact]
    public void Reset_ShouldRestoreNoOpContextAfterCustomContext()
    {
        // Arrange
        PluginHostContext.SetCurrent(new TestPluginHostContext(PluginHostMode.RealRuntime, ownerWindow: 123));

        // Act
        PluginHostContext.Reset();

        // Assert
        PluginHostContext.Current.Mode.Should().Be(PluginHostMode.Preview);
        PluginHostContext.Current.AllowSystemActions.Should().BeFalse();
        PluginHostContext.Current.OwnerWindow.Should().BeNull();
        PluginHostContext.Current.OpenPluginSettings("test-plugin").Should().BeFalse();
        PluginHostContext.Current.ShowDialog("dialog").Should().BeNull();
    }

    #endregion

    private sealed class TestPluginHostContext : IPluginHostContext
    {
        public TestPluginHostContext()
        {
        }

        public TestPluginHostContext(PluginHostMode mode, object? ownerWindow)
        {
            Mode = mode;
            OwnerWindow = ownerWindow;
        }

        public PluginHostMode Mode { get; set; } = PluginHostMode.Preview;

        public bool AllowSystemActions => Mode == PluginHostMode.RealRuntime;

        public object? OwnerWindow { get; set; }

        public bool OpenPluginSettings(string pluginId) => pluginId == "test-plugin";

        public bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null) => dialogOrContent is "dialog";
    }
}

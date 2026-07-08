using System;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class PluginHostContextTests
{
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

    #endregion

    #region PluginHostContext SetCurrent/Reset Tests

    [Fact]
    public void PluginHostContext_SetCurrent_CustomContext_ShouldSetIt()
    {
        var custom = new TestPluginHostContext { TestMode = PluginHostMode.RealRuntime };
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
        PluginHostContext.SetCurrent(new TestPluginHostContext { TestMode = PluginHostMode.RealRuntime });
        PluginHostContext.Reset();
        PluginHostContext.Current.Mode.Should().Be(PluginHostMode.Preview);
        PluginHostContext.Current.AllowSystemActions.Should().BeFalse();
    }

    #endregion

    private class TestPluginHostContext : IPluginHostContext
    {
        public PluginHostMode TestMode { get; set; } = PluginHostMode.Preview;
        public PluginHostMode Mode => TestMode;
        public bool AllowSystemActions => false;
        public object? OwnerWindow => null;
        public bool OpenPluginSettings(string pluginId) => false;
        public bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null) => null;
    }
}
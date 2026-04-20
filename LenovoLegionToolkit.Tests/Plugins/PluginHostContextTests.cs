using System;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using Xunit;

namespace LenovoLegionToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Unit)]
public class PluginHostContextTests : IDisposable
{
    private readonly IPluginHostContext _originalContext = PluginHostContext.Current;

    public void Dispose() => PluginHostContext.SetCurrent(_originalContext);

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

    private sealed class TestPluginHostContext : IPluginHostContext
    {
        public TestPluginHostContext(PluginHostMode mode, object? ownerWindow)
        {
            Mode = mode;
            OwnerWindow = ownerWindow;
        }

        public PluginHostMode Mode { get; }

        public bool AllowSystemActions => Mode == PluginHostMode.RealRuntime;

        public object? OwnerWindow { get; }

        public bool OpenPluginSettings(string pluginId) => pluginId == "test-plugin";

        public bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null) => dialogOrContent is "dialog";
    }
}

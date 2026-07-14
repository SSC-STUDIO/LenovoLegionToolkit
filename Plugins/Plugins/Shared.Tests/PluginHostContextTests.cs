using System;
using UniversalDeviceToolkit.Plugins.SDK;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.Shared.Tests;

public sealed class PluginHostContextTests : IDisposable
{
    private readonly IPluginHostContext _originalSdkContext = PluginHostContext.Current;

    public void Dispose()
    {
        PluginHostContext.Current = _originalSdkContext;
    }

    [Fact]
    public void Push_RestoresPreviousContextAfterDispose()
    {
        var replacement = new FakeSdkPluginHostContext(PluginHostMode.Preview, allowSystemActions: false);

        using (PluginHostContext.Push(replacement))
        {
            Assert.Same(replacement, PluginHostContext.Current);
            Assert.Equal(PluginHostMode.Preview, PluginHostContext.Current.Mode);
            Assert.False(PluginHostContext.Current.AllowSystemActions);
        }

        Assert.Same(_originalSdkContext, PluginHostContext.Current);
    }

    [Fact]
    public void Reset_RestoresDefaultPreviewContextWithoutHostRuntime()
    {
        PluginHostContext.Current = new FakeSdkPluginHostContext(PluginHostMode.RealRuntime, allowSystemActions: true);

        PluginHostContext.Reset();

        Assert.Equal(PluginHostMode.Preview, PluginHostContext.Current.Mode);
        Assert.False(PluginHostContext.Current.AllowSystemActions);
        Assert.Null(PluginHostContext.Current.OwnerWindow);
        Assert.False(PluginHostContext.Current.OpenPluginSettings("test-plugin"));
        Assert.False(PluginHostContext.Current.ShowDialog(new object()));
    }

    [Fact]
    public void CreateHostWindow_WithMissingType_ReturnsNull()
    {
        var window = PluginHostContext.CreateHostWindow("UniversalDeviceToolkit.WPF.Windows.Settings.DoesNotExistWindow");

        Assert.Null(window);
    }

    private sealed class FakeSdkPluginHostContext : IPluginHostContext
    {
        public FakeSdkPluginHostContext(PluginHostMode mode, bool allowSystemActions)
        {
            Mode = mode;
            AllowSystemActions = allowSystemActions;
        }

        public PluginHostMode Mode { get; }
        public bool AllowSystemActions { get; }
        public object? OwnerWindow => null;
        public bool OpenPluginSettings(string pluginId) => !string.IsNullOrWhiteSpace(pluginId);
        public bool ShowDialog(object dialogOrContent, string? title = null, string? icon = null) => dialogOrContent is not null;
    }
}

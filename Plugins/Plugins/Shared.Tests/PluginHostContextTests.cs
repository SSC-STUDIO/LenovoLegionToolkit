using System;
using UniversalDeviceToolkit.Plugins.SDK;
using UniversalDeviceToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.Shared.Tests;

public sealed class PluginHostContextTests : IDisposable
{
    private readonly IPluginHostContext _originalSdkContext = PluginHostContextRuntime.Current;

    public void Dispose()
    {
        PluginHostContextRuntime.Current = _originalSdkContext;
    }

    [Fact]
    public void Push_RestoresPreviousContextAfterDispose()
    {
        var replacement = new FakeSdkPluginHostContext(PluginHostMode.Preview, allowSystemActions: false);

        using (PluginHostContextRuntime.Push(replacement))
        {
            Assert.Same(replacement, PluginHostContextRuntime.Current);
            Assert.Equal(PluginHostMode.Preview, PluginHostContextRuntime.Current.Mode);
            Assert.False(PluginHostContextRuntime.Current.AllowSystemActions);
        }

        Assert.Same(_originalSdkContext, PluginHostContextRuntime.Current);
    }

    [Fact]
    public void Reset_RestoresDefaultPreviewContextWithoutHostRuntime()
    {
        PluginHostContextRuntime.Current = new FakeSdkPluginHostContext(PluginHostMode.RealRuntime, allowSystemActions: true);

        PluginHostContextRuntime.Reset();

        Assert.Equal(PluginHostMode.Preview, PluginHostContextRuntime.Current.Mode);
        Assert.False(PluginHostContextRuntime.Current.AllowSystemActions);
        Assert.Null(PluginHostContextRuntime.Current.OwnerWindow);
        Assert.False(PluginHostContextRuntime.Current.OpenPluginSettings("test-plugin"));
        Assert.False(PluginHostContextRuntime.Current.ShowDialog(new object()));
    }

    [Fact]
    public void CreateHostWindow_WithMissingType_ReturnsNull()
    {
        var window = PluginHostContextRuntime.CreateHostWindow("UniversalDeviceToolkit.WPF.Windows.Settings.DoesNotExistWindow");

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
        public bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null) => dialogOrContent is not null;
    }
}

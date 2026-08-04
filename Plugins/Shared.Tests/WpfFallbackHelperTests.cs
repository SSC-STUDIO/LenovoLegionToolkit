using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.Shared.Tests;

/// <summary>
/// Tests for WpfFallbackHelper that don't require WPF UI instantiation.
/// Full WPF UI tests would require STA thread context.
/// </summary>
public class WpfFallbackHelperTests
{
    #region Static Method Existence Tests

    [Fact]
    public void BuildFallbackPanel_MethodExists()
    {
        // Verify the method exists and can be called (in non-WPF context it may throw)
        // The actual WPF UI behavior is verified through integration tests
        var type = typeof(WpfFallbackHelper);
        // Method signature is BuildFallbackPanel(string message, string? details = null)
        var method = type.GetMethod("BuildFallbackPanel");

        Assert.NotNull(method);
    }

    [Fact]
    public void CreateErrorFallback_MethodExists()
    {
        var type = typeof(WpfFallbackHelper);
        var method = type.GetMethod("CreateErrorFallback", new[] { typeof(string), typeof(string) });

        Assert.NotNull(method);
    }

    [Fact]
    public void TryInitializeComponent_MethodExists()
    {
        var type = typeof(WpfFallbackHelper);
        var method = type.GetMethod("TryInitializeComponent");

        Assert.NotNull(method);
    }

    [Fact]
    public void TryInitializeComponent_IsGeneric()
    {
        var type = typeof(WpfFallbackHelper);
        var method = type.GetMethod("TryInitializeComponent");

        Assert.NotNull(method);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void TryInitializeComponent_HasCorrectSignature()
    {
        var type = typeof(WpfFallbackHelper);
        var method = type.GetMethod("TryInitializeComponent");

        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method.ReturnType);
        Assert.Equal(2, method.GetParameters().Length);
    }

    [Fact]
    public void BuildFallbackPanel_HasCorrectSignature()
    {
        var type = typeof(WpfFallbackHelper);
        var methodWithDetails = type.GetMethod("BuildFallbackPanel",
            new[] { typeof(string), typeof(string).MakeByRefType() });

        // Actually the method is BuildFallbackPanel(string, string?)
        var method = type.GetMethod("BuildFallbackPanel",
            new[] { typeof(string), typeof(string) });

        Assert.NotNull(method);
        Assert.Equal(typeof(StackPanel), method.ReturnType);
    }

    [Fact]
    public void CreateErrorFallback_ReturnsStackPanel()
    {
        var type = typeof(WpfFallbackHelper);
        var method = type.GetMethod("CreateErrorFallback",
            new[] { typeof(string), typeof(string) });

        Assert.NotNull(method);
        Assert.Equal(typeof(StackPanel), method.ReturnType);
    }

    #endregion

    #region Type Tests

    [Fact]
    public void WpfFallbackHelper_IsStaticClass()
    {
        var type = typeof(WpfFallbackHelper);

        Assert.True(type.IsClass);
        Assert.True(type.IsAbstract);
        Assert.True(type.IsSealed); // Static classes are abstract and sealed in IL
    }

    [Fact]
    public void StackPanel_TypeExists()
    {
        var type = Type.GetType("System.Windows.Controls.StackPanel, PresentationFramework");

        Assert.NotNull(type);
    }

    #endregion

    #region Constants Usage Tests

    [Fact]
    public void Constants_FallbackPanelWidth_IsDefined()
    {
        Assert.True(Constants.FallbackPanelWidth > 0);
    }

    [Fact]
    public void Constants_FallbackPanelHeight_IsDefined()
    {
        Assert.True(Constants.FallbackPanelHeight > 0);
    }

    [Fact]
    public void Constants_DefaultSpacing_IsDefined()
    {
        Assert.True(Constants.DefaultSpacing >= 0);
    }

    #endregion
}
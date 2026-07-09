using System;
using Xunit;

namespace LenovoLegionToolkit.Plugins.Shared.Tests;

[Collection("PluginLogSerial")]
public class PluginLogTests : IDisposable
{
    public PluginLogTests() => PluginLog.Reset();
    public void Dispose() => PluginLog.Reset();

    [Fact]
    public void IsTraceEnabled_WhenNotConfigured_ReturnsFalse()
    {
        Assert.False(PluginLog.IsTraceEnabled);
    }

    [Fact]
    public void Trace_WhenNotConfigured_DoesNotThrow()
    {
        var ex = Record.Exception(() => PluginLog.Trace("hello"));
        Assert.Null(ex);

        var ex2 = Record.Exception(() => PluginLog.Trace("hello", new InvalidOperationException()));
        Assert.Null(ex2);
    }

    [Fact]
    public void Error_WhenNotConfigured_DoesNotThrow()
    {
        var ex = Record.Exception(() => PluginLog.Error("boom"));
        Assert.Null(ex);

        var ex2 = Record.Exception(() => PluginLog.Error("boom", new InvalidOperationException()));
        Assert.Null(ex2);
    }

    [Fact]
    public void Configure_WithNullIsTraceEnabled_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            PluginLog.Configure(null!, static (_, _) => { }));
        Assert.Equal("isTraceEnabled", ex.ParamName);
    }

    [Fact]
    public void Configure_WithNullTrace_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            PluginLog.Configure(static () => true, null!));
        Assert.Equal("trace", ex.ParamName);
    }

    [Fact]
    public void Configure_WithDedicatedErrorSink_ErrorUsesErrorSinkNotTraceSink()
    {
        var traceCalls = 0;
        var errorCalls = 0;
        PluginLog.Configure(
            isTraceEnabled: static () => true,
            trace: (_, _) => traceCalls++,
            error: (_, _) => errorCalls++);

        PluginLog.Trace("trace-msg");
        PluginLog.Error("error-msg");

        Assert.Equal(1, traceCalls);
        Assert.Equal(1, errorCalls);
    }

    [Fact]
    public void Error_WithDedicatedErrorSink_AlwaysInvokesErrorSinkRegardlessOfTraceGate()
    {
        var errorCalls = 0;
        PluginLog.Configure(
            isTraceEnabled: static () => false,
            trace: (_, _) => { },
            error: (_, _) => errorCalls++);

        PluginLog.Error("msg");
        PluginLog.Error("msg", new InvalidOperationException());

        Assert.Equal(2, errorCalls);
    }

    [Fact]
    public void Configure_WithoutErrorSink_ErrorFallsBackToTraceSink()
    {
        var traceCalls = 0;
        PluginLog.Configure(
            isTraceEnabled: static () => false,
            trace: (_, _) => traceCalls++);

        PluginLog.Error("msg");

        Assert.Equal(1, traceCalls);
    }

    [Fact]
    public void Trace_WhenTraceGateIsFalse_DoesNotInvokeSink()
    {
        var sinkCalls = 0;
        PluginLog.Configure(
            isTraceEnabled: static () => false,
            trace: (_, _) => sinkCalls++);

        PluginLog.Trace("msg");
        PluginLog.Trace("msg", new InvalidOperationException());

        Assert.Equal(0, sinkCalls);
        Assert.False(PluginLog.IsTraceEnabled);
    }

    [Fact]
    public void Error_AlwaysInvokesSink_EvenWhenTraceGateIsFalse()
    {
        var sinkCalls = 0;
        PluginLog.Configure(
            isTraceEnabled: static () => false,
            trace: (_, _) => sinkCalls++);

        PluginLog.Error("msg");
        PluginLog.Error("msg", new InvalidOperationException());

        Assert.Equal(2, sinkCalls);  // Error() ALWAYS logs, regardless of trace gate
    }

    [Fact]
    public void Trace_WhenEnabled_ForwardsMessageAndException()
    {
        string? capturedMessage = null;
        Exception? capturedException = null;
        var expected = new InvalidOperationException("boom");

        PluginLog.Configure(
            isTraceEnabled: static () => true,
            trace: (m, e) => { capturedMessage = m; capturedException = e; });

        PluginLog.Trace("hello", expected);

        Assert.Equal("hello", capturedMessage);
        Assert.Same(expected, capturedException);
    }

    [Fact]
    public void Trace_WithoutException_PassesNullToSink()
    {
        string? capturedMessage = null;
        Exception? capturedException = new InvalidOperationException("sentinel");

        PluginLog.Configure(
            isTraceEnabled: static () => true,
            trace: (m, e) => { capturedMessage = m; capturedException = e; });

        PluginLog.Trace("just-a-message");

        Assert.Equal("just-a-message", capturedMessage);
        Assert.Null(capturedException);
    }

    [Fact]
    public void Error_WhenEnabled_ForwardsToSameSink()
    {
        string? capturedMessage = null;
        Exception? capturedException = null;
        var expected = new InvalidOperationException("err");

        PluginLog.Configure(
            isTraceEnabled: static () => true,
            trace: (m, e) => { capturedMessage = m; capturedException = e; });

        PluginLog.Error("bad-thing", expected);

        Assert.Equal("bad-thing", capturedMessage);
        Assert.Same(expected, capturedException);
    }

    [Fact]
    public void Reset_AfterConfigure_RestoresNoOpDefaults()
    {
        var sinkCalls = 0;
        PluginLog.Configure(
            isTraceEnabled: static () => true,
            trace: (_, _) => sinkCalls++);

        Assert.True(PluginLog.IsTraceEnabled);

        PluginLog.Reset();

        Assert.False(PluginLog.IsTraceEnabled);

        PluginLog.Trace("after-reset");
        PluginLog.Error("after-reset");

        Assert.Equal(0, sinkCalls);
    }

    [Fact]
    public void Configure_CalledTwice_SecondCallOverridesFirst()
    {
        var firstSinkCalls = 0;
        var secondSinkCalls = 0;

        PluginLog.Configure(
            isTraceEnabled: static () => true,
            trace: (_, _) => firstSinkCalls++);

        PluginLog.Configure(
            isTraceEnabled: static () => true,
            trace: (_, _) => secondSinkCalls++);

        PluginLog.Trace("msg");
        PluginLog.Error("msg");

        Assert.Equal(0, firstSinkCalls);
        Assert.Equal(2, secondSinkCalls);
    }
}

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Plugins.CustomMouse;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.CustomMouse.Tests;

[Collection("CustomMouseResourceCulture")]
public class ThemeWatcherRuntimeTests
{
    private static readonly MethodInfo OnDebounceElapsedMethod =
        typeof(ThemeWatcherRuntime).GetMethod("OnDebounceElapsed", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("ThemeWatcherRuntime.OnDebounceElapsed was not found.");

    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(10);

    private static ThemeWatcherRuntime CreateRuntime()
    {
        return new ThemeWatcherRuntime();
    }

    private static void InvokeDebounceElapsed(ThemeWatcherRuntime runtime)
    {
        OnDebounceElapsedMethod.Invoke(runtime, [null]);
    }

    private static bool IsLightOrDark(string? theme)
    {
        return string.Equals(theme, "light", StringComparison.Ordinal)
            || string.Equals(theme, "dark", StringComparison.Ordinal);
    }

    private static void WaitUntilThemeApplied(ThemeWatcherRuntime runtime)
    {
        Assert.True(
            SpinWait.SpinUntil(() => IsLightOrDark(runtime.PeekLastAppliedTheme()), EventTimeout),
            "Debounced theme apply should update last-applied theme.");
    }

    #region Lifecycle Tests

    [Fact]
    public void Start_InitialState_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        try
        {
            runtime.Start(null);
            Assert.NotEqual(CancellationToken.None, runtime.GetCancellationToken());
        }
        finally
        {
            runtime.Stop();
        }

        Assert.Equal(CancellationToken.None, runtime.GetCancellationToken());
    }

    [Fact]
    public void Start_MultipleTimes_OnlyOneInstance()
    {
        var runtime = CreateRuntime();

        try
        {
            runtime.Start(null);
            var firstToken = runtime.GetCancellationToken();
            runtime.Start(null);
            runtime.Start(null);

            var currentToken = runtime.GetCancellationToken();
            Assert.NotEqual(CancellationToken.None, currentToken);
            Assert.Equal(firstToken, currentToken);
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void Stop_AfterStart_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        runtime.Start(null);
        Assert.NotEqual(CancellationToken.None, runtime.GetCancellationToken());
        runtime.Stop();
        Assert.Equal(CancellationToken.None, runtime.GetCancellationToken());
    }

    [Fact]
    public void Stop_WithoutStart_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        runtime.Stop();
        Assert.Equal(CancellationToken.None, runtime.GetCancellationToken());
    }

    [Fact]
    public void Stop_MultipleTimes_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        runtime.Start(null);
        runtime.Stop();
        runtime.Stop();
        Assert.Equal(CancellationToken.None, runtime.GetCancellationToken());
    }

    [Fact]
    public void Start_AfterStop_CanRestart()
    {
        var runtime = CreateRuntime();

        try
        {
            runtime.Start(null);
            var firstToken = runtime.GetCancellationToken();
            runtime.Stop();

            runtime.Start(null);
            var restartedToken = runtime.GetCancellationToken();
            Assert.NotEqual(CancellationToken.None, restartedToken);
            Assert.NotEqual(firstToken, restartedToken);
        }
        finally
        {
            runtime.Stop();
        }
    }

    #endregion

    #region Event Tests

    [Fact]
    public void ThemeChanged_Event_CanSubscribe()
    {
        var runtime = CreateRuntime();
        using var invoked = new ManualResetEventSlim(false);
        string? receivedTheme = null;

        runtime.ThemeChanged += (theme, _) =>
        {
            receivedTheme = theme;
            invoked.Set();
            return Task.CompletedTask;
        };

        try
        {
            runtime.Start(null);
            InvokeDebounceElapsed(runtime);

            Assert.True(invoked.Wait(EventTimeout), "ThemeChanged handler should run after debounce elapsed.");
            Assert.True(IsLightOrDark(receivedTheme), $"Handler should receive light or dark, actual: {receivedTheme}");
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void ThemeChanged_Event_CanUnsubscribe()
    {
        var runtime = CreateRuntime();
        var callCount = 0;

        Func<string, CancellationToken, Task> handler = (_, _) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.CompletedTask;
        };
        runtime.ThemeChanged += handler;
        runtime.ThemeChanged -= handler;

        try
        {
            runtime.Start(null);
            InvokeDebounceElapsed(runtime);
            WaitUntilThemeApplied(runtime);

            Assert.Equal(0, callCount);
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void ThemeChanged_NoSubscriber_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        try
        {
            runtime.Start(null);
            InvokeDebounceElapsed(runtime);
            WaitUntilThemeApplied(runtime);
        }
        finally
        {
            runtime.Stop();
        }
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public void GetCancellationToken_WhenNotRunning_ReturnsNone()
    {
        var runtime = CreateRuntime();

        var token = runtime.GetCancellationToken();
        Assert.Equal(CancellationToken.None, token);
    }

    [Fact]
    public void GetCancellationToken_WhenRunning_ReturnsNonNone()
    {
        var runtime = CreateRuntime();

        try
        {
            runtime.Start(null);
            var token = runtime.GetCancellationToken();

            Assert.NotEqual(CancellationToken.None, token);
            Assert.False(token.IsCancellationRequested);
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void GetCancellationToken_AfterStop_ReturnsNone()
    {
        var runtime = CreateRuntime();

        runtime.Start(null);
        runtime.Stop();

        var token = runtime.GetCancellationToken();
        Assert.Equal(CancellationToken.None, token);
    }

    #endregion

    #region Initial Theme Tests

    [Fact]
    public void Start_WithNullInitialTheme_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        try
        {
            runtime.Start(null);
            Assert.Null(runtime.PeekLastAppliedTheme());
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void Start_WithLightInitialTheme_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        try
        {
            runtime.Start("light");
            Assert.Equal("light", runtime.PeekLastAppliedTheme());
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void Start_WithDarkInitialTheme_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        try
        {
            runtime.Start("dark");
            Assert.Equal("dark", runtime.PeekLastAppliedTheme());
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void Start_WithCustomInitialTheme_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        try
        {
            runtime.Start("custom-theme");
            Assert.Equal("custom-theme", runtime.PeekLastAppliedTheme());
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void Start_WhenAlreadyRunning_UpdatesLastAppliedTheme()
    {
        var runtime = CreateRuntime();

        try
        {
            runtime.Start("light");
            Assert.Equal("light", runtime.PeekLastAppliedTheme());

            runtime.Start("dark");
            Assert.Equal("dark", runtime.PeekLastAppliedTheme());
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void NotifyThemeApplied_UpdatesLastAppliedTheme()
    {
        var runtime = CreateRuntime();

        try
        {
            runtime.Start("light");
            runtime.NotifyThemeApplied("dark");

            Assert.Equal("dark", runtime.PeekLastAppliedTheme());
        }
        finally
        {
            runtime.Stop();
        }
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task StartAndStop_Concurrently_DoesNotThrow()
    {
        var runtime = CreateRuntime();
        using var bothReady = new Barrier(2);

        var startTask = Task.Run(() =>
        {
            bothReady.SignalAndWait(EventTimeout);
            runtime.Start(null);
        });
        var stopTask = Task.Run(() =>
        {
            bothReady.SignalAndWait(EventTimeout);
            runtime.Stop();
        });

        await Task.WhenAll(startTask, stopTask);
        runtime.Stop();
        Assert.Equal(CancellationToken.None, runtime.GetCancellationToken());
    }

    [Fact]
    public async Task MultipleStarts_Concurrently_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        try
        {
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() => runtime.Start(null));
            }

            await Task.WhenAll(tasks);
            Assert.NotEqual(CancellationToken.None, runtime.GetCancellationToken());
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public async Task MultipleStops_Concurrently_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        runtime.Start(null);

        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(runtime.Stop);
        }

        await Task.WhenAll(tasks);
        Assert.Equal(CancellationToken.None, runtime.GetCancellationToken());
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public void Stop_DisposesResources_CanRestart()
    {
        var runtime = CreateRuntime();

        try
        {
            runtime.Start(null);
            var firstToken = runtime.GetCancellationToken();
            runtime.Stop();

            runtime.Start(null);
            var restartedToken = runtime.GetCancellationToken();
            Assert.NotEqual(CancellationToken.None, restartedToken);
            Assert.NotEqual(firstToken, restartedToken);
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void Runtime_CanBeGarbageCollected()
    {
        WeakReference? weakRef;

        CreateAndReleaseRuntime(out weakRef);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(weakRef.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateAndReleaseRuntime(out WeakReference weakRef)
    {
        var runtime = CreateRuntime();
        runtime.Start(null);
        runtime.Stop();
        weakRef = new WeakReference(runtime);
    }

    #endregion

    #region CancellationToken Propagation Tests

    [Fact]
    public void ThemeChanged_HandlerReceivesCancelledTokenOnStop()
    {
        var runtime = CreateRuntime();
        using var tokenCancelled = new ManualResetEventSlim(false);
        using var handlerInvoked = new ManualResetEventSlim(false);

        runtime.ThemeChanged += async (_, token) =>
        {
            handlerInvoked.Set();
            try
            {
                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException)
            {
                tokenCancelled.Set();
            }
        };

        try
        {
            runtime.Start(null);
            InvokeDebounceElapsed(runtime);

            Assert.True(handlerInvoked.Wait(EventTimeout), "ThemeChanged handler should start before Stop.");
            runtime.Stop();
            Assert.True(tokenCancelled.Wait(EventTimeout), "Handler token should cancel when the runtime stops.");
        }
        finally
        {
            runtime.Stop();
        }
    }

    #endregion
}

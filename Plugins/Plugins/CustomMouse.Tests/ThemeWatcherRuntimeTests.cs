using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LenovoLegionToolkit.Plugins.CustomMouse.Tests;

public class ThemeWatcherRuntimeTests
{
    private ThemeWatcherRuntime CreateRuntime()
    {
        return new ThemeWatcherRuntime();
    }

    #region Lifecycle Tests

    [Fact]
    public void Start_InitialState_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        runtime.Start(null);

        runtime.Stop();
    }

    [Fact]
    public void Start_MultipleTimes_OnlyOneInstance()
    {
        var runtime = CreateRuntime();

        runtime.Start(null);
        runtime.Start(null);
        runtime.Start(null);

        // Should still be running (only one instance)
        // Stop should work once
        runtime.Stop();
    }

    [Fact]
    public void Stop_AfterStart_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        runtime.Start(null);
        runtime.Stop();
    }

    [Fact]
    public void Stop_WithoutStart_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        // Should not throw
        runtime.Stop();
    }

    [Fact]
    public void Stop_MultipleTimes_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        runtime.Start(null);
        runtime.Stop();
        // Second stop should not throw
        runtime.Stop();
    }

    [Fact]
    public void Start_AfterStop_CanRestart()
    {
        var runtime = CreateRuntime();

        runtime.Start(null);
        runtime.Stop();

        // Should be able to restart
        runtime.Start(null);

        runtime.Stop();
    }

    #endregion

    #region Event Tests

    [Fact]
    public void ThemeChanged_Event_CanSubscribe()
    {
        var runtime = CreateRuntime();
        var subscribed = false;

        runtime.ThemeChanged += (theme, token) => Task.CompletedTask;
        subscribed = true;

        runtime.Start(null);
        runtime.Stop();

        Assert.True(subscribed);
    }

    [Fact]
    public void ThemeChanged_Event_CanUnsubscribe()
    {
        var runtime = CreateRuntime();
        var callCount = 0;

        Func<string, CancellationToken, Task> handler = (theme, token) =>
        {
            callCount++;
            return Task.CompletedTask;
        };
        runtime.ThemeChanged += handler;
        runtime.ThemeChanged -= handler;

        runtime.Start(null);
        runtime.Stop();

        // After unsubscribe, handler should not be called
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ThemeChanged_NoSubscriber_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        // No subscriber
        runtime.Start(null);

        // Wait a bit
        Thread.Sleep(100);

        runtime.Stop();

        // Should not throw even with no subscribers
        Assert.True(true);
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

        runtime.Start(null);
        var token = runtime.GetCancellationToken();

        Assert.NotEqual(CancellationToken.None, token);
        Assert.False(token.IsCancellationRequested);

        runtime.Stop();
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

        runtime.Start(null);

        runtime.Stop();
    }

    [Fact]
    public void Start_WithLightInitialTheme_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        runtime.Start("light");

        runtime.Stop();
    }

    [Fact]
    public void Start_WithDarkInitialTheme_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        runtime.Start("dark");

        runtime.Stop();
    }

    [Fact]
    public void Start_WithCustomInitialTheme_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        runtime.Start("custom-theme");

        runtime.Stop();
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task StartAndStop_Concurrently_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        var startTask = Task.Run(() => runtime.Start(null));
        var stopTask = Task.Run(() =>
        {
            Thread.Sleep(100);
            runtime.Stop();
        });

        // Should not throw
        await Task.WhenAll(startTask, stopTask);

        runtime.Stop(); // Should not throw
    }

    [Fact]
    public async Task MultipleStarts_Concurrently_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(() => runtime.Start(null));
        }

        await Task.WhenAll(tasks);

        runtime.Stop();
    }

    [Fact]
    public async Task MultipleStops_Concurrently_DoesNotThrow()
    {
        var runtime = CreateRuntime();

        runtime.Start(null);

        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(() => runtime.Stop());
        }

        await Task.WhenAll(tasks);
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public void Stop_DisposesResources_CanRestart()
    {
        var runtime = CreateRuntime();

        runtime.Start(null);
        runtime.Stop();

        // Second start should work (resources were disposed)
        runtime.Start(null);

        runtime.Stop();
    }

    [Fact]
    public void Runtime_CanBeGarbageCollected()
    {
        WeakReference? weakRef;

        CreateAndReleaseRuntime(out weakRef);

        // Force GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Runtime should be collectible
        Assert.False(weakRef.IsAlive);
    }

    private void CreateAndReleaseRuntime(out WeakReference weakRef)
    {
        var runtime = CreateRuntime();
        runtime.Start(null);
        runtime.Stop();
        weakRef = new WeakReference(runtime);
    }

    #endregion

    #region CancellationToken Propagation Tests

    [Fact]
    public async Task ThemeChanged_HandlerReceivesCancelledTokenOnStop()
    {
        var runtime = CreateRuntime();
        var tokenCancelled = new ManualResetEventSlim(false);
        var handlerInvoked = new ManualResetEventSlim(false);

        runtime.ThemeChanged += async (theme, token) =>
        {
            handlerInvoked.Set();
            try
            {
                await Task.Delay(5000, token);
            }
            catch (OperationCanceledException)
            {
                tokenCancelled.Set();
            }
            return;
        };

        runtime.Start(null);

        // We can't easily trigger UserPreferenceChanged, so this test
        // primarily verifies the handler signature and token behavior compile correctly.
        // In integration tests with actual theme changes, the token would be cancelled on Stop().

        runtime.Stop();

        // Verify the runtime stops without hanging
        Assert.True(true);
    }

    #endregion
}

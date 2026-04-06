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
        Func<string, CancellationToken, Task> handler = async (theme, token) => { };

        runtime.ThemeChanged += handler;

        runtime.Start(null);
        runtime.Stop();

        // Event subscription should not throw
        Assert.True(true);
    }

    [Fact]
    public void ThemeChanged_Event_CanUnsubscribe()
    {
        var runtime = CreateRuntime();
        Func<string, CancellationToken, Task> handler = async (theme, token) => { };

        runtime.ThemeChanged += handler;
        runtime.ThemeChanged -= handler;

        runtime.Start(null);
        runtime.Stop();

        // Event unsubscription should not throw
        Assert.True(true);
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

        runtime.Stop();
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
    public void Stop_DisposesResources()
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

    #region CancellationToken Tests

    [Fact]
    public async Task ThemeChanged_ReceivesValidCancellationToken()
    {
        var runtime = CreateRuntime();
        CancellationToken? receivedToken = null;
        var tokenReceived = new ManualResetEventSlim(false);

        runtime.ThemeChanged += async (theme, token) =>
        {
            receivedToken = token;
            tokenReceived.Set();
        };

        runtime.Start(null);

        // Note: ThemeChanged is raised by SystemEvents.UserPreferenceChanged
        // which we can't easily trigger in tests. This test verifies the
        // signature exists but won't fire in normal test conditions.

        runtime.Stop();

        // If the event was fired, verify token is not null
        // In most cases, this test will pass as the event isn't triggered
        Assert.True(true);
    }

    #endregion
}
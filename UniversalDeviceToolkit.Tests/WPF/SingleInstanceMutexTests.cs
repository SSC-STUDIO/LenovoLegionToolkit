using System.Reflection;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class SingleInstanceMutexTests
{
    // ── Source-code structure tests ──────────────────────────────────────

    [Fact]
    public void ExitDuplicateInstance_ShouldCallEnvironmentExitWithCodeZero()
    {
        var source = ReadAppSource();
        var method = ExtractMethodBody(source, "ExitDuplicateInstance");
        method.Should().Contain("Environment.Exit(0)");
    }

    [Fact]
    public void ExitDuplicateInstance_ShouldFallBackToExitProcess()
    {
        var source = ReadAppSource();
        var method = ExtractMethodBody(source, "ExitDuplicateInstance");
        method.Should().Contain("ExitProcess(0)");
    }

    [Fact]
    public void EnsureSingleInstance_WhenMutexNotOwned_ShouldExit()
    {
        var source = ReadAppSource();
        var startup = ExtractMethodBody(source, "Application_Startup");
        startup.Should().Contain("EnsureSingleInstance()");
        startup.Should().Contain("ExitDuplicateInstance()");
    }

    [Fact]
    public void ExitDuplicateInstance_ShouldPrecedeEnsureSingleInstanceInStartup()
    {
        var source = ReadAppSource();
        var startup = ExtractMethodBody(source, "Application_Startup");

        var callSite = "if (!EnsureSingleInstance())";
        var exitCall = "ExitDuplicateInstance()";

        startup.Should().Contain(callSite);
        startup.IndexOf(callSite, StringComparison.Ordinal)
            .Should()
            .BeLessThan(startup.IndexOf(exitCall, StringComparison.Ordinal));
    }

    // ── Naming-convention tests ──────────────────────────────────────────

    [Fact]
    public void MutexName_ShouldBeBasedOnAppIdentityCompactName()
    {
        var source = ReadAppSource();
        var expected = "AppIdentity.CompactName + \"_Mutex_6efcc882-924c-4cbc-8fec-f45c25696f98\"";
        source.Should().Contain(expected);
    }

    [Fact]
    public void EventName_ShouldBeBasedOnAppIdentityCompactName()
    {
        var source = ReadAppSource();
        var expected = "AppIdentity.CompactName + \"_Event_6efcc882-924c-4cbc-8fec-f45c25696f98\"";
        source.Should().Contain(expected);
    }

    [Fact]
    public void AckEventName_ShouldBeBasedOnAppIdentityCompactName()
    {
        var source = ReadAppSource();
        var expected = "AppIdentity.CompactName + \"_AckEvent_6efcc882-924c-4cbc-8fec-f45c25696f98\"";
        source.Should().Contain(expected);
    }

    [Fact]
    public void LegacyMutexName_ShouldUseLegacyCompactName()
    {
        var source = ReadAppSource();
        var expected = "AppIdentity.LegacyCompactName + \"_Mutex_6efcc882-924c-4cbc-8fec-f45c25696f98\"";
        source.Should().Contain(expected);
    }

    [Fact]
    public void LegacyEventName_ShouldUseLegacyCompactName()
    {
        var source = ReadAppSource();
        var expected = "AppIdentity.LegacyCompactName + \"_Event_6efcc882-924c-4cbc-8fec-f45c25696f98\"";
        source.Should().Contain(expected);
    }

    [Fact]
    public void LegacyAckEventName_ShouldUseLegacyCompactName()
    {
        var source = ReadAppSource();
        var expected = "AppIdentity.LegacyCompactName + \"_AckEvent_6efcc882-924c-4cbc-8fec-f45c25696f98\"";
        source.Should().Contain(expected);
    }

    [Fact]
    public void RecoverySuffix_ShouldBeUnderscoreRecovery()
    {
        var source = ReadAppSource();
        source.Should().Contain("RECOVERY_SINGLE_INSTANCE_SUFFIX = \"_Recovery\"");
    }

    [Fact]
    public void Constants_AreResolvedThroughResolveSingleInstanceObjectName()
    {
        var source = ReadAppSource();
        var method = ExtractMethodBody(source, "EnsureSingleInstance");
        method.Should().Contain("ResolveSingleInstanceObjectName(MUTEX_NAME)");
        method.Should().Contain("ResolveSingleInstanceObjectName(EVENT_NAME)");
        method.Should().Contain("ResolveSingleInstanceObjectName(ACK_EVENT_NAME)");
        method.Should().Contain("ResolveSingleInstanceObjectName(LEGACY_MUTEX_NAME)");
        method.Should().Contain("ResolveSingleInstanceObjectName(LEGACY_EVENT_NAME)");
        method.Should().Contain("ResolveSingleInstanceObjectName(LEGACY_ACK_EVENT_NAME)");
    }

    // ── ResolveSingleInstanceObjectName (reflection, UDT_TEST_HOOKS) ────

    [Fact]
    public void ResolveSingleInstanceObjectName_WithoutIsolationKey_ReturnsBaseName()
    {
        var prevEnv = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, null);
            var result = InvokeResolveSingleInstanceObjectName("TestMutex");
            result.Should().Be("TestMutex");
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, prevEnv);
        }
    }

    [Fact]
    public void ResolveSingleInstanceObjectName_WithIsolationKey_AppendsSanitizedKey()
    {
        var prevEnv = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        try
        {
            var isolationPath = @"C:\Temp\UDT_Test_Isolation";
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, isolationPath);

            var result = InvokeResolveSingleInstanceObjectName("TestMutex");

            var fullPath = Path.GetFullPath(isolationPath);
            var sanitized = string.Concat(fullPath
                .Trim()
                .Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
            result.Should().Be($"TestMutex_{sanitized}");
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, prevEnv);
        }
    }

    [Fact]
    public void ResolveSingleInstanceObjectName_WithWhitespaceIsolationKey_ReturnsBaseName()
    {
        var prevEnv = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, "   ");
            var result = InvokeResolveSingleInstanceObjectName("TestMutex");
            result.Should().Be("TestMutex");
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, prevEnv);
        }
    }

    // ── Mutex-based duplicate detection (standalone kernel objects) ─────

    [Fact]
    public void FirstInstanceMutex_CreatedFirst_ShouldBeOwned()
    {
        var uniqueName = $"UDT_Mutex_FirstOwned_{Guid.NewGuid():N}";
        using var mutex = new Mutex(true, uniqueName, out var isOwned);
        isOwned.Should().BeTrue();
    }

    [Fact]
    public void SecondInstanceMutex_WhenFirstHeld_ShouldNotBeOwned()
    {
        var uniqueName = $"UDT_Mutex_SecondNotOwned_{Guid.NewGuid():N}";
        using var first = new Mutex(true, uniqueName, out _);
        using var second = new Mutex(true, uniqueName, out var isOwned);
        isOwned.Should().BeFalse();
    }

    [Fact]
    public void SecondInstanceMutex_WhenFirstReleased_ShouldBeOwned()
    {
        var uniqueName = $"UDT_Mutex_SecondOwnedAfterRelease_{Guid.NewGuid():N}";
        var first = new Mutex(true, uniqueName, out _);
        first.ReleaseMutex();
        first.Dispose();
        using var second = new Mutex(true, uniqueName, out var isOwned);
        isOwned.Should().BeTrue();
    }

    // ── EventWaitHandle signal/ack pattern ──────────────────────────────

    [Fact]
    public void SignalAndWaitForAck_WhenListenerAcknowledges_ReturnsTrue()
    {
        var signalName = $"UDT_Signal_Signal_{Guid.NewGuid():N}";
        var ackName = $"UDT_Signal_Ack_{Guid.NewGuid():N}";

        using var signal = new EventWaitHandle(false, EventResetMode.AutoReset, signalName);
        using var ack = new EventWaitHandle(false, EventResetMode.AutoReset, ackName);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listenerTask = Task.Run(() =>
        {
            using var listenerSignal = new EventWaitHandle(false, EventResetMode.AutoReset, signalName);
            using var listenerAck = new EventWaitHandle(false, EventResetMode.AutoReset, ackName);
            listenerSignal.WaitOne();
            listenerAck.Set();
        }, cts.Token);

        signal.Set();
        var ackReceived = ack.WaitOne(TimeSpan.FromSeconds(3));
        ackReceived.Should().BeTrue();
    }

    [Fact]
    public void SignalAndWaitForAck_WhenNoAcknowledgment_ReturnsFalse()
    {
        var signalName = $"UDT_Signal_NoAckSignal_{Guid.NewGuid():N}";
        var ackName = $"UDT_Signal_NoAckAck_{Guid.NewGuid():N}";

        using var signal = new EventWaitHandle(false, EventResetMode.AutoReset, signalName);
        using var ack = new EventWaitHandle(false, EventResetMode.AutoReset, ackName);

        signal.Set();
        var ackReceived = ack.WaitOne(TimeSpan.FromMilliseconds(50));
        ackReceived.Should().BeFalse();
    }

    [Fact]
    public void WaitAnyOnMultipleHandles_WhenAnySignaled_ReturnsNonTimeout()
    {
        var event1Name = $"UDT_WaitAny_Signal_{Guid.NewGuid():N}";
        var event2Name = $"UDT_WaitAny_Signal2_{Guid.NewGuid():N}";

        using var event1 = new EventWaitHandle(false, EventResetMode.AutoReset, event1Name);
        using var event2 = new EventWaitHandle(false, EventResetMode.AutoReset, event2Name);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = Task.Run(() =>
        {
            using var sig = new EventWaitHandle(false, EventResetMode.AutoReset, event2Name);
            Thread.Sleep(100);
            sig.Set();
        }, cts.Token);

        var signaledIndex = WaitHandle.WaitAny([event1, event2], TimeSpan.FromSeconds(3));
        signaledIndex.Should().NotBe(WaitHandle.WaitTimeout);
        signaledIndex.Should().Be(1);
    }

    [Fact]
    public void WaitAnyOnMultipleHandles_WhenNoneSignaled_ReturnsTimeout()
    {
        var event1Name = $"UDT_WaitAny_Timeout1_{Guid.NewGuid():N}";
        var event2Name = $"UDT_WaitAny_Timeout2_{Guid.NewGuid():N}";

        using var event1 = new EventWaitHandle(false, EventResetMode.AutoReset, event1Name);
        using var event2 = new EventWaitHandle(false, EventResetMode.AutoReset, event2Name);

        var signaledIndex = WaitHandle.WaitAny([event1, event2], TimeSpan.FromMilliseconds(50));
        signaledIndex.Should().Be(WaitHandle.WaitTimeout);
    }

    // ── Full orchestration flow: first + second instance ─────────────────

    [Fact]
    public async Task CrossInstanceFlow_SecondInstanceSignalsAndFirstReceives()
    {
        var mutexName = $"UDT_Flow_Mutex_{Guid.NewGuid():N}";
        var signalName = $"UDT_Flow_Signal_{Guid.NewGuid():N}";
        var ackName = $"UDT_Flow_Ack_{Guid.NewGuid():N}";
        var firstReady = new ManualResetEventSlim(false);
        var firstDone = new ManualResetEventSlim(false);

        var firstInstanceTask = Task.Run(() =>
        {
            using var mutex = new Mutex(true, mutexName, out var owned);
            owned.Should().BeTrue("first instance should own the mutex");

            using var signalHandle = new EventWaitHandle(false, EventResetMode.AutoReset, signalName);
            using var ackHandle = new EventWaitHandle(false, EventResetMode.AutoReset, ackName);

            firstReady.Set();

            var gotSignal = signalHandle.WaitOne(TimeSpan.FromSeconds(5));
            gotSignal.Should().BeTrue("first instance should receive the signal");

            ackHandle.Set();
            firstDone.Set();
        });

        firstReady.Wait(TimeSpan.FromSeconds(3));

        var secondInstanceTask = Task.Run(() =>
        {
            using var mutex = new Mutex(true, mutexName, out var owned);
            owned.Should().BeFalse("second instance should not own the mutex");

            using var signalHandle = new EventWaitHandle(false, EventResetMode.AutoReset, signalName);
            using var ackHandle = new EventWaitHandle(false, EventResetMode.AutoReset, ackName);

            ackHandle.Reset();
            signalHandle.Set();

            var ackReceived = ackHandle.WaitOne(TimeSpan.FromSeconds(5));
            ackReceived.Should().BeTrue("second instance should receive acknowledgment");
        });

        await Task.WhenAll(firstInstanceTask, secondInstanceTask);
        firstDone.IsSet.Should().BeTrue("first instance should complete the handshake");
    }

    [Fact]
    public async Task CrossInstanceFlow_WithLegacyNames_LegacySignalTriggersFirstInstance()
    {
        var mutexName = $"UDT_Legacy_Mutex_{Guid.NewGuid():N}";
        var legacySignalName = $"UDT_Legacy_Signal_{Guid.NewGuid():N}";
        var ackName = $"UDT_Legacy_Ack_{Guid.NewGuid():N}";
        var firstReady = new ManualResetEventSlim(false);

        var firstInstanceTask = Task.Run(() =>
        {
            using var mutex = new Mutex(true, mutexName, out var owned);
            owned.Should().BeTrue();

            using var legacySignal = new EventWaitHandle(false, EventResetMode.AutoReset, legacySignalName);
            using var ackHandle = new EventWaitHandle(false, EventResetMode.AutoReset, ackName);

            firstReady.Set();

            var handles = new WaitHandle[] { legacySignal };
            var signaledIndex = WaitHandle.WaitAny(handles, TimeSpan.FromSeconds(5));
            signaledIndex.Should().NotBe(WaitHandle.WaitTimeout, "legacy signal should be received");

            ackHandle.Set();
        });

        firstReady.Wait(TimeSpan.FromSeconds(3));

        var secondInstanceTask = Task.Run(() =>
        {
            using var legacySignal = new EventWaitHandle(false, EventResetMode.AutoReset, legacySignalName);
            using var ackHandle = new EventWaitHandle(false, EventResetMode.AutoReset, ackName);

            ackHandle.Reset();
            legacySignal.Set();

            var ackReceived = ackHandle.WaitOne(TimeSpan.FromSeconds(5));
            ackReceived.Should().BeTrue("second instance should get ack through legacy path");
        });

        await Task.WhenAll(firstInstanceTask, secondInstanceTask);
    }

    // ── Recovery suffix flow ─────────────────────────────────────────────

    [Fact]
    public void RecoveryMutex_WhenPrimaryFails_ShouldUseRecoverySuffix()
    {
        var baseMutexName = $"UDT_Recovery_Base_{Guid.NewGuid():N}";
        var recoveryMutexName = baseMutexName + "_Recovery";

        using var first = new Mutex(true, recoveryMutexName, out var owned);
        owned.Should().BeTrue("first recovery instance should own recovery mutex");

        using var second = new Mutex(true, recoveryMutexName, out var secondOwned);
        secondOwned.Should().BeFalse("second recovery instance should not own recovery mutex");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static string ReadAppSource()
    {
        var expectedRelativePath = Path.Combine("UniversalDeviceToolkit.WPF", "App.xaml.cs");
        foreach (var candidateRoot in GetRepositoryRootCandidates())
        {
            var path = Path.Combine(candidateRoot, expectedRelativePath);
            if (File.Exists(path))
                return File.ReadAllText(path);
        }

        throw new DirectoryNotFoundException($"Could not locate '{expectedRelativePath}'.");
    }

    private static IEnumerable<string> GetRepositoryRootCandidates()
    {
        var roots = new[]
        {
            Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT"),
            Environment.CurrentDirectory,
            AppContext.BaseDirectory,
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."))
        };

        foreach (var root in roots.Where(static r => !string.IsNullOrWhiteSpace(r)))
        {
            var directory = new DirectoryInfo(root!);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
                    yield return directory.FullName;

                directory = directory.Parent;
            }
        }
    }

    private static string ExtractMethodBody(string source, string methodName)
    {
        var searchFor = $"{methodName}(";
        var startIndex = source.IndexOf(searchFor, StringComparison.Ordinal);
        startIndex.Should().BeGreaterThanOrEqualTo(0, $"method '{methodName}' should exist in source");

        var braceStart = source.IndexOf('{', startIndex);
        braceStart.Should().BeGreaterThan(0);

        var depth = 0;
        for (var i = braceStart; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}') depth--;

            if (depth == 0)
                return source[braceStart..(i + 1)];
        }

        throw new InvalidOperationException($"Could not find closing brace for method '{methodName}'.");
    }

    private static string InvokeResolveSingleInstanceObjectName(string baseName)
    {
        var method = typeof(App).GetMethod("ResolveSingleInstanceObjectName",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull("ResolveSingleInstanceObjectName should be accessible via reflection");

        var result = method!.Invoke(null, [baseName]);
        return result.Should().BeOfType<string>().Subject;
    }
}

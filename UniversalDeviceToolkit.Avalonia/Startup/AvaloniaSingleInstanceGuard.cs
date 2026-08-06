#if WINDOWS

using System.IO;
using UniversalDeviceToolkit.Lib.System;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Startup;

/// <summary>
/// Shares WPF's instance identity so starting either desktop host activates the
/// already-running process instead of leaving two hardware controllers active.
/// </summary>
internal sealed class AvaloniaSingleInstanceGuard : IDisposable
{
    private const string PrimaryMutexName = AppIdentity.CompactName + "_Mutex_6efcc882-924c-4cbc-8fec-f45c25696f98";
    private const string PrimaryEventName = AppIdentity.CompactName + "_Event_6efcc882-924c-4cbc-8fec-f45c25696f98";
    private const string PrimaryAckName = AppIdentity.CompactName + "_AckEvent_6efcc882-924c-4cbc-8fec-f45c25696f98";
    private const string LegacyMutexName = AppIdentity.LegacyCompactName + "_Mutex_6efcc882-924c-4cbc-8fec-f45c25696f98";
    private const string LegacyEventName = AppIdentity.LegacyCompactName + "_Event_6efcc882-924c-4cbc-8fec-f45c25696f98";
    private const string LegacyAckName = AppIdentity.LegacyCompactName + "_AckEvent_6efcc882-924c-4cbc-8fec-f45c25696f98";
    private const string RecoverySuffix = "_Recovery";
    private const int ActivationTimeoutMilliseconds = 1200;

    private readonly object _gate = new();
    private Mutex? _primaryMutex;
    private Mutex? _legacyMutex;
    private EventWaitHandle? _primarySignal;
    private EventWaitHandle? _legacySignal;
    private EventWaitHandle? _primaryAck;
    private EventWaitHandle? _legacyAck;
    private bool _ownsPrimaryMutex;
    private bool _ownsLegacyMutex;
    private Thread? _listener;
    private bool _disposed;

    public bool TryAcquire()
    {
        ThrowIfDisposed();
        CreateHandles(string.Empty);
        if (_ownsPrimaryMutex && _ownsLegacyMutex)
            return true;

        if (SignalAndAwaitActivation())
        {
            Dispose();
            return false;
        }

        // Keep WPF's stale-process recovery behavior. A hung process should
        // not make the new host permanently impossible to start.
        DisposeHandles(releaseMutexes: true);
        CreateHandles(RecoverySuffix);
        if (_ownsPrimaryMutex && _ownsLegacyMutex)
            return true;

        SignalAndAwaitActivation();
        Dispose();
        return false;
    }

    public void StartListener(Action onSecondaryLaunch)
    {
        ArgumentNullException.ThrowIfNull(onSecondaryLaunch);
        ThrowIfDisposed();
        if (!_ownsPrimaryMutex || !_ownsLegacyMutex)
            throw new InvalidOperationException("Only the primary instance can listen for activation.");
        if (_listener is { IsAlive: true })
            return;

        _listener = new Thread(() => Listen(onSecondaryLaunch))
        {
            IsBackground = true,
            Name = "AvaloniaSingleInstanceListener",
        };
        _listener.Start();
    }

    private void Listen(Action onSecondaryLaunch)
    {
        try
        {
            while (WaitForSignal())
            {
                SignalPrimaryInstance();
                onSecondaryLaunch();
            }
        }
        catch (ObjectDisposedException)
        {
            // Expected while the primary host is exiting.
        }
    }

    private bool WaitForSignal()
    {
        var handles = new[] { _primarySignal, _legacySignal }
            .Where(handle => handle is not null)
            .Cast<WaitHandle>()
            .ToArray();
        return handles.Length > 0 && WaitHandle.WaitAny(handles) != WaitHandle.WaitTimeout;
    }

    private bool SignalAndAwaitActivation()
    {
        try { _primaryAck?.Reset(); } catch { }
        try { _legacyAck?.Reset(); } catch { }
        try { _primarySignal?.Set(); } catch { }
        try { _legacySignal?.Set(); } catch { }

        var handles = new[] { _primaryAck, _legacyAck }
            .Where(handle => handle is not null)
            .Cast<WaitHandle>()
            .ToArray();
        return handles.Length > 0
               && WaitHandle.WaitAny(handles, ActivationTimeoutMilliseconds) != WaitHandle.WaitTimeout;
    }

    private void SignalPrimaryInstance()
    {
        try { _primaryAck?.Set(); } catch { }
        try { _legacyAck?.Set(); } catch { }
    }

    private void CreateHandles(string suffix)
    {
        _primaryMutex = new Mutex(true, ResolveObjectName(PrimaryMutexName, suffix), out _ownsPrimaryMutex);
        _legacyMutex = new Mutex(true, ResolveObjectName(LegacyMutexName, suffix), out _ownsLegacyMutex);
        _primarySignal = new EventWaitHandle(false, EventResetMode.AutoReset, ResolveObjectName(PrimaryEventName, suffix));
        _legacySignal = new EventWaitHandle(false, EventResetMode.AutoReset, ResolveObjectName(LegacyEventName, suffix));
        _primaryAck = new EventWaitHandle(false, EventResetMode.AutoReset, ResolveObjectName(PrimaryAckName, suffix));
        _legacyAck = new EventWaitHandle(false, EventResetMode.AutoReset, ResolveObjectName(LegacyAckName, suffix));
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
        }

        DisposeHandles(releaseMutexes: true);
        GC.SuppressFinalize(this);
    }

    private void DisposeHandles(bool releaseMutexes)
    {
        try { _primarySignal?.Dispose(); } catch { }
        try { _legacySignal?.Dispose(); } catch { }
        try { _primaryAck?.Dispose(); } catch { }
        try { _legacyAck?.Dispose(); } catch { }
        _primarySignal = null;
        _legacySignal = null;
        _primaryAck = null;
        _legacyAck = null;

        if (_listener is { IsAlive: true })
            _listener.Join(500);
        _listener = null;

        if (releaseMutexes)
        {
            try { if (_ownsPrimaryMutex) _primaryMutex?.ReleaseMutex(); } catch (ApplicationException) { }
            try { if (_ownsLegacyMutex) _legacyMutex?.ReleaseMutex(); } catch (ApplicationException) { }
        }

        _ownsPrimaryMutex = false;
        _ownsLegacyMutex = false;
        _primaryMutex?.Dispose();
        _legacyMutex?.Dispose();
        _primaryMutex = null;
        _legacyMutex = null;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AvaloniaSingleInstanceGuard));
    }

    private static string ResolveObjectName(string baseName, string suffix)
    {
#if UDT_TEST_HOOKS
        var isolationKey = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(isolationKey))
        {
            try { isolationKey = Path.GetFullPath(isolationKey); }
            catch { /* Preserve the raw isolation key when it is not a path. */ }

            var sanitizedKey = string.Concat(isolationKey
                .Trim()
                .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_'));
            if (!string.IsNullOrWhiteSpace(sanitizedKey))
                return $"{baseName}_{sanitizedKey}{suffix}";
        }
#endif
        return baseName + suffix;
    }
}

#endif

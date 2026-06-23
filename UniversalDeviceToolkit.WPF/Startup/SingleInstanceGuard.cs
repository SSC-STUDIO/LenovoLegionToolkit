using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.WPF.Startup
{
    internal sealed class SingleInstanceGuard : IDisposable
    {
        private const string MUTEX_NAME = AppIdentity.CompactName + "_Mutex_6efcc882-924c-4cbc-8fec-f45c25696f98";
        private const string EVENT_NAME = AppIdentity.CompactName + "_Event_6efcc882-924c-4cbc-8fec-f45c25696f98";
        private const string ACK_EVENT_NAME = AppIdentity.CompactName + "_AckEvent_6efcc882-924c-4cbc-8fec-f45c25696f98";
        private const string LEGACY_MUTEX_NAME = AppIdentity.LegacyCompactName + "_Mutex_6efcc882-924c-4cbc-8fec-f45c25696f98";
        private const string LEGACY_EVENT_NAME = AppIdentity.LegacyCompactName + "_Event_6efcc882-924c-4cbc-8fec-f45c25696f98";
        private const string LEGACY_ACK_EVENT_NAME = AppIdentity.LegacyCompactName + "_AckEvent_6efcc882-924c-4cbc-8fec-f45c25696f98";
        private const int SINGLE_INSTANCE_ACTIVATION_TIMEOUT_MS = 1200;
        private const string RECOVERY_SINGLE_INSTANCE_SUFFIX = "_Recovery";

        private readonly Dispatcher _dispatcher;
        private readonly object _stateLock = new();

        private Mutex? _primaryMutex;
        private Mutex? _legacyMutex;
        private EventWaitHandle? _primarySignal;
        private EventWaitHandle? _legacySignal;
        private EventWaitHandle? _primaryAck;
        private EventWaitHandle? _legacyAck;
        private bool _primaryMutexOwned;
        private bool _legacyMutexOwned;
        private Thread? _listenerThread;
        private bool _disposed;

        public SingleInstanceGuard(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public bool TryAcquire(out int exitCode)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SingleInstanceGuard));

            exitCode = 0;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Checking for other instances...");

            var mutexName = ResolveSingleInstanceObjectName(MUTEX_NAME);
            var eventName = ResolveSingleInstanceObjectName(EVENT_NAME);
            var ackEventName = ResolveSingleInstanceObjectName(ACK_EVENT_NAME);
            var legacyMutexName = ResolveSingleInstanceObjectName(LEGACY_MUTEX_NAME);
            var legacyEventName = ResolveSingleInstanceObjectName(LEGACY_EVENT_NAME);
            var legacyAckEventName = ResolveSingleInstanceObjectName(LEGACY_ACK_EVENT_NAME);

            _primaryMutex = new Mutex(true, mutexName, out var isOwned);
            _primaryMutexOwned = isOwned;
            _primarySignal = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
            _primaryAck = new EventWaitHandle(false, EventResetMode.AutoReset, ackEventName);
            _legacyMutex = new Mutex(true, legacyMutexName, out var legacyIsOwned);
            _legacyMutexOwned = legacyIsOwned;
            _legacySignal = new EventWaitHandle(false, EventResetMode.AutoReset, legacyEventName);
            _legacyAck = new EventWaitHandle(false, EventResetMode.AutoReset, legacyAckEventName);

            if (isOwned && legacyIsOwned)
                return true;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Another instance running, signaling existing instance...");

            if (SignalAndWaitForSingleInstanceActivation())
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Another instance acknowledged activation, closing...");

                Dispose();
                exitCode = 0;
                return false;
            }

            if (TrySwitchToRecoverySingleInstance(mutexName, eventName, ackEventName, legacyMutexName, legacyEventName, legacyAckEventName))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Existing instance did not acknowledge activation; continuing with recovery single-instance guard.");
                return true;
            }

            Dispose();
            exitCode = 1;
            return false;
        }

        public void SignalPrimaryInstance()
        {
            try { _primaryAck?.Set(); }
            catch { /* Activation acknowledgement is best effort. */ }

            try { _legacyAck?.Set(); }
            catch { /* Activation acknowledgement is best effort. */ }
        }

        public void StartListener(Action onSecondaryLaunch)
        {
            if (onSecondaryLaunch is null)
                throw new ArgumentNullException(nameof(onSecondaryLaunch));

            if (_disposed)
                throw new ObjectDisposedException(nameof(SingleInstanceGuard));

            _listenerThread = new Thread(() =>
            {
                try
                {
                    while (WaitForSingleInstanceSignal())
                    {
                        SignalPrimaryInstance();
                        onSecondaryLaunch();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Expected when wait handle is disposed during shutdown
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error in single instance thread.", ex);
                }
            })
            {
                IsBackground = true,
                Name = "SingleInstanceThread"
            };
            _listenerThread.Start();
        }

        public void StopListener()
        {
            if (_listenerThread is not { IsAlive: true })
                return;

            try
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Stopping single instance thread...");

                DisposeWaitHandles();

                if (!_listenerThread.Join(500))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Single instance thread did not finish in time.");
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error stopping single instance thread: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            lock (_stateLock)
            {
                if (_disposed)
                    return;
                _disposed = true;
            }

            StopListener();
            ReleaseAndCloseMutexes();
            DisposeWaitHandles();
            GC.SuppressFinalize(this);
        }

        private bool WaitForSingleInstanceSignal()
        {
            var handles = new[] { _primarySignal, _legacySignal }
                .Where(handle => handle is not null)
                .Cast<WaitHandle>()
                .ToArray();

            if (handles.Length == 0)
                return false;

            return WaitHandle.WaitAny(handles) != WaitHandle.WaitTimeout;
        }

        private bool SignalAndWaitForSingleInstanceActivation()
        {
            try { _primaryAck?.Reset(); }
            catch { /* Reset is best effort. */ }
            try { _legacyAck?.Reset(); }
            catch { /* Reset is best effort. */ }

            try { _primarySignal?.Set(); }
            catch { /* Signal is best effort. */ }
            try { _legacySignal?.Set(); }
            catch { /* Signal is best effort. */ }

            var handles = new[] { _primaryAck, _legacyAck }
                .Where(handle => handle is not null)
                .Cast<WaitHandle>()
                .ToArray();

            return handles.Length > 0
                && WaitHandle.WaitAny(handles, SINGLE_INSTANCE_ACTIVATION_TIMEOUT_MS) != WaitHandle.WaitTimeout;
        }

        private bool TrySwitchToRecoverySingleInstance(
            string mutexName,
            string eventName,
            string ackEventName,
            string legacyMutexName,
            string legacyEventName,
            string legacyAckEventName)
        {
            ReleaseAndCloseMutexes();
            DisposeWaitHandles();

            var recoveryMutexName = mutexName + RECOVERY_SINGLE_INSTANCE_SUFFIX;
            var recoveryEventName = eventName + RECOVERY_SINGLE_INSTANCE_SUFFIX;
            var recoveryAckEventName = ackEventName + RECOVERY_SINGLE_INSTANCE_SUFFIX;
            var recoveryLegacyMutexName = legacyMutexName + RECOVERY_SINGLE_INSTANCE_SUFFIX;
            var recoveryLegacyEventName = legacyEventName + RECOVERY_SINGLE_INSTANCE_SUFFIX;
            var recoveryLegacyAckEventName = legacyAckEventName + RECOVERY_SINGLE_INSTANCE_SUFFIX;

            _primaryMutex = new Mutex(true, recoveryMutexName, out var recoveryIsOwned);
            _primaryMutexOwned = recoveryIsOwned;
            _primarySignal = new EventWaitHandle(false, EventResetMode.AutoReset, recoveryEventName);
            _primaryAck = new EventWaitHandle(false, EventResetMode.AutoReset, recoveryAckEventName);
            _legacyMutex = new Mutex(true, recoveryLegacyMutexName, out var recoveryLegacyIsOwned);
            _legacyMutexOwned = recoveryLegacyIsOwned;
            _legacySignal = new EventWaitHandle(false, EventResetMode.AutoReset, recoveryLegacyEventName);
            _legacyAck = new EventWaitHandle(false, EventResetMode.AutoReset, recoveryLegacyAckEventName);

            if (recoveryIsOwned && recoveryLegacyIsOwned)
                return true;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Recovery single-instance guard is already owned, signaling recovery instance...");

            if (!SignalAndWaitForSingleInstanceActivation() && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Recovery instance did not acknowledge activation.");

            ReleaseAndCloseMutexes();
            DisposeWaitHandles();
            return false;
        }

        private void ReleaseAndCloseMutexes()
        {
            try
            {
                if (_primaryMutexOwned && _primaryMutex != null)
                {
                    void ReleaseMutex()
                    {
                        if (_primaryMutexOwned && _primaryMutex != null)
                        {
                            _primaryMutex.ReleaseMutex();
                            _primaryMutexOwned = false;
                        }
                    }

                    if (_dispatcher.CheckAccess())
                    {
                        ReleaseMutex();
                    }
                    else if (!_dispatcher.HasShutdownStarted && !_dispatcher.HasShutdownFinished)
                    {
                        _dispatcher.Invoke(ReleaseMutex);
                    }
                    else
                    {
                        _primaryMutexOwned = false;
                    }
                }

                _primaryMutex?.Close();
                _primaryMutex = null;

                if (_legacyMutexOwned && _legacyMutex != null)
                {
                    _legacyMutex.ReleaseMutex();
                    _legacyMutexOwned = false;
                }

                _legacyMutex?.Close();
                _legacyMutex = null;
            }
            catch (ApplicationException ex) when (ex.Message.Contains("Object synchronization method", StringComparison.OrdinalIgnoreCase))
            {
                _primaryMutexOwned = false;
                _primaryMutex?.Close();
                _primaryMutex = null;
                _legacyMutexOwned = false;
                _legacyMutex?.Close();
                _legacyMutex = null;

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Single instance mutex was not owned by the current thread; closed without explicit release.");
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error disposing single instance mutex: {ex.Message}", ex);
            }
        }

        private void DisposeWaitHandles()
        {
            try
            {
                _primarySignal?.Dispose();
                _primarySignal = null;
                _legacySignal?.Dispose();
                _legacySignal = null;
                _primaryAck?.Dispose();
                _primaryAck = null;
                _legacyAck?.Dispose();
                _legacyAck = null;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error disposing wait handle: {ex.Message}", ex);
            }
        }

        private static string ResolveSingleInstanceObjectName(string baseName)
        {
#if UDT_TEST_HOOKS
            var isolationKey = ResolveSingleInstanceIsolationKey();
            if (string.IsNullOrWhiteSpace(isolationKey))
                return baseName;

            var sanitizedKey = string.Concat(isolationKey
                .Trim()
                .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_'));

            return string.IsNullOrWhiteSpace(sanitizedKey)
                ? baseName
                : $"{baseName}_{sanitizedKey}";
#else
            return baseName;
#endif
        }

#if UDT_TEST_HOOKS
        private static string? ResolveSingleInstanceIsolationKey()
        {
            var overridePath = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(overridePath))
                return null;

            try
            {
                return Path.GetFullPath(overridePath);
            }
            catch
            {
                return overridePath;
            }
        }
#endif
    }
}

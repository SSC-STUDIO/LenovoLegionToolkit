using System;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Listeners;

public abstract class AbstractWMIListener<TEventArgs, TValue, TRawValue>(Func<Action<TRawValue>, Task<IDisposable>> listen)
    : IListener<TEventArgs>, IDisposable
    where TEventArgs : EventArgs
{
    private IDisposable? _disposable;
    private readonly SemaphoreSlim _eventHandlerLock = new(1, 1);
    private bool _disposed;
    private bool _isUnsupported;

    public event EventHandler<TEventArgs>? Changed;


    public async Task StartAsync()
    {
        if (_isUnsupported)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Listener marked unsupported. Skipping start. [listener={GetType().Name}]");
            return;
        }

        try
        {
            if (_disposable is not null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Already started. [listener={GetType().Name}]");
                return;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Starting... [listener={GetType().Name}]");

            // Await async WMI watcher start (StartAsyncWithTimeout) — does not block UI/sync context.
            _disposable = await listen(Handler).ConfigureAwait(false);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Started successfully. [listener={GetType().Name}]");
        }
        catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidClass || ex.ErrorCode == ManagementStatus.InvalidNamespace)
        {
            _isUnsupported = true;

            Log.Instance.Warning($"WMI class or namespace not available; listener disabled. [listener={GetType().Name}, error={ex.ErrorCode}]");
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Couldn't start listener. [listener={GetType().Name}]", ex);
        }
    }

    public Task StopAsync()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Stopping... [listener={GetType().Name}]");

            _disposable?.Dispose();
            _disposable = null;
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Couldn't stop listener. [listener={GetType().Name}]", ex);
        }

        return Task.CompletedTask;
    }

    protected abstract TValue GetValue(TRawValue value);

    protected abstract TEventArgs GetEventArgs(TValue value);

    protected abstract Task OnChangedAsync(TValue value);

    protected void RaiseChanged(TValue value) => Changed?.Invoke(this, GetEventArgs(value));

    private async Task HandlerAsync(TRawValue properties, CancellationToken cancellationToken = default)
    {
        bool lockAcquired = false;
        try
        {
            await _eventHandlerLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockAcquired = true;

            var value = GetValue(properties);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Event received. [value={value}, listener={GetType().Name}]");

            await OnChangedAsync(value).ConfigureAwait(false);
            RaiseChanged(value);
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Failed to handle event. [listener={GetType().Name}]", ex);
        }
        finally
        {
            if (lockAcquired)
                _eventHandlerLock.Release();
        }
    }

    // Event handler wrapper that properly handles async task
    private void Handler(TRawValue properties)
    {
        HandlerAsync(properties).Forget($"{GetType().Name}.HandlerAsync");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            StopAsync().Forget($"{GetType().Name}.StopAsync");
            Changed = null;
        }

        _disposed = true;
    }
}

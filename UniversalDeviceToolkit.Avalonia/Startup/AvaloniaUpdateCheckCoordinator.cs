#if WINDOWS
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Messaging;
using UniversalDeviceToolkit.Lib.Messaging.Messages;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Startup;

/// <summary>
/// Runs the same frequency-gated background update check that the WPF shell
/// requests when its main window becomes available. Manual checks remain
/// force-refresh operations in the settings page.
/// </summary>
internal sealed class AvaloniaUpdateCheckCoordinator
{
    private readonly Func<Task<Version?>> _checkAsync;
    private readonly Action<NotificationMessage> _publish;
    private readonly Action<Exception> _reportFailure;
    private readonly SemaphoreSlim _checkGate = new(1, 1);

    internal AvaloniaUpdateCheckCoordinator(
        Func<Task<Version?>> checkAsync,
        Action<NotificationMessage> publish,
        Action<Exception> reportFailure)
    {
        _checkAsync = checkAsync;
        _publish = publish;
        _reportFailure = reportFailure;
    }

    internal static AvaloniaUpdateCheckCoordinator? Create()
    {
        var updateChecker = IoCContainer.TryResolve<UpdateChecker>();
        if (updateChecker is null || updateChecker.Disable)
            return null;

        return new AvaloniaUpdateCheckCoordinator(
            () => updateChecker.CheckAsync(forceCheck: false),
            static notification => MessagingCenter.Publish(notification),
            static exception => Log.Instance.Trace("Avalonia automatic update check failed.", exception));
    }

    /// <summary>
    /// Requests a normal, frequency-gated check. Concurrent restore/startup
    /// requests share the same check rather than creating duplicate network work.
    /// </summary>
    internal async Task CheckAsync()
    {
        if (!await _checkGate.WaitAsync(0).ConfigureAwait(false))
            return;

        try
        {
            var version = await _checkAsync().ConfigureAwait(false);
            if (version is not null)
            {
                _publish(new NotificationMessage(
                    NotificationType.UpdateAvailable,
                    version.ToString(3)));
            }
        }
        catch (Exception ex)
        {
            _reportFailure(ex);
        }
        finally
        {
            _checkGate.Release();
        }
    }
}
#endif

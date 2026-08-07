#if WINDOWS
using Avalonia.Controls;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Windows;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Messaging;
using UniversalDeviceToolkit.Lib.Messaging.Messages;
using UniversalDeviceToolkit.Lib.Utils;
#endif

namespace UniversalDeviceToolkit.Avalonia.Startup;

/// <summary>
/// Immutable snapshot of the newest available release. Kept free of shared-lib
/// types so any Avalonia surface (including portable builds) can subscribe to
/// <see cref="AvaloniaUpdateCheckCoordinator.UpdateAvailableChanged"/>.
/// </summary>
public sealed record UpdateReleaseInfo(
    Version Version,
    string TagName,
    bool IsPrerelease,
    string Title,
    string Description,
    DateTimeOffset Date);

#if WINDOWS
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
    private readonly Func<Task<IReadOnlyList<UpdateReleaseInfo>>> _getUpdates;
    private readonly SemaphoreSlim _checkGate = new(1, 1);
    private UpdateReleaseInfo? _latestUpdate;
    private AvaloniaUpdateWindow? _updateWindow;

    /// <summary>
    /// Raised after a check finds a newer release. May fire on a thread-pool
    /// thread; subscribers that touch UI must marshal via the dispatcher.
    /// </summary>
    public event Action<UpdateReleaseInfo>? UpdateAvailableChanged;

    /// <summary>
    /// The coordinator created by the app host, or null when update checks are
    /// disabled. UI surfaces reach the shared instance without owning it.
    /// </summary>
    public static AvaloniaUpdateCheckCoordinator? Current { get; private set; }

    /// <summary>Newest release known from the last successful check, or null.</summary>
    public UpdateReleaseInfo? LatestUpdate => _latestUpdate;

    internal AvaloniaUpdateCheckCoordinator(
        Func<Task<Version?>> checkAsync,
        Action<NotificationMessage> publish,
        Action<Exception> reportFailure,
        Func<Task<IReadOnlyList<UpdateReleaseInfo>>>? getUpdates = null)
    {
        _checkAsync = checkAsync;
        _publish = publish;
        _reportFailure = reportFailure;
        _getUpdates = getUpdates ?? (() => Task.FromResult<IReadOnlyList<UpdateReleaseInfo>>([]));
    }

    internal static AvaloniaUpdateCheckCoordinator? Create()
    {
        var updateChecker = IoCContainer.TryResolve<UpdateChecker>();
        if (updateChecker is null || updateChecker.Disable)
            return null;

        var coordinator = new AvaloniaUpdateCheckCoordinator(
            () => updateChecker.CheckAsync(forceCheck: false),
            static notification => MessagingCenter.Publish(notification),
            static exception => Log.Instance.Trace("Avalonia automatic update check failed.", exception),
            async () =>
            {
                var updates = await updateChecker.GetUpdatesAsync().ConfigureAwait(false);
                return updates
                    .OrderByDescending(update => update.Version)
                    .Select(static update => new UpdateReleaseInfo(
                        update.Version,
                        update.TagName,
                        update.IsPrerelease,
                        update.Title,
                        update.Description,
                        update.Date))
                    .ToArray();
            });
        Current = coordinator;
        return coordinator;
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

                var updates = await _getUpdates().ConfigureAwait(false);
                var latest = updates.OrderByDescending(update => update.Version).FirstOrDefault();
                if (latest is not null)
                {
                    _latestUpdate = latest;
                    UpdateAvailableChanged?.Invoke(latest);
                }
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

    /// <summary>
    /// Opens (or focuses) the update window for the newest available release.
    /// Falls back to a frequency-gated check when no release data is cached yet.
    /// </summary>
    public async Task ShowUpdateAsync(Window owner)
    {
        var latest = _latestUpdate;
        if (latest is null)
        {
            var updates = await _getUpdates().ConfigureAwait(false);
            latest = updates.OrderByDescending(update => update.Version).FirstOrDefault();
        }

        if (latest is null)
        {
            await CheckAsync().ConfigureAwait(false);
            latest = _latestUpdate;
        }

        if (latest is null)
            return;

        if (Dispatcher.UIThread.CheckAccess())
        {
            ShowUpdateWindowCore(latest, owner);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => ShowUpdateWindowCore(latest, owner))
            .GetTask()
            .ConfigureAwait(false);
    }

    private void ShowUpdateWindowCore(UpdateReleaseInfo update, Window owner)
    {
        if (_updateWindow is { IsVisible: true } visible)
        {
            visible.Activate();
            return;
        }

        var window = new AvaloniaUpdateWindow(update);
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_updateWindow, window))
                _updateWindow = null;
        };
        _updateWindow = window;
        window.Show(owner);
    }
}
#endif

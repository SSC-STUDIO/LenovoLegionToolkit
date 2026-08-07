#if WINDOWS

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Automation.Utils;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Lib.Messaging;
using UniversalDeviceToolkit.Lib.Messaging.Messages;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// WPF SmartKeyHelper parity for the Avalonia host: detects single/double Fn+F9
/// presses with a 500ms window, runs the configured automation pipeline and
/// notifies the host through the shared messaging bus.
/// </summary>
internal sealed class AvaloniaSmartKeyHandler : IDisposable
{
    private const double SmartKeyDoublePressWindowMilliseconds = 500;

    private static AvaloniaSmartKeyHandler? _current;

    private readonly ApplicationSettings _settings;
    private readonly FnKeysDisabler _fnKeysDisabler;
    private readonly SpecialKeyListener _specialKeyListener;
    private readonly AutomationProcessor _automationProcessor;

    private DateTime _lastSmartKeyPress = DateTime.MinValue;
    private CancellationTokenSource? _doublePressCancellationTokenSource;
    private bool _disposed;

    private AvaloniaSmartKeyHandler(
        ApplicationSettings settings,
        FnKeysDisabler fnKeysDisabler,
        SpecialKeyListener specialKeyListener,
        AutomationProcessor automationProcessor)
    {
        _settings = settings;
        _fnKeysDisabler = fnKeysDisabler;
        _specialKeyListener = specialKeyListener;
        _automationProcessor = automationProcessor;
        _specialKeyListener.Changed += SpecialKeyListener_Changed;
    }

    public static AvaloniaSmartKeyHandler? Current => _current;

    public static AvaloniaSmartKeyHandler? Start()
    {
        if (_current is not null)
            return _current;

        var settings = IoCContainer.TryResolve<ApplicationSettings>();
        var fnKeysDisabler = IoCContainer.TryResolve<FnKeysDisabler>();
        var specialKeyListener = IoCContainer.TryResolve<SpecialKeyListener>();
        var automationProcessor = IoCContainer.TryResolve<AutomationProcessor>();
        if (settings is null || fnKeysDisabler is null || specialKeyListener is null || automationProcessor is null)
            return null;

        _current = new AvaloniaSmartKeyHandler(settings, fnKeysDisabler, specialKeyListener, automationProcessor);
        return _current;
    }

    public static void Stop()
    {
        _current?.Dispose();
        _current = null;
    }

    private async void SpecialKeyListener_Changed(object? sender, SpecialKeyListener.ChangedEventArgs e)
    {
        try
        {
            if (e.SpecialKey != SpecialKey.FnF9)
                return;

            if (await _fnKeysDisabler.GetStatusAsync().ConfigureAwait(false) == SoftwareStatus.Enabled)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Ignoring Fn+F9, FnKeys are enabled.");

                return;
            }

            _doublePressCancellationTokenSource?.Cancel();
            _doublePressCancellationTokenSource?.Dispose();
            _doublePressCancellationTokenSource = new CancellationTokenSource();

            var token = _doublePressCancellationTokenSource.Token;

            _ = Task.Run(async () =>
            {
                var now = DateTime.UtcNow;
                var isDoublePress = SmartKeyPressClassifier.IsDoublePress(
                    _lastSmartKeyPress,
                    now,
                    SmartKeyDoublePressWindowMilliseconds);
                _lastSmartKeyPress = now;

                if (isDoublePress)
                {
                    await ProcessSpecialKeyAsync(true).ConfigureAwait(false);
                    return;
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(SmartKeyDoublePressWindowMilliseconds), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                await ProcessSpecialKeyAsync(false).ConfigureAwait(false);
            }, token);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(SpecialKeyListener_Changed)}.", ex);
        }
    }

    private async Task ProcessSpecialKeyAsync(bool isDoublePress)
    {
        var store = _settings.Store;
        var currentGuid = isDoublePress
            ? store.SmartKeyDoublePressActionId
            : store.SmartKeySinglePressActionId;
        var actionList = isDoublePress
            ? store.SmartKeyDoublePressActionList
            : store.SmartKeySinglePressActionList;

        if (!currentGuid.HasValue)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Bringing to foreground after {(isDoublePress ? "double" : "single")} Fn+F9 press.");

            BringToForeground();
            return;
        }

        if (currentGuid.Value == Guid.Empty)
            return;

        var (currentAction, nextAction) = SmartKeyActionSelector.Resolve(currentGuid.Value, actionList);
        currentGuid = currentAction;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Running action {currentGuid} after {(isDoublePress ? "double" : "single")} Fn+F9 press.");

        try
        {
            var pipelines = await _automationProcessor.GetPipelinesAsync().ConfigureAwait(false);
            var pipeline = pipelines.FirstOrDefault(p => p.Id == currentGuid);
            if (pipeline is not null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Running action {currentGuid} after {(isDoublePress ? "double" : "single")} Fn+F9 press.");

                await _automationProcessor.RunNowAsync(pipeline.Id).ConfigureAwait(false);

                var displayName = PipelineNameLocalizer.LocalizeStoredName(pipeline.Name) ?? pipeline.Name ?? string.Empty;
                MessagingCenter.Publish(new NotificationMessage(isDoublePress ? NotificationType.SmartKeyDoublePress : NotificationType.SmartKeySinglePress, displayName));
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Running action {currentGuid} after {(isDoublePress ? "double" : "single")} Fn+F9 press failed.", ex);
        }

        if (isDoublePress)
        {
            store.SmartKeyDoublePressActionList = actionList;
            store.SmartKeyDoublePressActionId = nextAction;
        }
        else
        {
            store.SmartKeySinglePressActionList = actionList;
            store.SmartKeySinglePressActionId = nextAction;
        }

        _settings.SynchronizeStore();
    }

    private static void BringToForeground()
    {
        Action restore = () =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is MainWindow mainWindow)
            {
                mainWindow.RestoreFromTray();
            }
        };

        if (Dispatcher.UIThread.CheckAccess())
            restore();
        else
            Dispatcher.UIThread.Post(restore, DispatcherPriority.Normal);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _specialKeyListener.Changed -= SpecialKeyListener_Changed;
        _doublePressCancellationTokenSource?.Cancel();
        _doublePressCancellationTokenSource?.Dispose();
        _doublePressCancellationTokenSource = null;
    }
}

/// <summary>
/// Pure classification of consecutive Fn+F9 presses, kept testable without a clock.
/// </summary>
internal static class SmartKeyPressClassifier
{
    public static bool IsDoublePress(DateTime lastPressUtc, DateTime nowUtc, double windowMilliseconds)
    {
        if (lastPressUtc == DateTime.MinValue)
            return false;

        return (nowUtc - lastPressUtc).TotalMilliseconds < windowMilliseconds;
    }
}

/// <summary>
/// Pure rotation over the stored action list, kept testable without a clock.
/// </summary>
internal static class SmartKeyActionSelector
{
    public static (Guid Current, Guid Next) Resolve(Guid currentId, IList<Guid> actionList)
    {
        if (actionList.IsEmpty())
            actionList.Add(currentId);

        var currentIndex = Math.Max(0, actionList.IndexOf(currentId));
        var nextIndex = (currentIndex + 1) % actionList.Count;
        return (actionList[currentIndex], actionList[nextIndex]);
    }
}

#endif

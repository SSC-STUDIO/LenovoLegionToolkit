using System;
using System.Threading;
#nullable enable

using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;
using Microsoft.Win32;

namespace LenovoLegionToolkit.Plugins.CustomMouse;

public sealed class ThemeWatcherRuntime
{
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(500);

    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private Timer? _debounceTimer;
    private string? _lastAppliedTheme;

    public event Func<string, CancellationToken, Task>? ThemeChanged;

    public void Start(string? initialLastAppliedTheme)
    {
        lock (_gate)
        {
            if (_cts != null)
                return;

            _lastAppliedTheme = initialLastAppliedTheme;
            _cts = new CancellationTokenSource();
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        Timer? timer;

        lock (_gate)
        {
            cts = _cts;
            timer = _debounceTimer;
            _cts = null;
            _debounceTimer = null;
        }

        if (cts == null)
            return;

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;

        try { cts.Cancel(); }
        catch (ObjectDisposedException) { /* ignore — already disposed */ }
        finally
        {
            timer?.Dispose();
            cts.Dispose();
        }
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General)
            return;

        lock (_gate)
        {
            if (_cts == null || _cts.IsCancellationRequested)
                return;

            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(
                OnDebounceElapsed,
                null,
                DebounceInterval,
                Timeout.InfiniteTimeSpan);
        }
    }

    private void OnDebounceElapsed(object? state)
    {
        CancellationToken token;

        lock (_gate)
        {
            if (_cts == null || _cts.IsCancellationRequested)
                return;

            token = _cts.Token;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var currentTheme = IsSystemLightTheme() ? "light" : "dark";

                lock (_gate)
                {
                    if (string.Equals(currentTheme, _lastAppliedTheme, StringComparison.OrdinalIgnoreCase))
                        return;

                    _lastAppliedTheme = currentTheme;
                }

                if (ThemeChanged != null)
                    await ThemeChanged.Invoke(currentTheme, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) when (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"ThemeWatcher: error watching theme changes: {ex.Message}", ex);
            }
        }, token);
    }

    private static bool IsSystemLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue ? intValue != 0 : true;
        }
        catch
        {
            return true;
        }
    }
}

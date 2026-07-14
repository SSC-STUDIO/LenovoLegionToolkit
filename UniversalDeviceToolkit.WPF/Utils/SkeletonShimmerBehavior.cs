using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Wpf.Ui.Appearance;

namespace UniversalDeviceToolkit.WPF.Utils;

internal static class SkeletonShimmerBehavior
{
    private sealed class ElementState
    {
        internal DispatcherOperation? PendingStart;
        internal Brush? OriginalBackground;
        internal bool HadLocalBackground;
        internal bool IsRunning;
    }

    private static readonly ConditionalWeakTable<Border, ElementState> States = new();
    private static readonly List<WeakReference<Border>> ActiveBorders = [];
    private static bool _themeEventsAttached;

    internal static void Attach(Border border)
    {
        EnsureThemeEventsAttached();
        border.Loaded -= OnLoaded;
        border.Unloaded -= OnUnloaded;
        border.IsVisibleChanged -= OnIsVisibleChanged;
        border.Loaded += OnLoaded;
        border.Unloaded += OnUnloaded;
        border.IsVisibleChanged += OnIsVisibleChanged;

        if (border.IsLoaded && border.IsVisible)
            QueueStart(border, null);
    }

    internal static void Detach(Border border)
    {
        border.Loaded -= OnLoaded;
        border.Unloaded -= OnUnloaded;
        border.IsVisibleChanged -= OnIsVisibleChanged;
        Stop(border);
        States.Remove(border);
    }

    internal static void QueueStart(Border border, TimeSpan? delay)
    {
        if (!border.Dispatcher.CheckAccess())
        {
            _ = border.Dispatcher.BeginInvoke(() => QueueStart(border, delay), DispatcherPriority.Render);
            return;
        }

        var state = States.GetOrCreateValue(border);
        CancelPendingStart(state);
        if (border.Dispatcher.HasShutdownStarted || border.Dispatcher.HasShutdownFinished)
            return;

        DispatcherOperation? operation = null;
        operation = border.Dispatcher.BeginInvoke(() =>
        {
            if (operation is not null && ReferenceEquals(state.PendingStart, operation))
                state.PendingStart = null;

            if (border.IsLoaded && border.IsVisible && SkeletonShimmer.GetIsEnabled(border))
                Start(border, delay);
        }, DispatcherPriority.Render);
        state.PendingStart = operation;
    }

    internal static void Start(Border border, TimeSpan? automaticDelay, bool forceRestart = false)
    {
        if (!border.Dispatcher.CheckAccess())
        {
            _ = border.Dispatcher.BeginInvoke(() => Start(border, automaticDelay, forceRestart), DispatcherPriority.Render);
            return;
        }

        var state = States.GetOrCreateValue(border);
        CancelPendingStart(state);

        // Soft path: keep the existing forever-sweep. Tear-down/rebuild is what made plugin
        // skeleton feel heavy when ShowSkeletonImmediate re-entered every navigation tick.
        if (state.IsRunning
            && !forceRestart
            && border.Background is LinearGradientBrush { RelativeTransform: TranslateTransform })
        {
            TrackActive(border);
            return;
        }

        StopAnimation(border, state, restoreBackground: true);

        var duration = SkeletonShimmer.ResolveDuration(border);
        if (IsMotionDisabled(duration))
            return;

        CaptureBackground(border, state);
        var baseColor = SkeletonShimmer.ResolveBaseColor(border);
        var (shimmerStart, shimmerPeak) = SkeletonShimmer.ResolveShimmerOverlayColors(border);
        var brush = SkeletonShimmer.CreateShimmerBrush(baseColor, shimmerStart, shimmerPeak);
        border.Background = brush;
        border.Opacity = 1;

        if (brush.RelativeTransform is not TranslateTransform transform)
            return;

        var configuredDelay = SkeletonShimmer.GetDelaySeconds(border);
        var beginTime = configuredDelay >= 0
            ? TimeSpan.FromSeconds(Math.Min(configuredDelay, SkeletonAnimationTokens.StaggerMaxSeconds))
            : automaticDelay ?? TimeSpan.Zero;
        // Sine ease (classic 4.x feel) — cheaper/smoother than cubic for endless loops.
        var animation = new DoubleAnimation
        {
            From = SkeletonAnimationTokens.SweepFrom,
            To = SkeletonAnimationTokens.SweepTo,
            Duration = duration,
            BeginTime = beginTime,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        transform.BeginAnimation(TranslateTransform.XProperty, animation, HandoffBehavior.SnapshotAndReplace);
        state.IsRunning = true;
        TrackActive(border);
    }

    internal static void Stop(Border border)
    {
        if (!border.Dispatcher.CheckAccess())
        {
            _ = border.Dispatcher.BeginInvoke(() => Stop(border), DispatcherPriority.Send);
            return;
        }

        if (!States.TryGetValue(border, out var state))
            return;

        CancelPendingStart(state);
        StopAnimation(border, state, restoreBackground: true);
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Border border)
            QueueStart(border, null);
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is Border border)
            Stop(border);
    }

    private static void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not Border border)
            return;

        if (e.NewValue is true)
            QueueStart(border, null);
        else
            Stop(border);
    }

    private static bool IsMotionDisabled(Duration duration) =>
        ApplicationThemeManager.IsHighContrast()
        || SystemParameters.HighContrast
        || !duration.HasTimeSpan
        || duration.TimeSpan <= TimeSpan.Zero;

    private static void CaptureBackground(Border border, ElementState state)
    {
        // Never treat an in-flight shimmer gradient as the "original" solid — that freezes a
        // half-translated band as the static bone fill after Stop/Restart.
        if (border.Background is LinearGradientBrush)
        {
            state.HadLocalBackground = true;
            state.OriginalBackground = new SolidColorBrush(SkeletonShimmer.ResolveBaseColor(border));
            return;
        }

        state.HadLocalBackground = border.ReadLocalValue(Border.BackgroundProperty) != DependencyProperty.UnsetValue;
        state.OriginalBackground = border.Background;
    }

    private static void StopAnimation(Border border, ElementState state, bool restoreBackground)
    {
        if (border.Background is LinearGradientBrush { RelativeTransform: TranslateTransform transform })
            transform.BeginAnimation(TranslateTransform.XProperty, null);

        border.BeginAnimation(UIElement.OpacityProperty, null);
        border.Opacity = 1;

        if (restoreBackground && (state.IsRunning || state.OriginalBackground is not null || border.Background is LinearGradientBrush))
        {
            if (state.HadLocalBackground && state.OriginalBackground is not null and not LinearGradientBrush)
                border.Background = state.OriginalBackground;
            else if (border.Background is LinearGradientBrush)
                border.Background = new SolidColorBrush(SkeletonShimmer.ResolveBaseColor(border));
            else if (!state.HadLocalBackground)
                border.ClearValue(Border.BackgroundProperty);
            else if (state.OriginalBackground is not null)
                border.Background = state.OriginalBackground;
        }

        state.OriginalBackground = null;
        state.HadLocalBackground = false;
        state.IsRunning = false;
    }

    private static void CancelPendingStart(ElementState state)
    {
        var operation = state.PendingStart;
        state.PendingStart = null;
        if (operation?.Status == DispatcherOperationStatus.Pending)
            operation.Abort();
    }

    private static void EnsureThemeEventsAttached()
    {
        if (_themeEventsAttached)
            return;

        _themeEventsAttached = true;
        ApplicationThemeManager.Changed += (_, _) => RefreshActiveBorders();
        SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
    }

    private static void OnSystemParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(SystemParameters.HighContrast))
            RefreshActiveBorders();
    }

    private static void RefreshActiveBorders()
    {
        var applicationDispatcher = Application.Current?.Dispatcher;
        if (applicationDispatcher is not null && !applicationDispatcher.CheckAccess())
        {
            _ = applicationDispatcher.BeginInvoke(RefreshActiveBorders, DispatcherPriority.Render);
            return;
        }

        for (var index = ActiveBorders.Count - 1; index >= 0; index--)
        {
            if (!ActiveBorders[index].TryGetTarget(out var border))
            {
                ActiveBorders.RemoveAt(index);
                continue;
            }

            if (!border.IsLoaded || !border.IsVisible || !SkeletonShimmer.GetIsEnabled(border))
            {
                Stop(border);
                ActiveBorders.RemoveAt(index);
                continue;
            }

            // Theme/contrast change must rebuild brushes — force restart.
            Start(border, automaticDelay: null, forceRestart: true);
        }
    }

    private static void TrackActive(Border border)
    {
        foreach (var weakReference in ActiveBorders)
        {
            if (weakReference.TryGetTarget(out var existing) && ReferenceEquals(existing, border))
                return;
        }

        ActiveBorders.Add(new WeakReference<Border>(border));
    }
}

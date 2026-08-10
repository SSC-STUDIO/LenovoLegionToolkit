using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Utils;

internal static class SkeletonShimmerBehavior
{
    private sealed class ElementState
    {
        internal bool PendingStart;
        internal IBrush? OriginalBackground;
        internal bool HadLocalBackground;
        internal bool IsRunning;

        // Avalonia animation bookkeeping (kept alive while the shimmer runs).
        internal CancellationTokenSource? Cancellation;
        internal Animation? SweepAnimation;
        internal Animation? BreathingAnimation;
        internal TranslateTransform? SweepTransform;
        internal EventHandler? TransformChanged;
        internal DispatcherTimer? SweepTimer;
        internal DispatcherTimer? BreathingTimer;
    }

    private static readonly ConditionalWeakTable<Border, ElementState> States = new();
    private static readonly List<WeakReference<Border>> ActiveBorders = [];
    private static bool _themeEventsAttached;
    private static bool _themeRefreshQueued;

    internal static void Attach(Border border)
    {
        EnsureThemeEventsAttached();
        border.Loaded -= OnLoaded;
        border.Unloaded -= OnUnloaded;
        border.PropertyChanged -= OnIsVisibleChanged;
        border.Loaded += OnLoaded;
        border.Unloaded += OnUnloaded;
        border.PropertyChanged += OnIsVisibleChanged;

        if (border.IsLoaded && border.IsVisible)
            QueueStart(border, null);
    }

    internal static void Detach(Border border)
    {
        border.Loaded -= OnLoaded;
        border.Unloaded -= OnUnloaded;
        border.PropertyChanged -= OnIsVisibleChanged;
        Stop(border);
        States.Remove(border);
    }

    internal static void QueueStart(Border border, TimeSpan? delay)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => QueueStart(border, delay));
            return;
        }

        var state = States.GetOrCreateValue(border);
        CancelPendingStart(state);

        state.PendingStart = true;
        Dispatcher.UIThread.Post(() =>
        {
            if (!state.PendingStart)
                return;
            state.PendingStart = false;

            if (border.IsLoaded && border.IsVisible && SkeletonShimmer.GetIsEnabled(border))
                Start(border, delay);
        });
    }

    internal static void Start(Border border, TimeSpan? automaticDelay, bool forceRestart = false)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Start(border, automaticDelay, forceRestart));
            return;
        }

        var state = States.GetOrCreateValue(border);
        CancelPendingStart(state);

        // Soft path: keep the existing forever-sweep. Tear-down/rebuild is what made plugin
        // skeleton feel heavy when ShowSkeletonImmediate re-entered every navigation tick.
        if (state.IsRunning
            && !forceRestart
            && border.Background is LinearGradientBrush { Transform: TranslateTransform })
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
        var (shimmerStart, shimmerPeak) = SkeletonShimmer.ResolveShimmerOverlayColors(baseColor);
        var brush = SkeletonShimmer.CreateShimmerBrush(baseColor, shimmerStart, shimmerPeak);
        border.Background = brush;
        border.Opacity = 1;

        if (brush.Transform is not TranslateTransform transform)
            return;

        var configuredDelay = SkeletonShimmer.GetDelaySeconds(border);
        var beginTime = configuredDelay >= 0
            ? TimeSpan.FromSeconds(Math.Min(configuredDelay, SkeletonAnimationTokens.StaggerMaxSeconds))
            : automaticDelay ?? TimeSpan.Zero;

        // Sine ease (classic 4.x feel) — cheaper/smoother than cubic for endless loops.
        var easing = new SineEaseInOut();

        var sweepAnimation = new Animation
        {
            Duration = duration,
            Delay = beginTime,
            IterationCount = IterationCount.Infinite,
            FillMode = FillMode.Both,
            Easing = easing,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters = { new Setter(TranslateTransform.XProperty, SkeletonAnimationTokens.SweepFrom) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters = { new Setter(TranslateTransform.XProperty, SkeletonAnimationTokens.SweepTo) }
                }
            }
        };

        var breathingAnimation = new Animation
        {
            Duration = duration,
            Delay = beginTime,
            IterationCount = IterationCount.Infinite,
            PlaybackDirection = PlaybackDirection.Alternate,
            FillMode = FillMode.Both,
            Easing = easing,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters = { new Setter(Visual.OpacityProperty, SkeletonAnimationTokens.BreathingFloorOpacity) }
                }
            }
        };

        var cts = new CancellationTokenSource();
        state.Cancellation = cts;
        state.SweepAnimation = sweepAnimation;
        state.BreathingAnimation = breathingAnimation;
        state.SweepTransform = transform;

        // Avalonia brushes are compositor resources that do not observe mutations of their
        // Transform value. Each animated tick re-assigns a fresh transform instance so the
        // brush re-serializes and the border repaints with the new sweep offset.
        EventHandler transformChanged = (_, _) => PushSweep(border, brush, transform);
        transform.Changed += transformChanged;
        state.TransformChanged = transformChanged;

        state.SweepTimer = CreateSweepTimer(sweepAnimation, transform, cts.Token);
        state.BreathingTimer = CreateBreathingTimer(breathingAnimation, border, cts.Token);

        state.IsRunning = true;
        TrackActive(border);
    }

    /// <summary>
    /// Scales the animated relative sweep offset (-1.25..1.25) to the border's current
    /// width and pushes it into the brush (see Start).
    /// </summary>
    private static void PushSweep(Border border, LinearGradientBrush brush, TranslateTransform transform)
    {
        try
        {
            var width = border.Bounds.Width > 0 ? border.Bounds.Width : 1;
            brush.Transform = new TranslateTransform(transform.X * width, 0);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to push skeleton shimmer sweep offset.", ex);
        }
    }

    internal static void Stop(Border border)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Stop(border));
            return;
        }

        if (!States.TryGetValue(border, out var state))
            return;

        CancelPendingStart(state);
        StopAnimation(border, state, restoreBackground: true);
    }

    private static void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border)
            QueueStart(border, null);
    }

    private static void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border)
            Stop(border);
    }

    private static void OnIsVisibleChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not Border border || e.Property != Visual.IsVisibleProperty)
            return;

        if (e.NewValue is true)
            QueueStart(border, null);
        else
            Stop(border);
    }

    private static bool IsMotionDisabled(TimeSpan duration) =>
        duration <= TimeSpan.Zero;

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

        state.HadLocalBackground = border.IsSet(Border.BackgroundProperty);
        state.OriginalBackground = border.Background;
    }

    private static void StopAnimation(Border border, ElementState state, bool restoreBackground)
    {
        state.SweepTimer?.Stop();
        state.SweepTimer = null;
        state.BreathingTimer?.Stop();
        state.BreathingTimer = null;

        if (state.Cancellation is { } cts)
        {
            cts.Cancel();
            cts.Dispose();
            state.Cancellation = null;
        }

        if (state.TransformChanged is { } transformChanged && state.SweepTransform is { } sweepTransform)
        {
            sweepTransform.Changed -= transformChanged;
            state.TransformChanged = null;
            state.SweepTransform = null;
        }

        state.SweepAnimation = null;
        state.BreathingAnimation = null;

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
        state.PendingStart = false;
    }

    private static void EnsureThemeEventsAttached()
    {
        if (_themeEventsAttached)
            return;

        _themeEventsAttached = true;

        if (Application.Current is { } application)
            application.ActualThemeVariantChanged += (_, _) => RefreshActiveBorders();
    }

    private static void RefreshActiveBorders()
    {
        var applicationDispatcher = Dispatcher.UIThread;
        if (!applicationDispatcher.CheckAccess())
        {
            applicationDispatcher.Post(RefreshActiveBorders);
            return;
        }

        // Theme notifications can arrive before DynamicResource values have settled.
        // Coalesce them and rebuild after the resource dictionaries have been refreshed.
        if (_themeRefreshQueued)
            return;

        _themeRefreshQueued = true;
        applicationDispatcher.Post(() =>
        {
            _themeRefreshQueued = false;
            RefreshActiveBordersCore();
        });
    }

    private static void RefreshActiveBordersCore()
    {
        var applicationDispatcher = Dispatcher.UIThread;
        if (!applicationDispatcher.CheckAccess())
        {
            applicationDispatcher.Post(RefreshActiveBordersCore);
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

    /// <summary>
    /// AVALONIA: <c>Animation.RunAsync</c> faults for infinite iteration counts
    /// ("Looping animations must not use the Run method."), so the shimmer loops are
    /// driven manually with DispatcherTimers instead of animations.
    /// </summary>
    private static DispatcherTimer CreateSweepTimer(Animation animation, TranslateTransform transform, CancellationToken token)
    {
        var durationMs = Math.Max(16, (int)animation.Duration.TotalMilliseconds);
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var from = SkeletonAnimationTokens.SweepFrom;
        var to = SkeletonAnimationTokens.SweepTo;
        timer.Tick += (_, _) =>
        {
            if (token.IsCancellationRequested)
            {
                timer.Stop();
                return;
            }
            var phase = (stopwatch.ElapsedMilliseconds % (durationMs * 2)) / (double)(durationMs * 2);
            transform.X = phase < 0.5
                ? from + (to - from) * (phase * 2)
                : to - (to - from) * ((phase - 0.5) * 2);
        };
        timer.Start();
        return timer;
    }

    private static DispatcherTimer CreateBreathingTimer(Animation animation, Border border, CancellationToken token)
    {
        var durationMs = Math.Max(16, (int)animation.Duration.TotalMilliseconds);
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var floor = SkeletonAnimationTokens.BreathingFloorOpacity;
        timer.Tick += (_, _) =>
        {
            if (token.IsCancellationRequested)
            {
                timer.Stop();
                return;
            }
            var phase = (stopwatch.ElapsedMilliseconds % (durationMs * 2)) / (double)(durationMs * 2);
            border.Opacity = phase < 0.5
                ? 1.0 - (1.0 - floor) * (phase * 2)
                : floor + (1.0 - floor) * ((phase - 0.5) * 2);
        };
        timer.Start();
        return timer;
    }
}


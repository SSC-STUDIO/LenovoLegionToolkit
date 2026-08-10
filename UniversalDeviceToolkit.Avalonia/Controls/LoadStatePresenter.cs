using UniversalDeviceToolkit.Avalonia.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Controls.Custom;
using UniversalDeviceToolkit.Avalonia.Controls.Loading;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Controls;

public class LoadStatePresenter : UserControl, IDisposable
{
    private static readonly TimeSpan DefaultLoadingDisplayDelay = TimeSpan.FromMilliseconds(120);
    private static readonly TimeSpan DefaultMinimumLoadingDisplayTime = TimeSpan.FromMilliseconds(220);
    private static readonly TimeSpan DefaultLongRunningThreshold = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan DefaultCrossfadeDuration = TimeSpan.FromMilliseconds(160);

    public static readonly StyledProperty<LoadState> LoadStateProperty =
        AvaloniaProperty.Register<LoadStatePresenter, LoadState>(nameof(LoadState), LoadState.Loading);

    public static readonly StyledProperty<object?> LoadingContentProperty =
        AvaloniaProperty.Register<LoadStatePresenter, object?>(nameof(LoadingContent), null);

    public static readonly StyledProperty<object?> EmptyContentProperty =
        AvaloniaProperty.Register<LoadStatePresenter, object?>(nameof(EmptyContent), null);

    public static readonly StyledProperty<object?> ErrorContentProperty =
        AvaloniaProperty.Register<LoadStatePresenter, object?>(nameof(ErrorContent), null);

    public static readonly StyledProperty<object?> LongRunningContentProperty =
        AvaloniaProperty.Register<LoadStatePresenter, object?>(nameof(LongRunningContent), null);

    public static readonly StyledProperty<bool> ContentVisibilityWhileLoadingProperty =
        AvaloniaProperty.Register<LoadStatePresenter, bool>(nameof(ContentVisibilityWhileLoading), true);

    public static readonly StyledProperty<TimeSpan> LoadingDisplayDelayProperty =
        AvaloniaProperty.Register<LoadStatePresenter, TimeSpan>(nameof(LoadingDisplayDelay), DefaultLoadingDisplayDelay);

    public static readonly StyledProperty<TimeSpan> MinimumLoadingDisplayTimeProperty =
        AvaloniaProperty.Register<LoadStatePresenter, TimeSpan>(nameof(MinimumLoadingDisplayTime), DefaultMinimumLoadingDisplayTime);

    public static readonly StyledProperty<TimeSpan> LongRunningThresholdProperty =
        AvaloniaProperty.Register<LoadStatePresenter, TimeSpan>(nameof(LongRunningThreshold), DefaultLongRunningThreshold);

    public static readonly StyledProperty<TimeSpan> CrossfadeDurationProperty =
        AvaloniaProperty.Register<LoadStatePresenter, TimeSpan>(nameof(CrossfadeDuration), DefaultCrossfadeDuration);

    static LoadStatePresenter()
    {
        LoadStateProperty.Changed.AddClassHandler<LoadStatePresenter>((presenter, _) => presenter.StartTransition());
        ContentVisibilityWhileLoadingProperty.Changed.AddClassHandler<LoadStatePresenter>((presenter, _) => presenter.StartTransition());
        LoadingContentProperty.Changed.AddClassHandler<LoadStatePresenter>(OnSlotContentChanged);
        EmptyContentProperty.Changed.AddClassHandler<LoadStatePresenter>(OnSlotContentChanged);
        ErrorContentProperty.Changed.AddClassHandler<LoadStatePresenter>(OnSlotContentChanged);
        LongRunningContentProperty.Changed.AddClassHandler<LoadStatePresenter>(OnSlotContentChanged);
    }

    private readonly ContentPresenter _contentPresenter = new();
    private readonly ContentPresenter _loadingPresenter = new();
    private readonly ContentPresenter _emptyPresenter = new();
    private readonly ContentPresenter _errorPresenter = new();
    private readonly ContentPresenter _longRunningPresenter = new();
    private readonly ProgressRing _progressRing = new();
    private readonly LoadStateCoordinator _sessionCoordinator = new();

    private CancellationTokenSource? _transitionCancellationTokenSource;
    private DateTime _loadingShownAtUtc = DateTime.MinValue;
    private long _transitionGeneration;
    private bool _isPresenterInitialized;
    private bool _disposed;

    // Crossfade tickers replacing WPF Storyboard/DoubleAnimation.
    private readonly List<DispatcherTimer> _activeFadeTimers = new();

    public LoadState LoadState
    {
        get => (LoadState)GetValue(LoadStateProperty);
        set => SetValue(LoadStateProperty, value);
    }

    public object? LoadingContent
    {
        get => GetValue(LoadingContentProperty);
        set => SetValue(LoadingContentProperty, value);
    }

    public object? EmptyContent
    {
        get => GetValue(EmptyContentProperty);
        set => SetValue(EmptyContentProperty, value);
    }

    public object? ErrorContent
    {
        get => GetValue(ErrorContentProperty);
        set => SetValue(ErrorContentProperty, value);
    }

    public object? LongRunningContent
    {
        get => GetValue(LongRunningContentProperty);
        set => SetValue(LongRunningContentProperty, value);
    }

    public bool ContentVisibilityWhileLoading
    {
        get => (bool)GetValue(ContentVisibilityWhileLoadingProperty);
        set => SetValue(ContentVisibilityWhileLoadingProperty, value);
    }

    public TimeSpan LoadingDisplayDelay
    {
        get => (TimeSpan)GetValue(LoadingDisplayDelayProperty);
        set => SetValue(LoadingDisplayDelayProperty, value);
    }

    public TimeSpan MinimumLoadingDisplayTime
    {
        get => (TimeSpan)GetValue(MinimumLoadingDisplayTimeProperty);
        set => SetValue(MinimumLoadingDisplayTimeProperty, value);
    }

    public TimeSpan LongRunningThreshold
    {
        get => (TimeSpan)GetValue(LongRunningThresholdProperty);
        set => SetValue(LongRunningThresholdProperty, value);
    }

    public TimeSpan CrossfadeDuration
    {
        get => (TimeSpan)GetValue(CrossfadeDurationProperty);
        set => SetValue(CrossfadeDurationProperty, value);
    }

    protected ProgressRing LoadingProgressRing => _progressRing;

    public LoadSession BeginLoadingSession(bool refreshing = false)
    {
        var session = _sessionCoordinator.BeginSession();
        SetLoadStateOnDispatcher(refreshing ? LoadState.Refreshing : LoadState.Loading);
        return session;
    }

    public bool TrySetLoadState(LoadSession session, LoadState state)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!session.IsCurrent)
            return false;

        SetLoadStateOnDispatcher(state);
        return true;
    }

    public void CancelLoadingSession() => _sessionCoordinator.CancelCurrent();

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _contentPresenter.Content = Content;
        // AVALONIA: removed SetResourceReference(StyleProperty, "AppLoadingRingStyle") —
        // the resource is an empty style and Avalonia controls have no Style property.
        UpdateSlotContents();

        var root = new Grid();
        root.Children.Add(_contentPresenter);
        root.Children.Add(_loadingPresenter);
        root.Children.Add(_emptyPresenter);
        root.Children.Add(_errorPresenter);
        root.Children.Add(_longRunningPresenter);
        Content = root;

        _isPresenterInitialized = true;
        Loaded += OnPresenterLoaded;
        Unloaded += OnPresenterUnloaded;
        ApplyImmediateBaseState();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Loaded -= OnPresenterLoaded;
        Unloaded -= OnPresenterUnloaded;
        CancelTransition();
        ResetAnimations();
        _sessionCoordinator.Dispose();
        StopSkeletons();
    }

    private static void OnSlotContentChanged(LoadStatePresenter presenter, AvaloniaPropertyChangedEventArgs e)
    {
        if (presenter._isPresenterInitialized)
            presenter.UpdateSlotContents();
    }

    private void OnPresenterLoaded(object? sender, RoutedEventArgs e) => StartTransition();

    private void OnPresenterUnloaded(object? sender, RoutedEventArgs e)
    {
        CancelTransition();
        StopSkeletons();
    }

    private void UpdateSlotContents()
    {
        _loadingPresenter.Content = LoadingContent ?? _progressRing;
        _emptyPresenter.Content = EmptyContent;
        _errorPresenter.Content = ErrorContent;
        _longRunningPresenter.Content = LongRunningContent;
    }

    private void StartTransition()
    {
        if (!_isPresenterInitialized || _disposed || !IsLoaded)
            return;

        CancelTransition();
        var cancellationTokenSource = new CancellationTokenSource();
        _transitionCancellationTokenSource = cancellationTokenSource;
        var generation = Interlocked.Increment(ref _transitionGeneration);
        _ = ApplyStateAsync(LoadState, generation, cancellationTokenSource.Token);
    }

    private async Task ApplyStateAsync(LoadState state, long generation, CancellationToken cancellationToken)
    {
        try
        {
            switch (state)
            {
                case LoadState.Loading:
                    PrepareForLoading();
                    _ = ShowLongRunningAfterAsync(generation, cancellationToken, false);
                    await DelayAsync(LoadingDisplayDelay, cancellationToken);
                    if (IsCurrent(generation, cancellationToken))
                        ShowLoading();
                    break;
                case LoadState.Refreshing:
                    ShowRefreshingContent();
                    _ = ShowLongRunningAfterAsync(generation, cancellationToken, true);
                    break;
                case LoadState.LongRunning:
                    ResetAnimations();
                    Show(_longRunningPresenter, true);
                    Hide(_loadingPresenter);
                    Hide(_emptyPresenter);
                    Hide(_errorPresenter);
                    _contentPresenter.IsVisible = ContentVisibilityWhileLoading;
                    _contentPresenter.IsHitTestVisible = false;
                    StopSkeletons();
                    break;
                case LoadState.Ready:
                case LoadState.Idle:
                    await ShowTerminalStateAsync(_contentPresenter, generation, cancellationToken);
                    break;
                case LoadState.Empty:
                    await ShowTerminalStateAsync(_emptyPresenter, generation, cancellationToken);
                    break;
                case LoadState.Error:
                    await ShowTerminalStateAsync(_errorPresenter, generation, cancellationToken);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void PrepareForLoading()
    {
        ResetAnimations();
        Hide(_loadingPresenter);
        Hide(_emptyPresenter);
        Hide(_errorPresenter);
        Hide(_longRunningPresenter);
        _contentPresenter.IsVisible = ContentVisibilityWhileLoading;
        _contentPresenter.IsHitTestVisible = false;
        StopSkeletons();
    }

    private void ShowLoading()
    {
        _loadingShownAtUtc = DateTime.UtcNow;
        Show(_loadingPresenter, true);
        SkeletonShimmer.RestartSubtree(_loadingPresenter);
    }

    private void ShowRefreshingContent()
    {
        ResetAnimations();
        Show(_contentPresenter, true);
        Hide(_loadingPresenter);
        Hide(_emptyPresenter);
        Hide(_errorPresenter);
        Hide(_longRunningPresenter);
        StopSkeletons();
    }

    private async Task ShowLongRunningAfterAsync(long generation, CancellationToken cancellationToken, bool refreshing)
    {
        try
        {
            await DelayAsync(LongRunningThreshold, cancellationToken);
            if (!IsCurrent(generation, cancellationToken) || LongRunningContent is null)
                return;

            if (!refreshing)
            {
                Hide(_loadingPresenter);
                SkeletonShimmer.StopSubtree(_loadingPresenter);
            }

            Show(_longRunningPresenter, isInteractive: !refreshing);
            SkeletonShimmer.RestartSubtree(_longRunningPresenter);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ShowTerminalStateAsync(ContentPresenter target, long generation, CancellationToken cancellationToken)
    {
        await DelayAsync(GetMinimumVisibleRemaining(), cancellationToken);
        if (!IsCurrent(generation, cancellationToken))
            return;

        _loadingShownAtUtc = DateTime.MinValue;
        StopSkeletons();
        CrossfadeTo(target, generation);
    }

    private void CrossfadeTo(ContentPresenter target, long generation)
    {
        ResetAnimations();
        var outgoing = new List<ContentPresenter>();
        foreach (var presenter in GetPresenters())
        {
            if (!ReferenceEquals(presenter, target) && presenter.IsVisible)
                outgoing.Add(presenter);
        }

        var duration = ResolveCrossfadeDuration();
        if (duration <= TimeSpan.Zero)
        {
            ApplySynchronously(target);
            return;
        }

        Show(target, true);
        target.Opacity = 0;

        foreach (var presenter in outgoing)
        {
            presenter.IsHitTestVisible = false;
            AnimateOpacity(presenter, presenter.Opacity, 0, duration, () =>
            {
                if (generation != _transitionGeneration)
                    return;
                Hide(presenter);
            });
        }

        AnimateOpacity(target, 0, 1, duration, () =>
        {
            if (generation != _transitionGeneration)
                return;
            target.Opacity = 1;
        });
    }

    private void ApplySynchronously(ContentPresenter target)
    {
        foreach (var presenter in GetPresenters())
        {
            if (ReferenceEquals(presenter, target))
                Show(presenter, true);
            else
                Hide(presenter);
        }
    }

    private void ApplyImmediateBaseState()
    {
        switch (LoadState)
        {
            case LoadState.Loading:
                PrepareForLoading();
                break;
            case LoadState.Refreshing:
            case LoadState.Ready:
            case LoadState.Idle:
                ApplySynchronously(_contentPresenter);
                break;
            case LoadState.LongRunning:
                ApplySynchronously(_longRunningPresenter);
                break;
            case LoadState.Empty:
                ApplySynchronously(_emptyPresenter);
                break;
            case LoadState.Error:
                ApplySynchronously(_errorPresenter);
                break;
        }
    }

    private TimeSpan ResolveCrossfadeDuration()
    {
        if (this.TryFindResource("AnimationDurationSkeletonCrossfade") is TimeSpan configured && configured <= TimeSpan.Zero)
            return TimeSpan.Zero;

        return CrossfadeDuration;
    }

    private TimeSpan GetMinimumVisibleRemaining()
    {
        if (_loadingShownAtUtc == DateTime.MinValue)
            return TimeSpan.Zero;

        var remaining = MinimumLoadingDisplayTime - (DateTime.UtcNow - _loadingShownAtUtc);
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private bool IsCurrent(long generation, CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested && generation == _transitionGeneration && IsLoaded;

    private static Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        delay <= TimeSpan.Zero ? Task.CompletedTask : Task.Delay(delay, cancellationToken);

    private void SetLoadStateOnDispatcher(LoadState state)
    {
        if (Dispatcher.UIThread.CheckAccess())
            SetCurrentValue(LoadStateProperty, state);
        else
            Dispatcher.UIThread.Invoke(() => SetCurrentValue(LoadStateProperty, state));
    }

    private void CancelTransition()
    {
        var cancellationTokenSource = Interlocked.Exchange(ref _transitionCancellationTokenSource, null);
        if (cancellationTokenSource is null)
            return;

        try
        {
            cancellationTokenSource.Cancel();
        }
        finally
        {
            cancellationTokenSource.Dispose();
        }
    }

    private void ResetAnimations()
    {
        foreach (var timer in _activeFadeTimers)
            timer.Stop();
        _activeFadeTimers.Clear();

        foreach (var presenter in GetPresenters())
            presenter.Opacity = 1;
    }

    private void AnimateOpacity(ContentPresenter target, double from, double to, TimeSpan duration, Action? onCompleted)
    {
        target.Opacity = from;
        if (duration <= TimeSpan.Zero)
        {
            target.Opacity = to;
            onCompleted?.Invoke();
            return;
        }

        var start = DateTime.UtcNow;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) =>
        {
            var progress = Math.Min(1.0, (DateTime.UtcNow - start).TotalMilliseconds / duration.TotalMilliseconds);
            var eased = EaseOutQuad(progress); // QuadraticEase EaseOut equivalent
            target.Opacity = from + (to - from) * eased;

            if (progress >= 1.0)
            {
                _activeFadeTimers.Remove(timer);
                timer.Stop();
                onCompleted?.Invoke();
            }
        };
        _activeFadeTimers.Add(timer);
        timer.Start();
    }

    private static double EaseOutQuad(double t) => 1 - (1 - t) * (1 - t);

    private void StopSkeletons()
    {
        SkeletonShimmer.StopSubtree(_loadingPresenter);
        SkeletonShimmer.StopSubtree(_longRunningPresenter);
    }

    private ContentPresenter[] GetPresenters() =>
        [_contentPresenter, _loadingPresenter, _emptyPresenter, _errorPresenter, _longRunningPresenter];

    private static void Show(ContentPresenter presenter, bool isInteractive)
    {
        presenter.IsVisible = true;
        presenter.Opacity = 1;
        presenter.IsHitTestVisible = isInteractive;
    }

    private static void Hide(ContentPresenter presenter)
    {
        presenter.IsVisible = false;
        presenter.Opacity = 1;
        presenter.IsHitTestVisible = false;
    }
}

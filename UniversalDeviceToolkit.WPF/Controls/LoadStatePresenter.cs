using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using UniversalDeviceToolkit.WPF.Controls.Loading;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls;

public class LoadStatePresenter : UserControl, IDisposable
{
    private static readonly TimeSpan DefaultLoadingDisplayDelay = TimeSpan.FromMilliseconds(120);
    private static readonly TimeSpan DefaultMinimumLoadingDisplayTime = TimeSpan.FromMilliseconds(220);
    private static readonly TimeSpan DefaultLongRunningThreshold = TimeSpan.FromSeconds(10);
    private static readonly Duration DefaultCrossfadeDuration = new(TimeSpan.FromMilliseconds(160));

    public static readonly DependencyProperty LoadStateProperty = DependencyProperty.Register(
        nameof(LoadState), typeof(LoadState), typeof(LoadStatePresenter),
        new PropertyMetadata(LoadState.Loading, OnPresentationChanged));

    public static readonly DependencyProperty LoadingContentProperty = DependencyProperty.Register(
        nameof(LoadingContent), typeof(object), typeof(LoadStatePresenter),
        new PropertyMetadata(null, OnSlotContentChanged));

    public static readonly DependencyProperty EmptyContentProperty = DependencyProperty.Register(
        nameof(EmptyContent), typeof(object), typeof(LoadStatePresenter),
        new PropertyMetadata(null, OnSlotContentChanged));

    public static readonly DependencyProperty ErrorContentProperty = DependencyProperty.Register(
        nameof(ErrorContent), typeof(object), typeof(LoadStatePresenter),
        new PropertyMetadata(null, OnSlotContentChanged));

    public static readonly DependencyProperty LongRunningContentProperty = DependencyProperty.Register(
        nameof(LongRunningContent), typeof(object), typeof(LoadStatePresenter),
        new PropertyMetadata(null, OnSlotContentChanged));

    public static readonly DependencyProperty ContentVisibilityWhileLoadingProperty = DependencyProperty.Register(
        nameof(ContentVisibilityWhileLoading), typeof(Visibility), typeof(LoadStatePresenter),
        new PropertyMetadata(Visibility.Hidden, OnPresentationChanged));

    public static readonly DependencyProperty LoadingDisplayDelayProperty = DependencyProperty.Register(
        nameof(LoadingDisplayDelay), typeof(TimeSpan), typeof(LoadStatePresenter),
        new PropertyMetadata(DefaultLoadingDisplayDelay));

    public static readonly DependencyProperty MinimumLoadingDisplayTimeProperty = DependencyProperty.Register(
        nameof(MinimumLoadingDisplayTime), typeof(TimeSpan), typeof(LoadStatePresenter),
        new PropertyMetadata(DefaultMinimumLoadingDisplayTime));

    public static readonly DependencyProperty LongRunningThresholdProperty = DependencyProperty.Register(
        nameof(LongRunningThreshold), typeof(TimeSpan), typeof(LoadStatePresenter),
        new PropertyMetadata(DefaultLongRunningThreshold));

    public static readonly DependencyProperty CrossfadeDurationProperty = DependencyProperty.Register(
        nameof(CrossfadeDuration), typeof(Duration), typeof(LoadStatePresenter),
        new PropertyMetadata(DefaultCrossfadeDuration));

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

    public Visibility ContentVisibilityWhileLoading
    {
        get => (Visibility)GetValue(ContentVisibilityWhileLoadingProperty);
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

    public Duration CrossfadeDuration
    {
        get => (Duration)GetValue(CrossfadeDurationProperty);
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

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        _contentPresenter.Content = Content;
        _progressRing.SetResourceReference(StyleProperty, "AppLoadingRingStyle");
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
        _sessionCoordinator.Dispose();
        StopSkeletons();
    }

    private static void OnPresentationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LoadStatePresenter presenter)
            presenter.StartTransition();
    }

    private static void OnSlotContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LoadStatePresenter { _isPresenterInitialized: true } presenter)
            presenter.UpdateSlotContents();
    }

    private void OnPresenterLoaded(object sender, RoutedEventArgs e) => StartTransition();

    private void OnPresenterUnloaded(object sender, RoutedEventArgs e)
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
                    _contentPresenter.Visibility = ContentVisibilityWhileLoading;
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
        _contentPresenter.Visibility = ContentVisibilityWhileLoading;
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
            if (!ReferenceEquals(presenter, target) && presenter.Visibility == Visibility.Visible)
                outgoing.Add(presenter);
        }

        var duration = ResolveCrossfadeDuration();
        if (!duration.HasTimeSpan || duration.TimeSpan <= TimeSpan.Zero)
        {
            ApplySynchronously(target);
            return;
        }

        Show(target, true);
        target.Opacity = 0;

        foreach (var presenter in outgoing)
        {
            presenter.IsHitTestVisible = false;
            var fadeOut = new DoubleAnimation(0, duration);
            fadeOut.Completed += (_, _) =>
            {
                if (generation != _transitionGeneration)
                    return;
                Hide(presenter);
                presenter.BeginAnimation(OpacityProperty, null);
            };
            presenter.BeginAnimation(OpacityProperty, fadeOut);
        }

        var fadeIn = new DoubleAnimation(0, 1, duration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        fadeIn.Completed += (_, _) =>
        {
            if (generation != _transitionGeneration)
                return;
            target.BeginAnimation(OpacityProperty, null);
            target.Opacity = 1;
        };
        target.BeginAnimation(OpacityProperty, fadeIn);
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

    private Duration ResolveCrossfadeDuration()
    {
        if (TryFindResource("AnimationDurationSkeletonCrossfade") is Duration configured &&
            configured.HasTimeSpan && configured.TimeSpan <= TimeSpan.Zero)
            return new Duration(TimeSpan.Zero);

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
        if (Dispatcher.CheckAccess())
            SetCurrentValue(LoadStateProperty, state);
        else
            Dispatcher.Invoke(() => SetCurrentValue(LoadStateProperty, state));
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
        foreach (var presenter in GetPresenters())
        {
            presenter.BeginAnimation(OpacityProperty, null);
            presenter.Opacity = 1;
        }
    }

    private void StopSkeletons()
    {
        SkeletonShimmer.StopSubtree(_loadingPresenter);
        SkeletonShimmer.StopSubtree(_longRunningPresenter);
    }

    private ContentPresenter[] GetPresenters() =>
        [_contentPresenter, _loadingPresenter, _emptyPresenter, _errorPresenter, _longRunningPresenter];

    private static void Show(ContentPresenter presenter, bool isInteractive)
    {
        presenter.Visibility = Visibility.Visible;
        presenter.Opacity = 1;
        presenter.IsHitTestVisible = isInteractive;
    }

    private static void Hide(ContentPresenter presenter)
    {
        presenter.Visibility = Visibility.Collapsed;
        presenter.Opacity = 1;
        presenter.IsHitTestVisible = false;
    }
}

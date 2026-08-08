using System.Windows;
using UniversalDeviceToolkit.WPF.Controls.Loading;

namespace UniversalDeviceToolkit.WPF.Controls;

public class LoadableControl : LoadStatePresenter
{
    public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
        nameof(IsLoading), typeof(bool), typeof(LoadableControl),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsLoadingChanged));

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public bool IsIndeterminate
    {
        get => LoadingProgressRing.IsIndeterminate;
        set => LoadingProgressRing.IsIndeterminate = value;
    }

    public double Progress
    {
        get => LoadingProgressRing.Progress;
        set => LoadingProgressRing.Progress = value;
    }

    public double IndicatorWidth
    {
        get => LoadingProgressRing.Width;
        set => LoadingProgressRing.Width = value;
    }

    public double IndicatorHeight
    {
        get => LoadingProgressRing.Height;
        set => LoadingProgressRing.Height = value;
    }

    public HorizontalAlignment IndicatorHorizontalAlignment
    {
        get => LoadingProgressRing.HorizontalAlignment;
        set => LoadingProgressRing.HorizontalAlignment = value;
    }

    public VerticalAlignment IndicatorVerticalAlignment
    {
        get => LoadingProgressRing.VerticalAlignment;
        set => LoadingProgressRing.VerticalAlignment = value;
    }

    public Thickness IndicatorMargin
    {
        get => LoadingProgressRing.Margin;
        set => LoadingProgressRing.Margin = value;
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property != LoadStateProperty)
            return;

        var isLoading = LoadState is LoadState.Loading or LoadState.Refreshing;
        if (IsLoading != isLoading)
            SetCurrentValue(IsLoadingProperty, isLoading);
    }

    private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not LoadableControl control)
            return;

        var targetState = e.NewValue is true ? LoadState.Loading : LoadState.Ready;
        if (control.LoadState != targetState)
            control.SetCurrentValue(LoadStateProperty, targetState);
    }
}

using Avalonia;
using Avalonia.Data;
using Avalonia.Layout;
using UniversalDeviceToolkit.Avalonia.Controls.Loading;

namespace UniversalDeviceToolkit.Avalonia.Controls;

public class LoadableControl : LoadStatePresenter
{
    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<LoadableControl, bool>(nameof(IsLoading), true, defaultBindingMode: BindingMode.TwoWay);

    static LoadableControl()
    {
        IsLoadingProperty.Changed.AddClassHandler<LoadableControl>((control, e) => OnIsLoadingChanged(control, e));
    }

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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property != LoadStateProperty)
            return;

        var isLoading = LoadState is LoadState.Loading or LoadState.Refreshing;
        if (IsLoading != isLoading)
            SetCurrentValue(IsLoadingProperty, isLoading);
    }

    private static void OnIsLoadingChanged(LoadableControl control, AvaloniaPropertyChangedEventArgs e)
    {
        var targetState = e.NewValue is true ? LoadState.Loading : LoadState.Ready;
        if (control.LoadState != targetState)
            control.SetCurrentValue(LoadStateProperty, targetState);
    }
}

using System;
using System.Threading;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;

namespace UniversalDeviceToolkit.Avalonia.Behaviors;

/// <summary>
/// Smoothly animates a <see cref="ProgressBar"/> value change over 250 ms.
/// Replaces the WPF DoubleAnimation implementation with an Avalonia keyframe animation
/// on <see cref="ProgressBar.ValueProperty"/>.
/// Attach via <see cref="Attach"/> / <see cref="Detach"/> (Avalonia has no Behavior{T}
/// base class).
/// </summary>
public class ProgressBarAnimateBehavior
{
    private ProgressBar? _associatedObject;
    private bool _isAnimating;

    /// <summary>Gets the progress bar this behavior is attached to.</summary>
    public ProgressBar? AssociatedObject => _associatedObject;

    public void Attach(ProgressBar progressBar)
    {
        ArgumentNullException.ThrowIfNull(progressBar);

        if (_associatedObject is not null)
            Detach();

        _associatedObject = progressBar;
        _associatedObject.ValueChanged += ProgressBar_ValueChanged;
    }

    public void Detach()
    {
        if (_associatedObject is null)
            return;

        _associatedObject.ValueChanged -= ProgressBar_ValueChanged;
        _associatedObject = null;
        _isAnimating = false;
    }

    private async void ProgressBar_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not ProgressBar progressBar || _isAnimating)
            return;

        _isAnimating = true;

        try
        {
            e.Handled = true;

            // AVALONIA: ProgressBar has no IClock property; RunAsync drives the
            // animation on the control's own clock.
            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(250),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters = { new Setter(ProgressBar.ValueProperty, e.OldValue) },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters = { new Setter(ProgressBar.ValueProperty, e.NewValue) },
                    },
                },
            };

            // The underlying Value is already e.NewValue (the event fired after the change),
            // so with the default FillMode the animation reverts to it on completion —
            // matching the WPF FillBehavior.Stop semantics.
            await animation.RunAsync(progressBar, CancellationToken.None);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProgressBarAnimateBehavior animation failed: {ex}");
        }
        finally
        {
            _isAnimating = false;
        }
    }
}

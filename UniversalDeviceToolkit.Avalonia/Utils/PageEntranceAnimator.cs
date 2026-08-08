using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Avalonia equivalent of the WPF page entrance animation: the page fades in
/// while translating up by the subtle offset, both with a cubic ease-out over
/// the medium animation duration used across the WPF shell.
/// </summary>
internal static class PageEntranceAnimator
{
    private const double Offset = 12;

    public static void Play(Control target)
    {
        var translate = target.RenderTransform as TranslateTransform;
        if (translate is null)
        {
            translate = new TranslateTransform { Y = Offset };
            target.RenderTransform = translate;
        }
        else
        {
            translate.Y = Offset;
        }

        target.Opacity = 0;

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(180),
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Forward,
        };
        animation.Children.Add(new KeyFrame
        {
            KeyTime = TimeSpan.Zero,
            Setters =
            {
                new Setter(Visual.OpacityProperty, 0d),
                new Setter(TranslateTransform.YProperty, Offset),
            },
        });
        animation.Children.Add(new KeyFrame
        {
            KeyTime = TimeSpan.FromMilliseconds(180),
            Setters =
            {
                new Setter(Visual.OpacityProperty, 1d),
                new Setter(TranslateTransform.YProperty, 0d),
            },
        });

        _ = animation.RunAsync(target);
    }
}

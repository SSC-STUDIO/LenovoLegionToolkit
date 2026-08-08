using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace UniversalDeviceToolkit.WPF.Utils;

internal static class PageEntranceAnimator
{
    public static void Play(FrameworkElement target)
    {
        if (target.RenderTransform is not TranslateTransform)
            target.RenderTransform = new TranslateTransform();

        target.Opacity = 0;

        var opacityAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = (Duration)Application.Current.Resources["AnimationDurationMedium"],
            EasingFunction = Application.Current.Resources["AnimationEasingCubicOut"] as IEasingFunction,
        };

        var translateAnimation = new DoubleAnimation
        {
            From = (double)Application.Current.Resources["AnimationSubtleTranslationOffset"],
            To = 0,
            Duration = (Duration)Application.Current.Resources["AnimationDurationMedium"],
            EasingFunction = Application.Current.Resources["AnimationEasingCubicOut"] as IEasingFunction,
        };

        Storyboard.SetTarget(opacityAnimation, target);
        Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));

        Storyboard.SetTarget(translateAnimation, target);
        Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

        var storyboard = new Storyboard();
        storyboard.Children.Add(opacityAnimation);
        storyboard.Children.Add(translateAnimation);
        storyboard.Begin();
    }
}

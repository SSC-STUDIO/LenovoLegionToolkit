using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LenovoLegionToolkit.Plugins.DockLauncher;

/// <summary>
/// Popup window for displaying magnified icon above the dock
/// This ensures the magnified icon is never clipped or obscured
/// </summary>
public partial class MagnifiedIconPopup : Window
{
    private readonly ScaleTransform _scaleTransform;

    public MagnifiedIconPopup()
    {
        InitializeComponent();
        _scaleTransform = (ScaleTransform)_magnifiedIcon.RenderTransform;
        
        // Make window non-interactive
        IsHitTestVisible = false;
    }

    /// <summary>
    /// Show the magnified icon at the specified position
    /// </summary>
    public void ShowMagnified(ImageSource iconSource, Point screenPosition, double scale = 1.3)
    {
        if (iconSource == null)
            return;

        _magnifiedIcon.Source = iconSource;
        
        // Position window above the icon
        Left = screenPosition.X - 48; // Center on icon (96/2)
        Top = screenPosition.Y - 120; // Position above icon
        
        // Animate scale
        var scaleAnimation = new DoubleAnimation
        {
            From = 0.8,
            To = scale,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
        _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        
        // Show window
        Visibility = Visibility.Visible;
        Show();
    }

    /// <summary>
    /// Hide the magnified icon with animation
    /// </summary>
    public void HideMagnified()
    {
        var scaleAnimation = new DoubleAnimation
        {
            From = _scaleTransform.ScaleX,
            To = 0.8,
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        
        scaleAnimation.Completed += (s, e) =>
        {
            Visibility = Visibility.Collapsed;
            Hide();
        };
        
        _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
        _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
    }

    /// <summary>
    /// Update the position of the magnified icon
    /// </summary>
    public void UpdatePosition(Point screenPosition)
    {
        Left = screenPosition.X - 48;
        Top = screenPosition.Y - 120;
    }
}


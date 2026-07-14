using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using FluentAssertions;
using UniversalDeviceToolkit.WPF.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class SkeletonShimmerTests
{
    private static readonly Color ShimmerStart = Color.FromArgb(0x26, 0x88, 0x91, 0xA0);
    private static readonly Color ShimmerPeak = Color.FromArgb(0x4A, 0x88, 0x91, 0xA0);
    private static readonly Color ShimmerStartLight = Color.FromArgb(0x4A, 0x88, 0xA8, 0xC0);
    private static readonly Color ShimmerPeakLight = Color.FromArgb(0x8A, 0x98, 0xB8, 0xD0);

    [Fact]
    public void CreateShimmerBrush_ShouldHaveOrderedStopsAndVisiblePeak()
    {
        var baseColor = Color.FromRgb(0x3A, 0x3D, 0x42);
        var brush = SkeletonShimmer.CreateShimmerBrush(baseColor, ShimmerStart, ShimmerPeak);
        brush.GradientStops.Select(x => x.Offset).Should().BeInAscendingOrder();
        brush.GradientStops.Should().OnlyContain(x => x.Offset >= 0 && x.Offset <= 1);
        brush.GradientStops.First().Offset.Should().Be(0);
        brush.GradientStops.Last().Offset.Should().Be(1);
        brush.GradientStops.Single(x => Math.Abs(x.Offset - 0.48) < 0.001).Color
            .Should().Be(SkeletonShimmer.CompositeOverlay(baseColor, ShimmerPeak));
        var peak = brush.GradientStops.Max(x => Luminance(x.Color));
        var lift = peak - Luminance(baseColor);
        lift.Should().BeInRange(0.02, 0.22);
    }

    [Fact]
    public void CreateShimmerBrush_OnLightBase_ShouldProduceStrongerVisiblePeak()
    {
        var baseColor = Color.FromRgb(0xEB, 0xEB, 0xEB);
        var darkBrush = SkeletonShimmer.CreateShimmerBrush(baseColor, ShimmerStart, ShimmerPeak);
        var lightBrush = SkeletonShimmer.CreateShimmerBrush(baseColor, ShimmerStartLight, ShimmerPeakLight);

        var darkLift = darkBrush.GradientStops.Max(x => Luminance(x.Color)) - Luminance(baseColor);
        var lightLift = lightBrush.GradientStops.Max(x => Luminance(x.Color)) - Luminance(baseColor);

        lightLift.Should().BeGreaterThan(darkLift);
        lightLift.Should().BeInRange(0.03, 0.28);
    }

    [Fact]
    public void CompositeOverlay_ShouldHonorTransparentAndOpaqueBoundaries()
    {
        var baseColor = Color.FromRgb(0x30, 0x32, 0x36);
        var overlay = Color.FromRgb(0x88, 0x91, 0xA0);
        SkeletonShimmer.CompositeOverlay(baseColor, Color.FromArgb(0, overlay.R, overlay.G, overlay.B)).Should().Be(baseColor);
        SkeletonShimmer.CompositeOverlay(baseColor, Color.FromArgb(255, overlay.R, overlay.G, overlay.B)).Should().Be(overlay);
    }

    [Fact]
    public void LoadedBorder_ShouldAnimate_AndUnloadShouldStop()
    {
        RunOnSta(() =>
        {
            var border = CreateBorder(TimeSpan.FromMilliseconds(240));
            using var host = Show(border);
            Pump(80);
            var transform = GetTransform(border);
            var first = transform.X;
            Pump(90);
            transform.X.Should().NotBeApproximately(first, 0.01);
            host.Window.Content = null;
            Pump(40);
            var stopped = transform.X;
            Pump(100);
            transform.X.Should().BeApproximately(stopped, 0.001);
        });
    }

    [Fact]
    public void RestartThenImmediateStop_ShouldLeaveNoMovingAnimation()
    {
        RunOnSta(() =>
        {
            var border = CreateBorder(TimeSpan.FromMilliseconds(240));
            var root = new Grid();
            root.Children.Add(border);
            using var host = Show(root);
            Pump(60);
            SkeletonShimmer.StopSubtree(root);
            SkeletonShimmer.RestartSubtree(root);
            SkeletonShimmer.StopSubtree(root);
            Pump(60);
            if ((border.Background as LinearGradientBrush)?.RelativeTransform is not TranslateTransform transform)
                return;
            var first = transform.X;
            Pump(100);
            transform.X.Should().BeApproximately(first, 0.001);
        });
    }

    [Fact]
    public void ZeroDuration_ShouldRenderStaticSolidBackground()
    {
        RunOnSta(() =>
        {
            var color = Color.FromRgb(0x44, 0x48, 0x50);
            var border = CreateBorder(TimeSpan.Zero, color);
            using var host = Show(border);
            Pump(60);
            border.Background.Should().BeOfType<SolidColorBrush>();
            ((SolidColorBrush)border.Background).Color.Should().Be(color);
        });
    }

    [Fact]
    public void RestartSubtree_ShouldPreserveConfiguredDelaysWithoutWritingAutomaticStagger()
    {
        RunOnSta(() =>
        {
            var root = new StackPanel();
            var borders = Enumerable.Range(0, 12).Select(_ => CreateBorder(TimeSpan.FromMilliseconds(240))).ToArray();
            foreach (var border in borders) root.Children.Add(border);
            using var host = Show(root);
            Pump(50);
            foreach (var border in borders) SkeletonShimmer.SetDelaySeconds(border, -1);
            SkeletonShimmer.SetDelaySeconds(borders[3], 0.22);
            SkeletonShimmer.RestartSubtree(root);
            Pump(40);
            SkeletonShimmer.GetDelaySeconds(borders[0]).Should().Be(-1);
            SkeletonShimmer.GetDelaySeconds(borders[1]).Should().Be(-1);
            SkeletonShimmer.GetDelaySeconds(borders[3]).Should().BeApproximately(0.22, 0.0001);
            borders.Where((_, index) => index != 3)
                .Select(SkeletonShimmer.GetDelaySeconds)
                .Should()
                .OnlyContain(x => x == -1);
        });
    }

    private static Border CreateBorder(TimeSpan duration, Color? color = null)
    {
        var brush = new SolidColorBrush(color ?? Color.FromRgb(0x44, 0x48, 0x50));
        var border = new Border { Width = 180, Height = 24, Background = brush };
        border.Resources["AnimationDurationShimmer"] = new Duration(duration);
        border.Resources["ControlFillColorSecondaryBrush"] = brush;
        SkeletonShimmer.SetIsEnabled(border, true);
        return border;
    }

    private static TranslateTransform GetTransform(Border border)
    {
        border.Background.Should().BeOfType<LinearGradientBrush>();
        return ((LinearGradientBrush)border.Background).RelativeTransform.Should().BeOfType<TranslateTransform>().Subject;
    }

    private static Host Show(UIElement content)
    {
        var window = new Window { Width = 360, Height = 180, ShowInTaskbar = false, WindowStyle = WindowStyle.None, Content = content };
        window.Show();
        return new Host(window);
    }

    private static void Pump(int milliseconds)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    private static void RunOnSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { exception = ex; }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(15)).Should().BeTrue();
        if (exception is not null) throw exception;
    }

    private static double Luminance(Color color)
    {
        static double Linear(byte c) { var x = c / 255.0; return x <= 0.04045 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4); }
        return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
    }

    private sealed class Host(Window window) : IDisposable
    {
        public Window Window { get; } = window;
        public void Dispose() { if (Window.IsVisible) Window.Close(); Pump(20); }
    }
}

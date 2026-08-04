using System.Threading;
using System.Windows;
using System.Windows.Automation;
using FluentAssertions;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.WPF.Controls;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class AdaptiveTextBlockTests
{
    [Fact]
    public void ShortText_ShouldKeepTheBaseFontSize()
    {
        RunOnSta(() =>
        {
            var block = new AdaptiveTextBlock
            {
                Text = "Short",
                FontSize = 16,
                Width = 240,
                OverflowMode = LocalizedOverflowMode.Wrap,
                MaxLines = 2,
            };
            using var host = Show(block, 260, 100);
            block.UpdateLayout();

            block.FontSize.Should().Be(16);
        });
    }

    [Fact]
    public void LongText_ShouldUseTheSharedMinimumFontAndPreserveTheFullAutomationName()
    {
        RunOnSta(() =>
        {
            const string text = "A localized description that needs more than one line in a compact card.";
            var block = new AdaptiveTextBlock
            {
                Text = text,
                FontSize = 16,
                Width = 90,
                OverflowMode = LocalizedOverflowMode.Wrap,
                MaxLines = 2,
                MinFontSize = LocalizedOverflowPolicy.MinimumReadableFontSize,
            };
            using var host = Show(block, 110, 80);
            block.UpdateLayout();

            block.FontSize.Should().BeGreaterThanOrEqualTo(LocalizedOverflowPolicy.MinimumReadableFontSize);
            AutomationProperties.GetName(block).Should().Be(text);
        });
    }

    [Fact]
    public void EllipsisMode_ShouldRemainSingleLineAndRecalculateAfterResize()
    {
        RunOnSta(() =>
        {
            var block = new AdaptiveTextBlock
            {
                Text = "A very long localized navigation title",
                FontSize = 14,
                Width = 80,
                OverflowMode = LocalizedOverflowMode.Ellipsis,
                MaxLines = 1,
            };
            using var host = Show(block, 100, 60);
            block.UpdateLayout();

            block.TextWrapping.Should().Be(TextWrapping.NoWrap);
            block.TextTrimming.Should().Be(TextTrimming.CharacterEllipsis);
            block.ToolTip.Should().Be(block.Text);

            block.Width = 260;
            block.UpdateLayout();
            block.ToolTip.Should().BeNull();
        });
    }

    private static IDisposable Show(FrameworkElement content, double width, double height)
    {
        var window = new Window
        {
            Width = width,
            Height = height,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            Content = content,
        };
        window.Show();
        return new WindowLease(window);
    }

    private static void RunOnSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { exception = ex; }
            finally { System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(15)).Should().BeTrue();
        if (exception is not null)
            throw exception;
    }

    private sealed class WindowLease(Window window) : IDisposable
    {
        public void Dispose()
        {
            if (window.IsVisible)
                window.Close();
        }
    }
}

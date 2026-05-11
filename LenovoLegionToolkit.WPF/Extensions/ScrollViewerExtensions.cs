using System.Windows.Controls;

namespace LenovoLegionToolkit.WPF.Extensions;

public static class ScrollViewerExtensions
{
    public static void ScrollToTop(this ScrollViewer scrollViewer)
    {
        scrollViewer.ScrollToVerticalOffset(0);
        scrollViewer.ScrollToHorizontalOffset(0);
    }
}

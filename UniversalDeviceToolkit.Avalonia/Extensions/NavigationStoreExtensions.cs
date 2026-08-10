using System.Linq;
using UniversalDeviceToolkit.Avalonia.Controls.Custom;

namespace UniversalDeviceToolkit.Avalonia.Extensions;

public static class NavigationStoreExtensions
{
    public static void NavigateToNext(this NavigationStore navigationStore)
    {
        var navigationItems = navigationStore.Items.Concat(navigationStore.Footer).ToList();
        if (navigationItems.Count == 0)
            return;

        var current = navigationStore.Current ?? navigationItems.FirstOrDefault();

        if (current is null)
            return;

        var currentIndex = navigationItems.IndexOf(current);
        var index = (currentIndex + 1) % navigationItems.Count;
        var next = navigationItems[index];

        navigationStore.Navigate(next.PageTag);
    }

    public static void NavigateToPrevious(this NavigationStore navigationStore)
    {
        var navigationItems = navigationStore.Items.Concat(navigationStore.Footer).ToList();
        if (navigationItems.Count == 0)
            return;

        var current = navigationStore.Current ?? navigationItems.FirstOrDefault();

        if (current is null)
            return;

        var currentIndex = navigationItems.IndexOf(current);
        var index = currentIndex < 0 ? 0 : currentIndex - 1;
        if (index < 0)
            index = navigationItems.Count - 1;
        var next = navigationItems[index];

        navigationStore.Navigate(next.PageTag);
    }
}

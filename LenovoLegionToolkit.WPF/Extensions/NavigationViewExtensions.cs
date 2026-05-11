using System.Linq;
using LenovoLegionToolkit.WPF.Controls.Custom;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Extensions;

public static class NavigationViewExtensions
{
    public static void NavigateToNext(this NavigationView navigationView)
    {
        var navigationItems = navigationView.MenuItems.OfType<NavigationItem>()
            .Concat(navigationView.FooterMenuItems.OfType<NavigationItem>())
            .ToList();
        var current = navigationView.SelectedItem as NavigationItem ?? navigationItems.FirstOrDefault();

        if (current is null)
            return;

        var index = (navigationItems.IndexOf(current) + 1) % navigationItems.Count;
        var next = navigationItems[index];

        if (next.TargetPageTag is { } tag)
            navigationView.Navigate(tag, null);
    }

    public static void NavigateToPrevious(this NavigationView navigationView)
    {
        var navigationItems = navigationView.MenuItems.OfType<NavigationItem>()
            .Concat(navigationView.FooterMenuItems.OfType<NavigationItem>())
            .ToList();
        var current = navigationView.SelectedItem as NavigationItem ?? navigationItems.FirstOrDefault();

        if (current is null)
            return;

        var index = navigationItems.IndexOf(current) - 1;
        if (index < 0)
            index = navigationItems.Count - 1;
        var next = navigationItems[index];

        if (next.TargetPageTag is { } tag)
            navigationView.Navigate(tag, null);
    }
}

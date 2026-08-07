#if WINDOWS
using Avalonia;
using UniversalDeviceToolkit.Lib;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Pure placement policy shared by the Avalonia shell and its unit tests. Screen
/// coordinates are logical DIPs so persisted WPF placement remains usable.
/// </summary>
internal static class AvaloniaWindowPlacementCoordinator
{
    internal readonly record struct ScreenWorkArea(Rect Bounds, bool IsPrimary);

    internal readonly record struct RestoreResult(Rect Bounds, bool IsMaximized);

    internal static RestoreResult? Restore(
        WindowPlacement? placement,
        WindowSize? legacySize,
        Size minimumSize,
        IReadOnlyList<ScreenWorkArea> screens)
    {
        var validScreens = screens.Where(screen => IsValid(screen.Bounds)).ToArray();
        var primary = validScreens.FirstOrDefault(screen => screen.IsPrimary);
        if (!IsValid(primary.Bounds) && validScreens.Length > 0)
            primary = validScreens[0];

        if (placement is { } saved && IsValid(saved))
        {
            var bounds = ApplyMinimumSize(new Rect(saved.Left, saved.Top, saved.Width, saved.Height), minimumSize);
            if (validScreens.Length == 0)
                return new RestoreResult(bounds, saved.IsMaximized);

            var center = bounds.Center;
            if (!validScreens.Any(screen => screen.Bounds.Contains(center)))
                bounds = CenterOn(bounds, primary.Bounds);
            else
                bounds = ClampToVirtualDesktop(bounds, validScreens);

            return new RestoreResult(bounds, saved.IsMaximized);
        }

        if (legacySize is not { } size || !IsValid(size))
            return null;

        var legacyBounds = ApplyMinimumSize(new Rect(0, 0, size.Width, size.Height), minimumSize);
        if (validScreens.Length > 0)
            legacyBounds = CenterOn(legacyBounds, primary.Bounds);

        return new RestoreResult(legacyBounds, false);
    }

    internal static WindowPlacement Capture(Rect normalBounds, bool isMaximized) =>
        new(normalBounds.X, normalBounds.Y, normalBounds.Width, normalBounds.Height, isMaximized);

    private static Rect ApplyMinimumSize(Rect bounds, Size minimumSize) =>
        new(bounds.X, bounds.Y, Math.Max(bounds.Width, minimumSize.Width), Math.Max(bounds.Height, minimumSize.Height));

    private static Rect CenterOn(Rect bounds, Rect workArea) =>
        new(
            workArea.X + (workArea.Width - bounds.Width) / 2,
            workArea.Y + (workArea.Height - bounds.Height) / 2,
            bounds.Width,
            bounds.Height);

    private static Rect ClampToVirtualDesktop(Rect bounds, IReadOnlyList<ScreenWorkArea> screens)
    {
        var left = screens.Min(screen => screen.Bounds.Left);
        var top = screens.Min(screen => screen.Bounds.Top);
        var right = screens.Max(screen => screen.Bounds.Right);
        var bottom = screens.Max(screen => screen.Bounds.Bottom);
        var width = Math.Min(bounds.Width, right - left);
        var height = Math.Min(bounds.Height, bottom - top);

        return new Rect(
            Math.Clamp(bounds.X, left, right - width),
            Math.Clamp(bounds.Y, top, bottom - height),
            width,
            height);
    }

    private static bool IsValid(WindowPlacement placement) =>
        double.IsFinite(placement.Left)
        && double.IsFinite(placement.Top)
        && double.IsFinite(placement.Width)
        && double.IsFinite(placement.Height)
        && placement.Width > 0
        && placement.Height > 0;

    private static bool IsValid(WindowSize size) =>
        double.IsFinite(size.Width)
        && double.IsFinite(size.Height)
        && size.Width > 0
        && size.Height > 0;

    private static bool IsValid(Rect bounds) =>
        double.IsFinite(bounds.X)
        && double.IsFinite(bounds.Y)
        && double.IsFinite(bounds.Width)
        && double.IsFinite(bounds.Height)
        && bounds.Width > 0
        && bounds.Height > 0;
}
#endif

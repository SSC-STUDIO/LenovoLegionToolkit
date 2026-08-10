using System;

namespace UniversalDeviceToolkit.Avalonia.Controls.Loading;

public enum LoadingChromeOwnership
{
    Navigation,
    Page
}

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class LoadingChromeOwnerAttribute(
    LoadingChromeOwnership ownership,
    int delayMilliseconds = 120,
    int minimumVisibleMilliseconds = 220) : Attribute
{
    public LoadingChromeOwnership Ownership { get; } = ownership;
    public int DelayMilliseconds { get; } = delayMilliseconds;
    public int MinimumVisibleMilliseconds { get; } = minimumVisibleMilliseconds;
}

public interface ILoadingChromeOwner
{
    LoadingChromeOwnership LoadingChromeOwnership { get; }
}

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// Mirrors the WPF-UI <c>ControlAppearance</c> enum. Drives the visual state of
/// appearance-aware controls (Button, TextBox, ToggleSwitch, SnackbarPresenter, ...).
/// XAML attribute values such as <c>Appearance="Caution"</c> convert automatically
/// because <see cref="ControlAppearance"/> is an enum.
/// </summary>
public enum ControlAppearance
{
    Primary,
    Secondary,
    Tertiary,
    Success,
    Caution,
    Danger,
    Info,
    Transparent
}

/// <summary>
/// Mirrors the WPF-UI <c>AppearanceMode</c> enum.
/// </summary>
public enum AppearanceMode
{
    None,
    Disabled
}

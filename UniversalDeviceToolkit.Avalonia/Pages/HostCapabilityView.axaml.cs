using Avalonia.Automation;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using UniversalDeviceToolkit.Avalonia.Localization;

namespace UniversalDeviceToolkit.Avalonia.Pages;

/// <summary>
/// Explicitly describes a WPF-only feature when it is opened from the Avalonia
/// shell. This is a capability result, not a placeholder page: it records why
/// no safe cross-platform adapter is available and keeps the route observable.
/// </summary>
public partial class HostCapabilityView : UserControl
{
    public HostCapabilityView(
        string titleKey,
        string titleFallback,
        string descriptionFallback,
        string iconIdentifier,
        string capabilityFallback)
    {
        InitializeComponent();

        var title = AvaloniaLocalization.GetString(titleKey, titleFallback);
        TitleBlock.Text = title;
        DescriptionBlock.Text = descriptionFallback;
        CapabilityIcon.IconIdentifier = iconIdentifier;
        StatusTitleBlock.Text = AvaloniaLocalization.GetString(
            "HostCapability_UnsupportedTitle",
            "Unavailable in this Avalonia host");
        StatusMessageBlock.Text = AvaloniaLocalization.GetString(
            "HostCapability_UnsupportedMessage",
            "This host does not expose a safe adapter for this page. Use the Windows host for this capability.");

        DataContext = new CapabilityViewModel(
            AvaloniaLocalization.GetString("HostCapability_CapabilityLabel", "Host capability"),
            capabilityFallback,
            AvaloniaLocalization.GetString("HostCapability_Unavailable", "Unavailable"));
        AutomationProperties.SetName(this, title);
    }

    private sealed class CapabilityViewModel(string capabilityLabel, string capabilityReason, string unavailableLabel)
        : ObservableObject
    {
        public string CapabilityLabel { get; } = capabilityLabel;
        public string CapabilityReason { get; } = capabilityReason;
        public string UnavailableLabel { get; } = unavailableLabel;
    }
}

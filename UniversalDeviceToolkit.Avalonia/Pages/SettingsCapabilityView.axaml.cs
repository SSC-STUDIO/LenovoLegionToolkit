using Avalonia.Automation;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using UniversalDeviceToolkit.Avalonia.Localization;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class SettingsCapabilityView : UserControl
{
    public SettingsCapabilityView(string titleKey, string titleFallback, string descriptionFallback, string featureFallback)
    {
        InitializeComponent();

        var title = AvaloniaLocalization.GetString(titleKey, titleFallback);
        TitleBlock.Text = title;
        DescriptionBlock.Text = descriptionFallback;
        StatusTitleBlock.Text = AvaloniaLocalization.GetString(
            "Settings_PlatformUnavailable_Title",
            "Unavailable in this host");
        StatusMessageBlock.Text = AvaloniaLocalization.GetString(
            "Settings_PlatformUnavailable_Message",
            "This Avalonia host does not expose a safe adapter for this setting.");

        DataContext = new CapabilityViewModel(
            AvaloniaLocalization.GetString("Settings_PlatformUnavailable_Feature", "Feature availability"),
            featureFallback,
            AvaloniaLocalization.GetString("Settings_PlatformUnavailable_Action", "Unavailable"));
        AutomationProperties.SetName(this, title);
    }

    private sealed class CapabilityViewModel(string featureLabel, string featureMessage, string unavailableLabel)
        : ObservableObject
    {
        public string FeatureLabel { get; } = featureLabel;
        public string FeatureMessage { get; } = featureMessage;
        public string UnavailableLabel { get; } = unavailableLabel;
    }
}

public sealed class SettingsSmartKeysView() : SettingsCapabilityView(
    "SettingsPage_Navigation_SmartKeys",
    "Smart Keys",
    "Configure Fn-lock and Smart Key actions.",
    "Smart Key controls are available only through the Windows host adapter.");

public sealed class SettingsUpdateView() : SettingsCapabilityView(
    "SettingsPage_Update_Title",
    "Update",
    "Check for application updates and choose the update channel.",
    "Update checks are not exposed by the Avalonia host adapter.");

public sealed class SettingsPowerView() : SettingsCapabilityView(
    "SettingsPage_Power_Title",
    "Power",
    "Configure power mode mapping and battery behavior.",
    "Power mode and battery controls require the Windows hardware adapter.");

public sealed class SettingsIntegrationsView() : SettingsCapabilityView(
    "SettingsPage_Integrations_Title",
    "Integrations",
    "Connect external tools and services such as HWiNFO and CLI.",
    "Integration services are not exposed by the Avalonia host adapter.");

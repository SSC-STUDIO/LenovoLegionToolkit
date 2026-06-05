using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Plugins;
using UniversalDeviceToolkit.WPF.Pages;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace UniversalDeviceToolkit.WPF.Windows.Settings
{
public partial class PluginSettingsWindow : BaseWindow
{
    private readonly IPluginManager _pluginManager = IoCContainer.Resolve<IPluginManager>();
    private readonly string _pluginId;

    public PluginSettingsWindow(string pluginId)
    {
        _pluginId = pluginId;
        InitializeComponent();
        Loaded += PluginSettingsWindow_Loaded;
        Closed += PluginSettingsWindow_Closed;
        LocalizationHelper.PluginResourceCulturesChanged += LocalizationHelper_PluginResourceCulturesChanged;
    }

    private void PluginSettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyLocalizedChromeText();
        LoadPluginSettings();
    }

    private void PluginSettingsWindow_Closed(object? sender, EventArgs e)
    {
        LocalizationHelper.PluginResourceCulturesChanged -= LocalizationHelper_PluginResourceCulturesChanged;
    }

    private void LoadPluginSettings()
    {
        try
        {
            FlowDirection = LocalizationHelper.Direction;

            var plugin = _pluginManager.GetRegisteredPlugins()
                .FirstOrDefault(p => p.Id == _pluginId);

            var metadata = _pluginManager.GetPluginMetadata(_pluginId);
            var manifest = GetPluginManifest(plugin, _pluginId);

            if (!CanShowPluginSettings(plugin, manifest))
            {
                MessageBox.Show(
                    string.Format(Resource.PluginSettingsWindow_PluginNotFound, _pluginId),
                    Resource.PluginSettingsWindow_Error,
                    System.Windows.MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
                return;
            }

            var pluginName = FirstNonEmpty(plugin?.Name, manifest?.Name, metadata?.Name, _pluginId);
            var pluginDescription = FirstNonEmpty(plugin?.Description, manifest?.Description, metadata?.Description);
            pluginDescription = string.IsNullOrWhiteSpace(pluginDescription)
                ? Resource.PluginSettingsWindow_NoConfigMessage
                : pluginDescription;

            _titleTextBlock.Text = $"{pluginName} {Resource.PluginSettingsWindow_Settings}";
            Title = _titleTextBlock.Text;
            _pluginNameTextBlock.Text = pluginName;
            _pluginDescriptionTextBlock.Text = pluginDescription;
            var pluginsDirectory = PluginIconResolver.ResolvePluginsDirectory();
            var pluginIcon = PluginIconResolver.Resolve(
                _pluginId,
                pluginName,
                FirstNonEmpty(plugin?.Icon, manifest?.Icon, metadata?.Icon),
                metadata?.FilePath,
                pluginsDirectory);
            _pluginIconHost.Content = PluginIconResolver.CreateElement(pluginIcon);
            AutomationProperties.SetAutomationId(_pluginIconHost, $"PluginSettingsIcon_{_pluginId}");
            _pluginIdTextBlock.Text = _pluginId;
            _pluginVersionTextBlock.Text = $"v{FirstNonEmpty(metadata?.Version, manifest?.Version, "1.0.0")}";
            _settingsSectionTitleTextBlock.Text = Resource.PluginSettingsWindow_Settings;
            _emptyStateTitleTextBlock.Text = Resource.PluginSettingsWindow_NoConfigMessage;
            _closeButton.Content = null;
            _closeButton.ToolTip = Resource.PluginSettingsWindow_Close;

            var author = FirstNonEmpty(metadata?.Author, manifest?.Author);
            if (!string.IsNullOrWhiteSpace(author))
            {
                _pluginAuthorTextBlock.Text = string.Format(Resource.PluginSettingsWindow_Author, author);
                _pluginAuthorBadge.Visibility = Visibility.Visible;
            }
            else
            {
                _pluginAuthorBadge.Visibility = Visibility.Collapsed;
            }

            // Try to get plugin's custom settings page using reflection
            bool hasSettingsPage = false;
            if (plugin is not null and not PluginManifestAdapter)
            {
                try
                {
                    if (_pluginSettingsHost != null)
                        _pluginSettingsHost.Content = null;

                    if (HasIncompatibleWpfUiDependency(metadata))
                    {
                        ShowCompatibilityFallback(pluginDescription, metadata);
                        return;
                    }

                    var pluginType = plugin.GetType();
                    var getSettingsPage = pluginType.GetMethod("GetSettingsPage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (getSettingsPage != null)
                    {
                        var settingsPage = getSettingsPage.Invoke(plugin, null);

                        if (PluginPageWrapper.TryCreateHostedPluginPage(settingsPage, out var pluginPage))
                        {
                            var pageObject = pluginPage.CreatePage();
                            if (pageObject is UIElement generatedElement)
                            {
                                hasSettingsPage = true;
                                if (_pluginSettingsContainer != null)
                                    _pluginSettingsContainer.Visibility = Visibility.Visible;
                                if (_pluginSettingsHost != null)
                                    _pluginSettingsHost.Content = generatedElement;
                            }
                        }
                        else if (settingsPage is UIElement element)
                        {
                            hasSettingsPage = true;
                            if (_pluginSettingsContainer != null)
                                _pluginSettingsContainer.Visibility = Visibility.Visible;
                            if (_pluginSettingsHost != null)
                                _pluginSettingsHost.Content = element;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error loading plugin settings: {ex.Message}", ex);
                    hasSettingsPage = false;
                }
            }
            
            // If plugin doesn't have a settings page, hide the container
            if (!hasSettingsPage)
            {
                if (_pluginSettingsContainer != null)
                    _pluginSettingsContainer.Visibility = Visibility.Collapsed;

                if (_pluginSettingsHost != null)
                    _pluginSettingsHost.Content = null;

                if (_emptyStateBorder != null)
                    _emptyStateBorder.Visibility = Visibility.Visible;

                if (_emptyStateHintTextBlock != null)
                    _emptyStateHintTextBlock.Text = pluginDescription;
            }
            else if (_emptyStateBorder != null)
            {
                _emptyStateBorder.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error loading plugin settings: {ex.Message}", ex);
            MessageBox.Show(
                string.Format(Resource.PluginSettingsWindow_LoadError, ex.Message),
                Resource.PluginSettingsWindow_Error,
                System.Windows.MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ShowCompatibilityFallback(string pluginDescription, PluginMetadata? metadata)
    {
        if (_pluginSettingsContainer != null)
            _pluginSettingsContainer.Visibility = Visibility.Collapsed;

        if (_pluginSettingsHost != null)
            _pluginSettingsHost.Content = null;

        if (_emptyStateBorder != null)
            _emptyStateBorder.Visibility = Visibility.Visible;

        if (_emptyStateTitleTextBlock != null)
            _emptyStateTitleTextBlock.Text = Resource.PluginSettingsWindow_NoConfigMessage;

        if (_emptyStateHintTextBlock != null)
        {
            var pluginVersion = metadata?.WpfUiVersion ?? "unknown";
            var hostVersion = typeof(SymbolIcon).Assembly.GetName().Version?.ToString() ?? "unknown";
            _emptyStateHintTextBlock.Text =
                $"{pluginDescription}{Environment.NewLine}{Environment.NewLine}This plugin settings UI targets Wpf.Ui {pluginVersion}, but the host is running Wpf.Ui {hostVersion}. The page is hidden to keep the app stable.";
        }
    }

    internal static bool CanShowPluginSettings(IPlugin? plugin, PluginManifest? manifest)
    {
        if (plugin is not null)
            return true;

        return PluginUiCapabilityResolver.ResolveFromManifest(manifest).SupportsSettingsPage;
    }

    private static PluginManifest? GetPluginManifest(IPlugin? plugin, string pluginId) =>
        plugin is PluginManifestAdapter adapter
            ? adapter.Manifest
            : PluginUiCapabilityResolver.ReadInstalledManifest(pluginId);

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }

    private static bool HasIncompatibleWpfUiDependency(PluginMetadata? metadata)
    {
        if (metadata == null || string.IsNullOrWhiteSpace(metadata.WpfUiVersion))
            return false;

        if (!Version.TryParse(metadata.WpfUiVersion, out var pluginWpfUiVersion))
            return false;

        var hostWpfUiVersion = typeof(SymbolIcon).Assembly.GetName().Version;
        if (hostWpfUiVersion == null)
            return false;

        return pluginWpfUiVersion.Major != hostWpfUiVersion.Major;
    }

    private void LocalizationHelper_PluginResourceCulturesChanged(object? sender, EventArgs e)
    {
        if (!IsLoaded)
            return;

        Dispatcher.InvokeAsync(() =>
        {
            ApplyLocalizedChromeText();
            LoadPluginSettings();
        });
    }

    private void ApplyLocalizedChromeText()
    {
        FlowDirection = LocalizationHelper.Direction;

        _titleTextBlock.Text = Resource.PluginSettingsWindow_Title;
        _settingsSectionTitleTextBlock.Text = Resource.PluginSettingsWindow_Settings;
        _emptyStateTitleTextBlock.Text = Resource.PluginSettingsWindow_NoConfigMessage;
        _emptyStateHintTextBlock.Text = Resource.PluginSettingsWindow_NoConfigMessage;
        _closeButton.Content = null;
        _closeButton.ToolTip = Resource.PluginSettingsWindow_Close;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

}
}

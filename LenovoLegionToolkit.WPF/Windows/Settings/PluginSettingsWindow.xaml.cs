using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using LenovoLegionToolkit.WPF.Windows;

namespace LenovoLegionToolkit.WPF.Windows.Settings
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
            
            if (plugin == null)
            {
                MessageBox.Show(
                    string.Format(Resource.PluginSettingsWindow_PluginNotFound, _pluginId),
                    Resource.PluginSettingsWindow_Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
                return;
            }

            var metadata = _pluginManager.GetPluginMetadata(_pluginId);
            
            var pluginName = string.IsNullOrWhiteSpace(plugin.Name) ? _pluginId : plugin.Name;
            var pluginDescription = string.IsNullOrWhiteSpace(plugin.Description)
                ? Resource.PluginSettingsWindow_NoConfigMessage
                : plugin.Description;

            _titleTextBlock.Text = $"{pluginName} {Resource.PluginSettingsWindow_Settings}";
            Title = _titleTextBlock.Text;
            _pluginNameTextBlock.Text = pluginName;
            _pluginDescriptionTextBlock.Text = pluginDescription;
            _pluginIconTextBlock.Text = GetPluginIconText(pluginName);
            _pluginIdTextBlock.Text = _pluginId;
            _pluginVersionTextBlock.Text = !string.IsNullOrWhiteSpace(metadata?.Version) ? $"v{metadata.Version}" : "v1.0.0";
            _settingsSectionTitleTextBlock.Text = Resource.PluginSettingsWindow_Settings;
            _emptyStateTitleTextBlock.Text = Resource.PluginSettingsWindow_NoConfigMessage;
            _closeButton.Content = Resource.PluginSettingsWindow_Close;

            if (!string.IsNullOrWhiteSpace(metadata?.Author))
            {
                _pluginAuthorTextBlock.Text = string.Format(Resource.PluginSettingsWindow_Author, metadata.Author);
                _pluginAuthorBadge.Visibility = Visibility.Visible;
            }
            else
            {
                _pluginAuthorBadge.Visibility = Visibility.Collapsed;
            }

            // Try to get plugin's custom settings page using reflection
            bool hasSettingsPage = false;
            if (plugin != null)
            {
                try
                {
                    if (_pluginSettingsHost != null)
                        _pluginSettingsHost.Content = null;

                    var pluginType = plugin.GetType();
                    var getSettingsPage = pluginType.GetMethod("GetSettingsPage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (getSettingsPage != null)
                    {
                        var settingsPage = getSettingsPage.Invoke(plugin, null);

                        if (settingsPage is IPluginPage pluginPage)
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
                    Lib.Utils.Log.Instance.Trace($"Error loading plugin settings: {ex.Message}", ex);
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
            Lib.Utils.Log.Instance.Trace($"Error loading plugin settings: {ex.Message}", ex);
            MessageBox.Show(
                string.Format(Resource.PluginSettingsWindow_LoadError, ex.Message),
                Resource.PluginSettingsWindow_Error,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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
        _closeButton.Content = Resource.PluginSettingsWindow_Close;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static string GetPluginIconText(string pluginName)
    {
        var candidate = (pluginName ?? string.Empty).Trim().FirstOrDefault(c => !char.IsWhiteSpace(c));
        return candidate == default ? "P" : char.ToUpperInvariant(candidate).ToString();
    }
}
}

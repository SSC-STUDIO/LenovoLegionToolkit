using System;
using System.Linq;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Windows;

namespace LenovoLegionToolkit.WPF.Windows.Settings;

public partial class PluginSettingsWindow : BaseWindow
{
    private readonly IPluginManager _pluginManager = IoCContainer.Resolve<IPluginManager>();
    private readonly string _pluginId;

    public PluginSettingsWindow(string pluginId)
    {
        _pluginId = pluginId;
        InitializeComponent();
        Loaded += PluginSettingsWindow_Loaded;
    }

    private void PluginSettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadPluginSettings();
    }

    private void LoadPluginSettings()
    {
        try
        {
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
            
            _titleTextBlock.Text = $"{plugin.Name} {Resource.PluginSettingsWindow_Settings}";
            _pluginNameTextBlock.Text = plugin.Name;
            _pluginDescriptionTextBlock.Text = plugin.Description;
            
            if (metadata != null && !string.IsNullOrWhiteSpace(metadata.Version))
            {
                _pluginNameTextBlock.Text += $" v{metadata.Version}";
            }
            
            if (metadata != null && !string.IsNullOrWhiteSpace(metadata.Author))
            {
                _pluginDescriptionTextBlock.Text += $"\n\n{string.Format(Resource.PluginSettingsWindow_Author, metadata.Author)}";
            }

            // Try to get plugin's custom settings page
            bool hasSettingsPage = false;
            if (plugin is Plugins.SDK.PluginBase sdkPlugin)
            {
                var settingsPage = sdkPlugin.GetSettingsPage();
                if (settingsPage is Plugins.SDK.IPluginPage pluginPage)
                {
                    var pageContent = pluginPage.CreatePage();
                    if (pageContent != null)
                    {
                        hasSettingsPage = true;
                        
                        // Show plugin settings container
                        if (_pluginSettingsContainer != null)
                        {
                            _pluginSettingsContainer.Visibility = Visibility.Visible;
                        }
                        
                        // Display the plugin's settings page
                        if (pageContent is System.Windows.Controls.Page page)
                        {
                            if (_pluginSettingsFrame != null)
                            {
                                _pluginSettingsFrame.Navigate(page);
                            }
                        }
                        else if (pageContent is UIElement uiElement)
                        {
                            if (_pluginSettingsFrame != null)
                            {
                                _pluginSettingsFrame.Content = uiElement;
                            }
                        }
                    }
                }
            }
            
            // If plugin doesn't have a settings page, hide the container
            if (!hasSettingsPage)
            {
                // Hide plugin settings container
                if (_pluginSettingsContainer != null)
                {
                    _pluginSettingsContainer.Visibility = Visibility.Collapsed;
                }
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

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

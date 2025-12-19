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

            // Try to get plugin configuration page
            if (plugin is Plugins.SDK.PluginBase sdkPlugin)
            {
                var featureExtension = sdkPlugin.GetFeatureExtension();
                if (featureExtension is Plugins.SDK.IPluginPage pluginPage)
                {
                    // Create and display plugin configuration page
                    var pageContent = pluginPage.CreatePage();
                    
                    // Hide the "no config" message
                    if (_noConfigTextBlock != null)
                    {
                        _noConfigTextBlock.Visibility = Visibility.Collapsed;
                    }
                    
                    if (pageContent is System.Windows.Controls.Page page)
                    {
                        // If it's a Page, use Frame to navigate (Page can only have Window or Frame as parent)
                        if (_pluginConfigFrame != null)
                        {
                            _pluginConfigFrame.Visibility = Visibility.Visible;
                            _pluginConfigFrame.Navigate(page);
                        }
                    }
                    else if (pageContent is UIElement uiElement)
                    {
                        // If it's a UIElement, set it as Frame content
                        if (_pluginConfigFrame != null)
                        {
                            _pluginConfigFrame.Visibility = Visibility.Visible;
                            _pluginConfigFrame.Content = uiElement;
                        }
                    }
                    else if (pageContent != null)
                    {
                        // If it's not a UIElement or Page, show error
                        if (_noConfigTextBlock != null)
                        {
                            _noConfigTextBlock.Text = Resource.PluginSettingsWindow_ConfigFormatError;
                            _noConfigTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                            _noConfigTextBlock.Visibility = Visibility.Visible;
                        }
                    }
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

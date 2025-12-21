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

            // Only show advanced settings for Network Acceleration plugin
            // Don't show plugin's own configuration page
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

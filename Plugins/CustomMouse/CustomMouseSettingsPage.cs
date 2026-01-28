using LenovoLegionToolkit.Plugins.CustomMouse.Services;
using LenovoLegionToolkit.Plugins.SDK;
using System.Windows;
using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.CustomMouse;

/// <summary>
/// Interaction logic for CustomMouseSettingsPage.xaml
/// </summary>
public partial class CustomMouseSettingsPage : UserControl, IPluginPage
{
    private readonly ICustomMouseService _mouseService;
    private readonly CustomMouseSettingsManager _settingsManager;
    private readonly CustomMouseSettings _settings;

    public string PageTitle => "Mouse Settings";
    public string PageIcon => "Settings";

    public CustomMouseSettingsPage()
    {
        InitializeComponent();
        
        // Simplified initialization
        _mouseService = new CustomMouseService();
        _settingsManager = new CustomMouseSettingsManager();
        _settings = _settingsManager.LoadSettings();
        
        DataContext = this;
        InitializeUI();
        RefreshDebugInfo();
    }

    private void InitializeUI()
    {
        try
        {
            // Initialize settings options
            AutoApplyOnStartupCheckBox.IsChecked = _settings.AutoApplyOnStartup;
            ShowNotificationsCheckBox.IsChecked = _settings.ShowNotifications;
            
            // Initialize advanced settings
            BrightnessThresholdSlider.Value = _settings.BrightnessThreshold;
            BrightnessThresholdValue.Text = _settings.BrightnessThreshold.ToString("F1");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize settings page: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshDebugInfo()
    {
        try
        {
            DebugCurrentStyle.Text = _settings.SelectedStyle ?? "W11-CC-V2.2-HDPI";
            DebugThemeMode.Text = _settings.ThemeMode.ToLower() switch
            {
                "auto" => "Auto",
                "light" => "Light",
                "dark" => "Dark",
                _ => _settings.ThemeMode
            };
            DebugPluginMode.Text = _settings.PluginMode.ToLower() switch
            {
                "extension" => "Extension Mode",
                "independent" => "Independent Mode",
                _ => _settings.PluginMode
            };
            DebugLastDetected.Text = _settings.LastDetectedTheme ?? "None";
        }
        catch (Exception ex)
        {
            DebugCurrentStyle.Text = $"Error: {ex.Message}";
        }
    }

    private void Setting_Checked(object sender, RoutedEventArgs e)
    {
        UpdateSetting(true);
    }

    private void Setting_Unchecked(object sender, RoutedEventArgs e)
    {
        UpdateSetting(false);
    }

    private void UpdateSetting(bool isChecked)
    {
        try
        {
            if (sender == AutoApplyOnStartupCheckBox)
            {
                _settings.AutoApplyOnStartup = isChecked;
            }
            else if (sender == ShowNotificationsCheckBox)
            {
                _settings.ShowNotifications = isChecked;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to update settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveSettingsButton.IsEnabled = false;
            
            _settingsManager.SaveSettings(_settings);
            RefreshDebugInfo();
            
            MessageBox.Show("Settings saved", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SaveSettingsButton.IsEnabled = true;
        }
    }

    private async void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
var result = MessageBox.Show("Are you sure you want to reset all settings to default values?\nThis operation cannot be undone.", 
                                        "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
                return;

            ResetSettingsButton.IsEnabled = false;
            
            _settingsManager.ResetToDefault();
            
            // Reload default settings
            var defaultSettings = _settingsManager.LoadSettings();
            
            // Update UI
            AutoApplyOnStartupCheckBox.IsChecked = defaultSettings.AutoApplyOnStartup;
            ShowNotificationsCheckBox.IsChecked = defaultSettings.ShowNotifications;
            
            BrightnessThresholdSlider.Value = defaultSettings.BrightnessThreshold;
            BrightnessThresholdValue.Text = defaultSettings.BrightnessThreshold.ToString("F1");
            
            // Update internal settings
            _settings = defaultSettings;
            
            RefreshDebugInfo();
            
            MessageBox.Show("Settings have been reset to default values", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to reset settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ResetSettingsButton.IsEnabled = true;
        }
    }

    private void AdvancedSetting_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        try
        {
            var value = BrightnessThresholdSlider.Value;
            BrightnessThresholdValue.Text = value.ToString("F1");
            
            _settings.BrightnessThreshold = value;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to update advanced settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
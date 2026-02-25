using LenovoLegionToolkit.Plugins.CustomMouse.Resources;
using LenovoLegionToolkit.Plugins.CustomMouse.Services;
using LenovoLegionToolkit.Plugins.SDK;
using System.Windows;
using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.CustomMouse;

/// <summary>
/// CustomMouseSettingsPage.xaml 的交互逻辑
/// </summary>
public partial class CustomMouseSettingsPage : UserControl, IPluginPage
{
    private readonly ICustomMouseService _mouseService;
    private readonly CustomMouseSettingsManager _settingsManager;
    private readonly CustomMouseSettings _settings;

    public string PageTitle => "鼠标设置";
    public string PageIcon => "Settings";

    public CustomMouseSettingsPage()
    {
        InitializeComponent();
        
        // 简化的服务初始化
        _mouseService = new CustomMouseService();
        _settingsManager = new CustomMouseSettingsManager();
        _settings = _settingsManager.LoadSettings();
        
        DataContext = this;
        InitializeUI();
        RefreshDebugInfo();
    }

    public CustomMouseSettingsPage(ICustomMouseService mouseService, CustomMouseSettingsManager settingsManager, CustomMouseSettings settings)
    {
        InitializeComponent();
        _mouseService = mouseService;
        _settingsManager = settingsManager;
        _settings = settings;
        
        DataContext = this;
        InitializeUI();
        RefreshDebugInfo();
    }

    public object CreatePage() => this;

    private void InitializeUI()
    {
        try
        {
            // 初始化插件模式
            if (_settings.PluginMode == "extension")
            {
                ExtensionModeRadio.IsChecked = true;
            }
            else
            {
                IndependentModeRadio.IsChecked = true;
            }
            UpdatePluginModeDescription();

            // 初始化智能设置
            AutoApplyOnStartupCheckBox.IsChecked = _settings.AutoApplyOnStartup;
            ShowNotificationsCheckBox.IsChecked = _settings.ShowNotifications;
            EnablePreviewCheckBox.IsChecked = _settings.EnablePreview;

            // 初始化高级设置
            BrightnessThresholdSlider.Value = _settings.BrightnessThreshold;
            BrightnessThresholdValue.Text = _settings.BrightnessThreshold.ToString("F1");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"初始化设置页面失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdatePluginModeDescription()
    {
        try
        {
            var isExtension = ExtensionModeRadio.IsChecked == true;
            PluginModeDescription.Text = isExtension 
                ? Resource.CustomMouse_ExtensionMode_Description 
                : Resource.CustomMouse_IndependentMode_Description;
        }
        catch
        {
            PluginModeDescription.Text = "插件模式描述";
        }
    }

    private void RefreshDebugInfo()
    {
        try
        {
            DebugCurrentStyle.Text = _settings.SelectedStyle ?? "-";
            
            DebugThemeMode.Text = _settings.ThemeMode.ToLower() switch
            {
                "auto" => "自动",
                "light" => "浅色",
                "dark" => "深色",
                _ => _settings.ThemeMode
            };
            
            DebugPluginMode.Text = _settings.PluginMode.ToLower() switch
            {
                "extension" => "扩展模式",
                "independent" => "独立模式",
                _ => _settings.PluginMode
            };
            
            DebugLastDetected.Text = _settings.LastDetectedTheme ?? "-";
        }
        catch (Exception ex)
        {
            DebugCurrentStyle.Text = $"错误: {ex.Message}";
        }
    }

    private void PluginMode_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is RadioButton radio && radio.Tag is string mode)
            {
                _settings.PluginMode = mode;
                UpdatePluginModeDescription();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"插件模式设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            else if (sender == EnablePreviewCheckBox)
            {
                _settings.EnablePreview = isChecked;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"设置更新失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveSettingsButton.IsEnabled = false;
            
            _settingsManager.SaveSettings(_settings);
            RefreshDebugInfo();
            
            MessageBox.Show("设置已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            var result = MessageBox.Show("确定要重置所有设置为默认值吗？\n此操作无法撤销。", 
                                       "确认重置", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
                return;

            ResetSettingsButton.IsEnabled = false;
            
            _settingsManager.ResetToDefault();
            
            // 重新加载默认设置
            var defaultSettings = _settingsManager.LoadSettings();
            
            // 更新UI
            ExtensionModeRadio.IsChecked = defaultSettings.PluginMode == "extension";
            IndependentModeRadio.IsChecked = defaultSettings.PluginMode == "independent";
            
            AutoApplyOnStartupCheckBox.IsChecked = defaultSettings.AutoApplyOnStartup;
            ShowNotificationsCheckBox.IsChecked = defaultSettings.ShowNotifications;
            EnablePreviewCheckBox.IsChecked = defaultSettings.EnablePreview;
            
            BrightnessThresholdSlider.Value = defaultSettings.BrightnessThreshold;
            BrightnessThresholdValue.Text = defaultSettings.BrightnessThreshold.ToString("F1");
            
            // 更新内部设置
            _settings.AutoColorDetectionEnabled = defaultSettings.AutoColorDetectionEnabled;
            _settings.SelectedStyle = defaultSettings.SelectedStyle;
            _settings.ThemeMode = defaultSettings.ThemeMode;
            _settings.BrightnessThreshold = defaultSettings.BrightnessThreshold;
            _settings.PluginMode = defaultSettings.PluginMode;
            _settings.EnablePreview = defaultSettings.EnablePreview;
            _settings.AutoApplyOnStartup = defaultSettings.AutoApplyOnStartup;
            _settings.LastDetectedTheme = defaultSettings.LastDetectedTheme;
            _settings.ShowNotifications = defaultSettings.ShowNotifications;
            
            UpdatePluginModeDescription();
            RefreshDebugInfo();
            
            MessageBox.Show("设置已重置为默认值", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"重置设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show($"高级设置更新失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
using LenovoLegionToolkit.Plugins.CustomMouse.Resources;
using LenovoLegionToolkit.Plugins.CustomMouse.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.CustomMouse;

/// <summary>
/// CustomMousePage.xaml 的交互逻辑
/// </summary>
public partial class CustomMousePage : UserControl
{
    private readonly ICustomMouseService _mouseService;
    private readonly CustomMouseSettingsManager _settingsManager;
    private readonly CustomMouseSettings _settings;
    private readonly List<MouseStyleInfo> _availableStyles = new();

    public CustomMousePage(ICustomMouseService mouseService, CustomMouseSettingsManager settingsManager, CustomMouseSettings settings)
    {
        InitializeComponent();
        _mouseService = mouseService;
        _settingsManager = settingsManager;
        _settings = settings;
        
        DataContext = this;
        InitializeUI();
        LoadAvailableStyles();
    }

    private void InitializeUI()
    {
        try
        {
            // 初始化模式选择
            if (_settings.PluginMode == "extension")
            {
                ExtensionModeRadio.IsChecked = true;
            }
            else
            {
                IndependentModeRadio.IsChecked = true;
            }
            UpdateModeDescription();

            // 初始化设置选项
            AutoColorDetectionCheckBox.IsChecked = _settings.AutoColorDetectionEnabled;
            
            // 设置主题模式
            foreach (ComboBoxItem item in ThemeModeComboBox.Items)
            {
                if (item.Tag?.ToString() == _settings.ThemeMode)
                {
                    ThemeModeComboBox.SelectedItem = item;
                    break;
                }
            }

            // 设置亮度阈值
            BrightnessThresholdSlider.Value = _settings.BrightnessThreshold;
            BrightnessThresholdValue.Text = _settings.BrightnessThreshold.ToString("F1");

            // 更新当前状态显示
            UpdateCurrentStatus();
        }
        catch (Exception ex)
        {
            UpdateStatus($"初始化错误: {ex.Message}", true);
        }
    }

    private async void LoadAvailableStyles()
    {
        try
        {
            UpdateStatus("正在加载可用样式...");
            
            var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "CustomMouse");
            var resourcesDir = Path.Combine(pluginDir, "Resources");
            
            if (Directory.Exists(resourcesDir))
            {
                _availableStyles.Clear();
                var styles = await _mouseService.GetAvailableStylesAsync(resourcesDir);
                _availableStyles.AddRange(styles);
                
                // 更新样式列表
                StylesListBox.ItemsSource = _availableStyles.Select(s => $"{s.Name} ({s.Theme})").ToList();
                
                // 选择当前样式
                for (int i = 0; i < _availableStyles.Count; i++)
                {
                    if (_availableStyles[i].Name == _settings.SelectedStyle)
                    {
                        StylesListBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            
            UpdateStatus($"找到 {_availableStyles.Count} 个可用样式");
        }
        catch (Exception ex)
        {
            UpdateStatus($"加载样式失败: {ex.Message}", true);
        }
    }

    private void UpdateCurrentStatus()
    {
        try
        {
            CurrentStyleText.Text = _settings.SelectedStyle;
            
            var themeText = _settings.ThemeMode.ToLower() switch
            {
                "auto" => "自动",
                "light" => "浅色",
                "dark" => "深色",
                _ => "未知"
            };
            
            if (_settings.AutoColorDetectionEnabled)
            {
                themeText += $" (上次检测: {_settings.LastDetectedTheme ?? "无"})";
            }
            
            CurrentThemeText.Text = $"主题：{themeText}";
        }
        catch (Exception ex)
        {
            UpdateStatus($"更新状态失败: {ex.Message}", true);
        }
    }

    private void UpdateModeDescription()
    {
        try
        {
            var isExtension = ExtensionModeRadio.IsChecked == true;
            ModeDescriptionText.Text = isExtension 
                ? Resource.CustomMouse_ExtensionMode_Description 
                : Resource.CustomMouse_IndependentMode_Description;
        }
        catch
        {
            ModeDescriptionText.Text = "插件模式描述";
        }
    }

    private void UpdateStatus(string message, bool isError = false)
    {
        try
        {
            StatusText.Text = message;
            StatusText.Opacity = isError ? 1.0 : 0.7;
            
            if (isError)
            {
                StatusText.Foreground = FindResource("SystemControlForegroundAccentBrush") as Brush;
            }
            else
            {
                StatusText.Foreground = FindResource("SystemControlForegroundBaseMediumBrush") as Brush;
            }
        }
        catch
        {
            // 忽略UI更新错误
        }
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyButton.IsEnabled = false;
            UpdateStatus("正在应用鼠标样式...");

            var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "CustomMouse");
            var stylePath = Path.Combine(pluginDir, "Resources", _settings.SelectedStyle);

            if (!Directory.Exists(stylePath))
            {
                UpdateStatus("样式目录不存在", true);
                return;
            }

            string theme = _settings.ThemeMode;
            
            // 如果启用自动颜色检测
            if (_settings.AutoColorDetectionEnabled && theme == "auto")
            {
                UpdateStatus("正在检测背景颜色...");
                var isLightBackground = await _mouseService.IsLightBackgroundAsync();
                theme = isLightBackground ? "light" : "dark";
                
                // 记录检测到的主题
                _settings.LastDetectedTheme = theme;
                _settingsManager.SaveSettings(_settings);
                
                UpdateStatus(isLightBackground ? "检测到浅色背景" : "检测到深色背景");
            }

            await _mouseService.ApplyMouseStyleAsync(stylePath, theme);
            
            UpdateCurrentStatus();
            UpdateStatus("鼠标样式应用成功");
            
            if (_settings.ShowNotifications)
            {
                // 这里可以显示通知
                MessageBox.Show("鼠标样式已成功应用", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"应用失败: {ex.Message}", true);
            
            if (_settings.ShowNotifications)
            {
                MessageBox.Show($"应用鼠标样式失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            ApplyButton.IsEnabled = true;
        }
    }

    private async void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RestoreButton.IsEnabled = false;
            UpdateStatus("正在恢复默认样式...");

            await _mouseService.RestoreDefaultMouseStyleAsync();
            
            UpdateStatus("已恢复默认鼠标样式");
            
            if (_settings.ShowNotifications)
            {
                MessageBox.Show("已恢复默认鼠标样式", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"恢复失败: {ex.Message}", true);
            
            if (_settings.ShowNotifications)
            {
                MessageBox.Show($"恢复默认样式失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            RestoreButton.IsEnabled = true;
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadAvailableStyles();
    }

    private void ModeRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is RadioButton radio && radio.Tag is string mode)
            {
                _settings.PluginMode = mode;
                _settingsManager.SaveSettings(_settings);
                UpdateModeDescription();
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"模式切换失败: {ex.Message}", true);
        }
    }

    private void AutoColorDetectionCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings.AutoColorDetectionEnabled = true;
            _settingsManager.SaveSettings(_settings);
            UpdateCurrentStatus();
        }
        catch (Exception ex)
        {
            UpdateStatus($"设置更新失败: {ex.Message}", true);
        }
    }

    private void AutoColorDetectionCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings.AutoColorDetectionEnabled = false;
            _settingsManager.SaveSettings(_settings);
            UpdateCurrentStatus();
        }
        catch (Exception ex)
        {
            UpdateStatus($"设置更新失败: {ex.Message}", true);
        }
    }

    private void ThemeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (ThemeModeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string theme)
            {
                _settings.ThemeMode = theme;
                _settingsManager.SaveSettings(_settings);
                UpdateCurrentStatus();
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"主题模式设置失败: {ex.Message}", true);
        }
    }

    private void BrightnessThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        try
        {
            var value = BrightnessThresholdSlider.Value;
            BrightnessThresholdValue.Text = value.ToString("F1");
            
            _settings.BrightnessThreshold = value;
            _settingsManager.SaveSettings(_settings);
        }
        catch (Exception ex)
        {
            UpdateStatus($"亮度阈值设置失败: {ex.Message}", true);
        }
    }

    private void StylesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (StylesListBox.SelectedIndex >= 0 && StylesListBox.SelectedIndex < _availableStyles.Count)
            {
                var selectedStyle = _availableStyles[StylesListBox.SelectedIndex];
                _settings.SelectedStyle = selectedStyle.Name;
                _settingsManager.SaveSettings(_settings);
                UpdateCurrentStatus();
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"样式选择失败: {ex.Message}", true);
        }
    }
}
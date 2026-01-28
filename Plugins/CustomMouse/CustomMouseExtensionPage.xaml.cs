using LenovoLegionToolkit.Plugins.CustomMouse.Resources;
using LenovoLegionToolkit.Plugins.CustomMouse.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace LenovoLegionToolkit.Plugins.CustomMouse;

/// <summary>
/// CustomMouseExtensionPage.xaml 的交互逻辑
/// </summary>
public partial class CustomMouseExtensionPage : UserControl
{
    private readonly ICustomMouseService _mouseService;
    private readonly CustomMouseSettingsManager _settingsManager;
    private readonly CustomMouseSettings _settings;
    private bool _isCheckingStatus = false;

    public CustomMouseExtensionPage(ICustomMouseService mouseService, CustomMouseSettingsManager settingsManager, CustomMouseSettings settings)
    {
        InitializeComponent();
        _mouseService = mouseService;
        _settingsManager = settingsManager;
        _settings = settings;
        
        DataContext = this;
        
        Loaded += CustomMouseExtensionPage_Loaded;
        _ = CheckStatusAsync();
    }

    private async void CustomMouseExtensionPage_Loaded(object sender, RoutedEventArgs e)
    {
        await CheckStatusAsync();
    }

    private async Task CheckStatusAsync()
    {
        if (_isCheckingStatus)
            return;

        _isCheckingStatus = true;
        
        try
        {
            await Dispatcher.InvokeAsync(() =>
            {
                StatusText.Text = "正在检查状态...";
                StatusIndicator.Fill = new SolidColorBrush(Colors.Orange);
            }, DispatcherPriority.Background);

            var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "CustomMouse");
            var stylePath = Path.Combine(pluginDir, "Resources", "W11-CC-V2.2-HDPI");

            if (!Directory.Exists(stylePath))
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    StatusText.Text = "样式文件不存在";
                    StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                }, DispatcherPriority.Background);
                return;
            }

            var isApplied = await _mouseService.IsMouseStyleAppliedAsync(stylePath);
            
            await Dispatcher.InvokeAsync(() =>
            {
                if (isApplied)
                {
                    StatusText.Text = "已应用";
                    StatusIndicator.Fill = new SolidColorBrush(Colors.LimeGreen);
                    ApplyButton.IsEnabled = false;
                    RemoveButton.IsEnabled = true;
                }
                else
                {
                    StatusText.Text = "未应用";
                    StatusIndicator.Fill = new SolidColorBrush(Colors.Gray);
                    ApplyButton.IsEnabled = true;
                    RemoveButton.IsEnabled = false;
                }
            }, DispatcherPriority.Background);
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                StatusText.Text = $"检查失败: {ex.Message}";
                StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                ApplyButton.IsEnabled = true;
                RemoveButton.IsEnabled = true;
            }, DispatcherPriority.Background);
        }
        finally
        {
            _isCheckingStatus = false;
        }
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyButton.IsEnabled = false;
            StatusText.Text = "正在应用...";
            StatusIndicator.Fill = new SolidColorBrush(Colors.Orange);

            var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "CustomMouse");
            var stylePath = Path.Combine(pluginDir, "Resources", "W11-CC-V2.2-HDPI");

            if (!Directory.Exists(stylePath))
            {
                StatusText.Text = "样式文件不存在";
                StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                ApplyButton.IsEnabled = true;
                return;
            }

            string theme = _settings.ThemeMode;
            
            // 如果启用自动颜色检测
            if (_settings.AutoColorDetectionEnabled && theme == "auto")
            {
                StatusText.Text = "正在检测主题...";
                var isLightBackground = await _mouseService.IsLightBackgroundAsync();
                theme = isLightBackground ? "light" : "dark";
                
                // 记录检测到的主题
                _settings.LastDetectedTheme = theme;
                _settingsManager.SaveSettings(_settings);
            }

            await _mouseService.ApplyMouseStyleAsync(stylePath, theme);

            StatusText.Text = "已应用";
            StatusIndicator.Fill = new SolidColorBrush(Colors.LimeGreen);
            ApplyButton.IsEnabled = false;
            RemoveButton.IsEnabled = true;

            if (_settings.ShowNotifications)
            {
                MessageBox.Show("鼠标样式已成功应用", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"应用失败: {ex.Message}";
            StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
            ApplyButton.IsEnabled = true;
            
            if (_settings.ShowNotifications)
            {
                MessageBox.Show($"应用鼠标样式失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RemoveButton.IsEnabled = false;
            StatusText.Text = "正在移除...";
            StatusIndicator.Fill = new SolidColorBrush(Colors.Orange);

            await _mouseService.RestoreDefaultMouseStyleAsync();

            StatusText.Text = "已移除";
            StatusIndicator.Fill = new SolidColorBrush(Colors.Gray);
            ApplyButton.IsEnabled = true;
            RemoveButton.IsEnabled = false;

            if (_settings.ShowNotifications)
            {
                MessageBox.Show("鼠标样式已移除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"移除失败: {ex.Message}";
            StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
            RemoveButton.IsEnabled = true;
            
            if (_settings.ShowNotifications)
            {
                MessageBox.Show($"移除鼠标样式失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// 刷新状态，供外部调用
    /// </summary>
    public async Task RefreshStatusAsync()
    {
        await CheckStatusAsync();
    }
}
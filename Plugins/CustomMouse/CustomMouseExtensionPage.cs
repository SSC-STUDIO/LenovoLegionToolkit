using LenovoLegionToolkit.Plugins.CustomMouse.Services;
using LenovoLegionToolkit.Plugins.SDK;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.CustomMouse;

/// <summary>
/// CustomMouseExtensionPage.xaml 的交互逻辑
/// </summary>
public partial class CustomMouseExtensionPage : UserControl, IPluginPage
{
    private readonly ICustomMouseService _mouseService;
    private bool _isCheckingStatus = false;

    public string PageTitle => "W11-CC-V2.2-HDPI 鼠标样式";
    public string PageIcon => "Mouse";

    public CustomMouseExtensionPage()
    {
        InitializeComponent();
        
        // 简化的服务初始化
        _mouseService = new CustomMouseService();
        
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
            });

            var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "CustomMouse");
            var stylePath = Path.Combine(pluginDir, "Resources", "W11-CC-V2.2-HDPI");

            if (!Directory.Exists(stylePath))
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    StatusText.Text = "样式文件不存在";
                    StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                });
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
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                StatusText.Text = $"检查失败: {ex.Message}";
                StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                ApplyButton.IsEnabled = true;
                RemoveButton.IsEnabled = true;
            });
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

            await _mouseService.ApplyMouseStyleAsync(stylePath, "auto");

            StatusText.Text = "已应用";
            StatusIndicator.Fill = new SolidColorBrush(Colors.LimeGreen);
            ApplyButton.IsEnabled = false;
            RemoveButton.IsEnabled = true;

            MessageBox.Show("鼠标样式已成功应用", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"应用失败: {ex.Message}";
            StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
            ApplyButton.IsEnabled = true;
            
            MessageBox.Show($"应用鼠标样式失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

            MessageBox.Show("鼠标样式已移除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"移除失败: {ex.Message}";
            StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
            RemoveButton.IsEnabled = true;
            
            MessageBox.Show($"移除鼠标样式失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
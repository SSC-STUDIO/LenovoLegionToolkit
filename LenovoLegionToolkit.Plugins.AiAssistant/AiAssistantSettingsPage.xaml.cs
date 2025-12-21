using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.AiAssistant.Services;
using LenovoLegionToolkit.Plugins.AiAssistant.Services.Ollama;
using LenovoLegionToolkit.Plugins.AiAssistant.Services.Settings;
using Wpf.Ui.Controls;
using System.Windows.Media;

namespace LenovoLegionToolkit.Plugins.AiAssistant;

/// <summary>
/// AI Assistant Settings Page - Configuration page for AI Assistant plugin
/// </summary>
public partial class AiAssistantSettingsPage
{
    private readonly AiAssistantSettings _settings;
    private IAiService? _aiService;
    private readonly OllamaServiceManager _ollamaServiceManager = new();
    private CancellationTokenSource? _downloadCancellationTokenSource;

    public AiAssistantSettingsPage()
    {
        InitializeComponent();
        _settings = new AiAssistantSettings();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _settings.LoadAsync();
            
            // Update UI from settings (this will set ComboBox and update visibility)
            UpdateUIFromSettings();
            
            // Force visibility update after a short delay to ensure UI is fully initialized
            // This is important because UpdateUIFromSettings might be called before UI is fully initialized
            await Task.Delay(50); // Small delay to ensure UI elements are ready
            UpdateProviderSettingsVisibility();
            
            // Initialize service after UI is updated
            await InitializeAiService();
            UpdateConnectionStatus();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading AI Assistant settings: {ex.Message}", ex);
        }
    }

    private void UpdateUIFromSettings()
    {
        // Temporarily disable SelectionChanged event to avoid triggering it during initialization
        _providerComboBox.SelectionChanged -= ProviderComboBox_SelectionChanged;
        
        try
        {
            _providerComboBox.SelectedIndex = _settings.SelectedProvider switch
            {
                AiProvider.OpenAI => 0,
                AiProvider.Ollama => 1,
                AiProvider.DeepSeek => 2,
                _ => 0
            };
            
            _openAiApiKeyBox.Password = _settings.OpenAiApiKey;
            _openAiModelBox.Text = _settings.OpenAiModel;
            _deepSeekApiKeyBox.Password = _settings.DeepSeekApiKey;
            _deepSeekModelBox.Text = _settings.DeepSeekModel;
            _ollamaBaseUrlBox.Text = _settings.OllamaBaseUrl;
            _ollamaModelBox.Text = _settings.OllamaModel;
            _autoStartOllamaToggle.IsChecked = _settings.AutoStartOllama;
            _useBuiltInOllamaToggle.IsChecked = _settings.UseBuiltInOllama;
            _ollamaExecutablePathBox.Text = _settings.OllamaExecutablePath ?? string.Empty;
            
            // Auto-detect Ollama path if not set and not using built-in
            if (!_settings.UseBuiltInOllama && string.IsNullOrEmpty(_settings.OllamaExecutablePath))
            {
                var detectedPath = OllamaServiceManager.FindOllamaExecutable();
                if (!string.IsNullOrEmpty(detectedPath))
                {
                    _ollamaExecutablePathBox.Text = detectedPath;
                    _settings.OllamaExecutablePath = detectedPath;
                }
            }
            
            // Update visibility after all UI elements are set
            UpdateProviderSettingsVisibility();
        }
        finally
        {
            // Re-enable SelectionChanged event
            _providerComboBox.SelectionChanged += ProviderComboBox_SelectionChanged;
        }
    }

    private void UpdateProviderSettingsVisibility()
    {
        if (_providerComboBox == null || _openAiSettingsPanel == null || _deepSeekSettingsPanel == null || _ollamaSettingsPanel == null)
            return;

        var selectedIndex = _providerComboBox.SelectedIndex;
        var isOpenAI = selectedIndex == 0;
        var isDeepSeek = selectedIndex == 2;
        var isOllama = selectedIndex == 1;
        
        // Update visibility for all provider panels
        _openAiSettingsPanel.Visibility = isOpenAI ? Visibility.Visible : Visibility.Collapsed;
        _deepSeekSettingsPanel.Visibility = isDeepSeek ? Visibility.Visible : Visibility.Collapsed;
        _ollamaSettingsPanel.Visibility = isOllama ? Visibility.Visible : Visibility.Collapsed;
        
        // Show/hide external Ollama path based on built-in toggle (only when Ollama is selected)
        if (_externalOllamaPathPanel != null && isOllama)
        {
            var useBuiltIn = _useBuiltInOllamaToggle?.IsChecked == true;
            _externalOllamaPathPanel.Visibility = useBuiltIn ? Visibility.Collapsed : Visibility.Visible;
        }
        else if (_externalOllamaPathPanel != null)
        {
            _externalOllamaPathPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateConnectionStatus()
    {
        if (_aiService != null)
        {
            _connectionStatusText.Text = Resource.AiAssistant_ConnectionStatusConnected;
        }
        else
        {
            _connectionStatusText.Text = Resource.AiAssistant_ConnectionStatusDisconnected;
        }
    }

    private async void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_settings == null || _providerComboBox.SelectedIndex < 0) return;

        _settings.SelectedProvider = _providerComboBox.SelectedIndex switch
        {
            0 => AiProvider.OpenAI,
            1 => AiProvider.Ollama,
            2 => AiProvider.DeepSeek,
            _ => AiProvider.OpenAI
        };
        
        // Update visibility first
        UpdateProviderSettingsVisibility();
        
        // Notify main page to update AI name if it's loaded
        // This is done through settings change, which will be picked up on next service initialization
        
        // Then initialize service
        await InitializeAiService();
        UpdateConnectionStatus();
    }

    private void OpenAiApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_settings != null)
            _settings.OpenAiApiKey = _openAiApiKeyBox.Password;
    }

    private void OpenAiModelBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_settings != null)
            _settings.OpenAiModel = _openAiModelBox.Text;
    }

    private void DeepSeekApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_settings != null)
            _settings.DeepSeekApiKey = _deepSeekApiKeyBox.Password;
    }

    private void DeepSeekModelBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_settings != null)
            _settings.DeepSeekModel = _deepSeekModelBox.Text;
    }

    private void OllamaBaseUrlBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_settings != null)
            _settings.OllamaBaseUrl = _ollamaBaseUrlBox.Text;
    }

    private void OllamaModelBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_settings != null)
            _settings.OllamaModel = _ollamaModelBox.Text;
    }

    private void AutoStartOllamaToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (_settings != null)
            _settings.AutoStartOllama = true;
    }

    private void AutoStartOllamaToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_settings != null)
            _settings.AutoStartOllama = false;
    }

    private void UseBuiltInOllamaToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (_settings != null)
        {
            _settings.UseBuiltInOllama = true;
            UpdateProviderSettingsVisibility();
        }
    }

    private void UseBuiltInOllamaToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_settings != null)
        {
            _settings.UseBuiltInOllama = false;
            UpdateProviderSettingsVisibility();
        }
    }

    private void OllamaExecutablePathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_settings != null)
            _settings.OllamaExecutablePath = _ollamaExecutablePathBox.Text;
    }

    private void BrowseOllamaExecutableButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = Resource.AiAssistant_BrowseOllamaExecutable,
            Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
            FileName = _ollamaExecutablePathBox.Text
        };

        if (dialog.ShowDialog() == true)
        {
            _ollamaExecutablePathBox.Text = dialog.FileName;
            if (_settings != null)
                _settings.OllamaExecutablePath = dialog.FileName;
        }
    }

    private Task InitializeAiService()
    {
        try
        {
            _aiService = AiServiceFactory.CreateService(_settings.SelectedProvider, _settings);
            UpdateConnectionStatus();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error initializing AI service: {ex.Message}", ex);
            _aiService = null;
            UpdateConnectionStatus();
        }
        return Task.CompletedTask;
    }

    private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
    {
        _testConnectionButton.IsEnabled = false;
        _connectionStatusText.Text = Resource.AiAssistant_Status_Processing;

        try
        {
            // If using Ollama and auto-start is enabled, ensure service is running
            if (_settings.SelectedProvider == AiProvider.Ollama && _settings.AutoStartOllama)
            {
                _connectionStatusText.Text = Resource.AiAssistant_Status_StartingOllama;
                try
                {
                    // Use built-in Ollama if enabled, otherwise use external path
                    string? ollamaPath = null;
                    if (!_settings.UseBuiltInOllama)
                    {
                        ollamaPath = _settings.OllamaExecutablePath;
                    }
                    
                    var started = await _ollamaServiceManager.EnsureServiceRunningAsync(
                        ollamaPath,
                        _settings.AutoStartOllama);
                    
                    if (!started)
                    {
                        _connectionStatusText.Text = Resource.AiAssistant_Error_OllamaStartFailed;
                    System.Windows.MessageBox.Show(
                        Resource.AiAssistant_Error_OllamaStartFailedDescription,
                        Resource.AiAssistant_Error_OllamaStartFailed,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                        _testConnectionButton.IsEnabled = true;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _connectionStatusText.Text = Resource.AiAssistant_Error_OllamaStartFailed;
                    System.Windows.MessageBox.Show(
                        $"{Resource.AiAssistant_Error_OllamaStartFailedDescription}\n{ex.Message}",
                        Resource.AiAssistant_Error_OllamaStartFailed,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    _testConnectionButton.IsEnabled = true;
                    return;
                }
            }

            await InitializeAiService();
            if (_aiService != null)
            {
                var isConnected = await _aiService.TestConnectionAsync();
                _connectionStatusText.Text = isConnected ? Resource.AiAssistant_ConnectionStatusConnected : Resource.AiAssistant_ConnectionStatusDisconnected;
            }
            else
            {
                _connectionStatusText.Text = Resource.AiAssistant_ConnectionStatusDisconnected;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error testing connection: {ex.Message}", ex);
            _connectionStatusText.Text = Resource.AiAssistant_ConnectionStatusDisconnected;
            System.Windows.MessageBox.Show(
                $"Connection test failed: {ex.Message}",
                Resource.AiAssistant_Error_ConnectionFailed,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            _testConnectionButton.IsEnabled = true;
        }
    }

    private async void DownloadOllamaButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Check if already downloaded
            if (OllamaDownloader.IsOllamaDownloaded())
            {
                System.Windows.MessageBox.Show(
                    "Ollama 已经下载完成。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Cancel previous download if any
            if (_downloadCancellationTokenSource != null)
            {
                await _downloadCancellationTokenSource.CancelAsync();
                _downloadCancellationTokenSource.Dispose();
            }

            _downloadCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _downloadCancellationTokenSource.Token;

            // Show progress UI
            if (_downloadOllamaButton != null)
            {
                _downloadOllamaButton.IsEnabled = false;
                _downloadOllamaButton.Visibility = Visibility.Collapsed;
            }

            if (_downloadProgressPanel != null)
            {
                _downloadProgressPanel.Visibility = Visibility.Visible;
            }

            if (_downloadProgressBar != null)
            {
                _downloadProgressBar.Value = 0;
                _downloadProgressBar.IsIndeterminate = true;
            }

            if (_downloadSpeedText != null)
            {
                _downloadSpeedText.Text = "准备下载...";
            }

            if (_downloadTimeRemainingText != null)
            {
                _downloadTimeRemainingText.Text = "";
            }

            // Download with progress tracking
            var startTime = DateTime.Now;
            long lastBytes = 0;
            var lastUpdateTime = startTime;

            var progress = new Progress<float>(progressValue =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                Dispatcher.Invoke(() =>
                {
                    if (_downloadProgressBar != null)
                    {
                        _downloadProgressBar.IsIndeterminate = false;
                        _downloadProgressBar.Value = progressValue * 100;
                    }

                    // Calculate download speed (simplified - actual implementation would track bytes)
                    var currentTime = DateTime.Now;
                    var elapsed = (currentTime - lastUpdateTime).TotalSeconds;
                    
                    if (elapsed >= 1.0) // Update every second
                    {
                        // Estimate speed based on progress (this is simplified)
                        // In a real implementation, we'd track actual bytes downloaded
                        var estimatedTotalBytes = 100L * 1024 * 1024; // Assume ~100MB
                        var currentBytes = (long)(progressValue * estimatedTotalBytes);
                        var bytesPerSecond = (currentBytes - lastBytes) / elapsed;
                        var speedMBps = bytesPerSecond / (1024.0 * 1024.0);

                        if (_downloadSpeedText != null && speedMBps > 0)
                        {
                            _downloadSpeedText.Text = $"速度: {speedMBps:F2} MB/s";
                        }

                        // Calculate remaining time
                        if (progressValue > 0 && progressValue < 1 && speedMBps > 0)
                        {
                            var remainingBytes = estimatedTotalBytes - currentBytes;
                            var remainingSeconds = remainingBytes / (speedMBps * 1024 * 1024);
                            
                            if (_downloadTimeRemainingText != null)
                            {
                                if (remainingSeconds < 60)
                                {
                                    _downloadTimeRemainingText.Text = $"剩余: {remainingSeconds:F0}秒";
                                }
                                else
                                {
                                    var minutes = (int)(remainingSeconds / 60);
                                    var seconds = (int)(remainingSeconds % 60);
                                    _downloadTimeRemainingText.Text = $"剩余: {minutes}分{seconds}秒";
                                }
                            }
                        }

                        lastBytes = currentBytes;
                        lastUpdateTime = currentTime;
                    }
                });
            });

            var ollamaPath = await OllamaDownloader.DownloadOllamaAsync(progress, cancellationToken);

            // Update settings to use built-in Ollama
            _settings.UseBuiltInOllama = true;
            await _settings.SaveAsync();

            // Update UI
            _useBuiltInOllamaToggle.IsChecked = true;
            UpdateProviderSettingsVisibility();

            // Hide progress UI
            if (_downloadProgressPanel != null)
            {
                _downloadProgressPanel.Visibility = Visibility.Collapsed;
            }

            if (_downloadOllamaButton != null)
            {
                _downloadOllamaButton.Visibility = Visibility.Visible;
            }

            System.Windows.MessageBox.Show(
                $"Ollama 下载完成！\n路径: {ollamaPath}",
                "下载成功",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            // Download was cancelled
            if (_downloadProgressPanel != null)
            {
                _downloadProgressPanel.Visibility = Visibility.Collapsed;
            }

            if (_downloadOllamaButton != null)
            {
                _downloadOllamaButton.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"下载 Ollama 失败: {ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Hide progress UI on error
            if (_downloadProgressPanel != null)
            {
                _downloadProgressPanel.Visibility = Visibility.Collapsed;
            }

            if (_downloadOllamaButton != null)
            {
                _downloadOllamaButton.Visibility = Visibility.Visible;
            }
        }
        finally
        {
            if (_downloadOllamaButton != null)
            {
                _downloadOllamaButton.IsEnabled = true;
            }

            _downloadCancellationTokenSource?.Dispose();
            _downloadCancellationTokenSource = null;
        }
    }

    private void CancelDownloadButton_Click(object sender, RoutedEventArgs e)
    {
        _downloadCancellationTokenSource?.Cancel();
    }
}


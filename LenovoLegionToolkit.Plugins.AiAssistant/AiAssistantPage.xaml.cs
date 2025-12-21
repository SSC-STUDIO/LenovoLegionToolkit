using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.AiAssistant.Models;
using LenovoLegionToolkit.Plugins.AiAssistant.Services;
using LenovoLegionToolkit.Plugins.AiAssistant.Services.Ollama;
using LenovoLegionToolkit.Plugins.AiAssistant.Services.Settings;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.Plugins.AiAssistant;

/// <summary>
/// AI Assistant Page - AI-powered assistant with multiple features
/// </summary>
public partial class AiAssistantPage : INotifyPropertyChanged
{
    private string _connectionStatus = string.Empty;
    private bool _isProcessing;
    private string _greetingMessage = string.Empty;
    private string _userName = string.Empty;
    private string _aiName = "助手";

    private readonly AiAssistantSettings _settings;
    private IAiService? _aiService;
    private CancellationTokenSource? _currentCancellationTokenSource;
    private readonly OllamaServiceManager _ollamaServiceManager = new();
    private static readonly string ChatHistoryFilePath = Path.Combine(
        Folders.AppData,
        "plugins",
        "ai-assistant",
        "chat-history.json");
    private static readonly string SessionsFilePath = Path.Combine(
        Folders.AppData,
        "plugins",
        "ai-assistant",
        "sessions.json");

    public ObservableCollection<ChatMessage> ChatHistory { get; } = new();
    public ObservableCollection<ChatSession> ChatSessions { get; } = new();
    public ObservableCollection<PromptTemplate> PromptTemplates { get; } = new();
    
    // Grouped sessions by time period
    public ObservableCollection<SessionGroup> SessionGroups { get; } = new();
    
    private ChatSession? _currentSession;
    
    public ChatSession? CurrentSession
    {
        get => _currentSession;
        set
        {
            if (_currentSession != value)
            {
                _currentSession = value;
                OnPropertyChanged();
                if (_currentSession != null)
                {
                    ChatHistory.Clear();
                    foreach (var message in _currentSession.Messages)
                    {
                        ChatHistory.Add(message);
                    }
                    ScrollChatToBottom();
                }
            }
        }
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set
        {
            if (_connectionStatus != value)
            {
                _connectionStatus = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            if (_isProcessing != value)
            {
                _isProcessing = value;
                OnPropertyChanged();
            }
        }
    }

    public string GreetingMessage
    {
        get => _greetingMessage;
        set
        {
            if (_greetingMessage != value)
            {
                _greetingMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public string UserName
    {
        get => _userName;
        set
        {
            if (_userName != value)
            {
                _userName = value;
                OnPropertyChanged();
            }
        }
    }

    public string AiName
    {
        get => _aiName;
        set
        {
            if (_aiName != value)
            {
                _aiName = value;
                OnPropertyChanged();
            }
        }
    }

    public AiAssistantPage()
    {
        InitializeComponent();
        _settings = new AiAssistantSettings();
        DataContext = this;
        ConnectionStatus = Resource.AiAssistant_ConnectionStatusDisconnected;
        UpdateGreetingMessage();
        UserName = Environment.UserName;
    }

    private void UpdateGreetingMessage()
    {
        var hour = DateTime.Now.Hour;
        string greeting;
        
        if (hour >= 5 && hour < 12)
        {
            greeting = Resource.AiAssistant_GoodMorning;
        }
        else if (hour >= 12 && hour < 18)
        {
            greeting = Resource.AiAssistant_GoodAfternoon;
        }
        else if (hour >= 18 && hour < 22)
        {
            greeting = Resource.AiAssistant_GoodEvening;
        }
        else
        {
            greeting = Resource.AiAssistant_GoodNight;
        }
        
        GreetingMessage = greeting;
    }

    private void UpdateAiName()
    {
        AiName = _settings.SelectedProvider switch
        {
            AiProvider.OpenAI => "ChatGPT",
            AiProvider.DeepSeek => "DeepSeek",
            AiProvider.Ollama => "Ollama",
            _ => "助手"
        };
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _settings.LoadAsync();
            await InitializeAiService();
            await LoadChatHistoryAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading AI Assistant page: {ex.Message}", ex);
        }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Save chat history before unloading
            _ = SaveChatHistoryAsync();
            
            // Cancel and dispose cancellation token source safely
            if (_currentCancellationTokenSource != null)
            {
                try
                {
                    if (!_currentCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        _currentCancellationTokenSource.Cancel();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Already disposed, ignore
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error cancelling token: {ex.Message}", ex);
                }
                
                try
                {
                    _currentCancellationTokenSource.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // Already disposed, ignore
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error disposing token: {ex.Message}", ex);
                }
                
                _currentCancellationTokenSource = null;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in Page_Unloaded: {ex.Message}", ex);
        }
    }

    private Task InitializeAiService()
    {
        try
        {
            _aiService = AiServiceFactory.CreateService(_settings.SelectedProvider, _settings);
            UpdateAiName();
            ConnectionStatus = Resource.AiAssistant_ConnectionStatusDisconnected;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error initializing AI service: {ex.Message}", ex);
            ConnectionStatus = Resource.AiAssistant_ConnectionStatusDisconnected;
        }
        return Task.CompletedTask;
    }


    private async void ChatSendButton_Click(object sender, RoutedEventArgs e)
    {
        await SendChatMessage();
    }

    private async Task SendChatMessage()
    {
        var prompt = _chatInputBox?.Text;
        if (string.IsNullOrWhiteSpace(prompt)) return;
        
        // Ensure we have a current session
        if (CurrentSession == null)
        {
            await CreateNewSessionAsync();
            if (CurrentSession == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to create new session");
                return;
            }
        }
        
        // Hide welcome panel immediately when user sends first message
        if (ChatHistory.Count == 0)
        {
            UpdateWelcomePanelVisibility();
        }
        
        // Add user message to history
        var userMessage = new ChatMessage
        {
            IsUser = true,
            Content = prompt,
            Timestamp = DateTime.Now
        };
        ChatHistory.Add(userMessage);
        CurrentSession.Messages.Add(userMessage);
        
        // Update session title if it's the first message
        if (string.IsNullOrWhiteSpace(CurrentSession.Title) || CurrentSession.Title == Resource.AiAssistant_NewConversation)
        {
            CurrentSession.Title = prompt.Length > 30 ? prompt.Substring(0, 30) + "..." : prompt;
            if (CurrentSession.CreatedAt == default)
            {
                CurrentSession.CreatedAt = DateTime.Now;
            }
        }
        CurrentSession.LastUpdatedAt = DateTime.Now;

        // Clear input
        if (_chatInputBox != null)
        {
            _chatInputBox.Text = string.Empty;
        }

        // Scroll to bottom
        ScrollChatToBottom();

        // Call internal method to send message
        await SendChatMessageInternal(prompt);
    }

    private void ScrollChatToBottom()
    {
        if (_chatHistoryScrollViewer != null)
        {
            _chatHistoryScrollViewer.ScrollToEnd();
        }
    }

    private void ScrollChatToBottomSmooth()
    {
        if (_chatHistoryScrollViewer == null) return;
        
        // Use smooth scrolling with gradual updates
        _ = Task.Run(async () =>
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var targetOffset = _chatHistoryScrollViewer?.ScrollableHeight ?? 0;
                    var currentOffset = _chatHistoryScrollViewer?.VerticalOffset ?? 0;
                    
                    if (Math.Abs(targetOffset - currentOffset) < 1.0)
                    {
                        // Already at bottom
                        return;
                    }
                });
                
                // Smooth scroll by gradually updating offset
                var steps = 8;
                for (int i = 0; i < steps; i++)
                {
                    await Task.Delay(15);
                    
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (_chatHistoryScrollViewer == null) return;
                        
                        var targetOffset = _chatHistoryScrollViewer.ScrollableHeight;
                        var currentOffset = _chatHistoryScrollViewer.VerticalOffset;
                        var stepSize = (targetOffset - currentOffset) / (steps - i);
                        var newOffset = currentOffset + stepSize;
                        
                        _chatHistoryScrollViewer.ScrollToVerticalOffset(newOffset);
                    });
                }
                
                // Ensure we're at the bottom
                await Dispatcher.InvokeAsync(() =>
                {
                    _chatHistoryScrollViewer?.ScrollToEnd();
                });
            }
            catch
            {
                // Fallback to simple scroll
                Dispatcher.Invoke(() =>
                {
                    _chatHistoryScrollViewer?.ScrollToEnd();
                });
            }
        });
    }


    private void CopyMessageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.Button button && button.Tag is ChatMessage message)
        {
            try
            {
                Clipboard.SetText(message.Content);
                // Show temporary feedback
                button.Content = Resource.AiAssistant_Copied;
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (s, args) =>
                {
                    button.Content = Resource.AiAssistant_Copy;
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to copy to clipboard: {ex.Message}", ex);
            }
        }
    }

    private void CopyResponseButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.Button button && button.Tag is string text)
        {
            try
            {
                Clipboard.SetText(text);
                button.Content = Resource.AiAssistant_Copied;
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (s, args) =>
                {
                    button.Content = Resource.AiAssistant_CopyResponse;
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to copy to clipboard: {ex.Message}", ex);
            }
        }
    }

    private void LikeMessageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.Button button && button.Tag is ChatMessage message)
        {
            message.IsLiked = !message.IsLiked;
        }
    }

    private void AiMessageMenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.Button button && button.Tag is ChatMessage message)
        {
            var contextMenu = new System.Windows.Controls.ContextMenu();
            
            // Copy menu item
            var copyItem = new System.Windows.Controls.MenuItem
            {
                Header = Resource.AiAssistant_Copy,
                Icon = new TextBlock { Text = "📋", FontSize = 14 }
            };
            copyItem.Click += (s, args) =>
            {
                try
                {
                    Clipboard.SetText(message.Content);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to copy to clipboard: {ex.Message}", ex);
                }
            };
            contextMenu.Items.Add(copyItem);
            
            // Like menu item
            var likeItem = new System.Windows.Controls.MenuItem
            {
                Header = message.IsLiked ? Resource.AiAssistant_MessageLiked : Resource.AiAssistant_LikeMessage,
                Icon = new TextBlock { Text = "👍", FontSize = 14 }
            };
            likeItem.Click += (s, args) =>
            {
                message.IsLiked = !message.IsLiked;
            };
            contextMenu.Items.Add(likeItem);
            
            // Regenerate menu item
            var regenerateItem = new System.Windows.Controls.MenuItem
            {
                Header = Resource.AiAssistant_Regenerate,
                Icon = new TextBlock { Text = "🔄", FontSize = 14 }
            };
            regenerateItem.Click += (s, args) =>
            {
                RegenerateMessageButton_Click(sender, e);
            };
            contextMenu.Items.Add(regenerateItem);
            
            contextMenu.PlacementTarget = button;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }
    }

    private void UserMessageMenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.Button button && button.Tag is ChatMessage message)
        {
            var contextMenu = new System.Windows.Controls.ContextMenu();
            
            // Copy menu item only
            var copyItem = new System.Windows.Controls.MenuItem
            {
                Header = Resource.AiAssistant_Copy,
                Icon = new TextBlock { Text = "📋", FontSize = 14 }
            };
            copyItem.Click += (s, args) =>
            {
                try
                {
                    Clipboard.SetText(message.Content);
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to copy to clipboard: {ex.Message}", ex);
                }
            };
            contextMenu.Items.Add(copyItem);
            
            contextMenu.PlacementTarget = button;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }
    }

    private void ChatInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            e.Handled = true;
            _ = SendChatMessage();
        }
    }

    // These methods are no longer used as functionality has been integrated into the chat UI
    // They are kept for reference but the UI elements they reference have been removed

    private async Task ExecuteAiOperation(
        Func<CancellationToken, Task<string>> operation,
        System.Windows.Controls.TextBlock responseTextBlock,
        string buttonContentKey)
    {
        if (_aiService == null)
        {
            System.Windows.MessageBox.Show(
                Resource.AiAssistant_Error_ConnectionFailedDescription,
                Resource.AiAssistant_Error_ConnectionFailed,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Cancel previous operation
        _currentCancellationTokenSource?.Cancel();
        _currentCancellationTokenSource?.Dispose();
        _currentCancellationTokenSource = new CancellationTokenSource();

        if (responseTextBlock != null)
        {
            responseTextBlock.Text = string.Empty;
        }
        IsProcessing = true;

        // Disable all buttons
        SetButtonsEnabled(false);

        try
        {
            var result = await operation(_currentCancellationTokenSource.Token);
            if (responseTextBlock != null)
            {
                responseTextBlock.Text = result;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"AI operation failed: {ex.Message}", ex);

            var errorMessage = string.Format(Resource.AiAssistant_Error_RequestFailedDescription, ex.Message);
            System.Windows.MessageBox.Show(
                errorMessage,
                Resource.AiAssistant_Error_RequestFailed,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
            SetButtonsEnabled(true);
            
            // Hide loading indicator on send button
            Dispatcher.Invoke(() =>
            {
                if (_sendButtonText != null)
                    _sendButtonText.Visibility = Visibility.Visible;
                if (_sendButtonProgress != null)
                    _sendButtonProgress.Visibility = Visibility.Collapsed;
            });
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        if (_chatSendButton != null)
        {
            _chatSendButton.IsEnabled = enabled;
        }
    }

    private async Task SaveChatHistoryAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(ChatHistoryFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var historyData = ChatHistory.Select(m => new ChatMessageData
            {
                IsUser = m.IsUser,
                Content = m.Content,
                Timestamp = m.Timestamp
            }).ToList();

            var json = JsonSerializer.Serialize(historyData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(ChatHistoryFilePath, json);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error saving chat history: {ex.Message}", ex);
        }
    }

    private async Task LoadChatHistoryAsync()
    {
        try
        {
            if (File.Exists(ChatHistoryFilePath))
            {
                var json = await File.ReadAllTextAsync(ChatHistoryFilePath);
                var historyData = JsonSerializer.Deserialize<List<ChatMessageData>>(json);
                
                if (historyData != null)
                {
                    ChatHistory.Clear();
                    foreach (var data in historyData)
                    {
                        ChatHistory.Add(new ChatMessage
                        {
                            IsUser = data.IsUser,
                            Content = data.Content,
                            Timestamp = data.Timestamp
                        });
                    }
                    
                    // Scroll to bottom after loading
                    await Task.Delay(100);
                    ScrollChatToBottom();
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading chat history: {ex.Message}", ex);
        }
    }

    private async void ClearConversationButton_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            Resource.AiAssistant_ClearConversationConfirm,
            Resource.AiAssistant_ClearConversation,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            ChatHistory.Clear();
            await SaveChatHistoryAsync();
        }
    }

    private async void ExportHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = $"chat-history-{DateTime.Now:yyyyMMdd-HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
                
                if (extension == ".json")
                {
                    var historyData = ChatHistory.Select(m => new ChatMessageData
                    {
                        IsUser = m.IsUser,
                        Content = m.Content,
                        Timestamp = m.Timestamp
                    }).ToList();
                    
                    var json = JsonSerializer.Serialize(historyData, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    await File.WriteAllTextAsync(dialog.FileName, json);
                }
                else
                {
                    var sb = new StringBuilder();
                    foreach (var message in ChatHistory)
                    {
                        sb.AppendLine($"[{message.Timestamp:yyyy-MM-dd HH:mm:ss}] {(message.IsUser ? "User" : "AI")}:");
                        sb.AppendLine(message.Content);
                        sb.AppendLine();
                    }
                    await File.WriteAllTextAsync(dialog.FileName, sb.ToString());
                }

                System.Windows.MessageBox.Show(
                    Resource.AiAssistant_ExportSuccess,
                    Resource.AiAssistant_ExportHistory,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                string.Format(Resource.AiAssistant_ExportError, ex.Message),
                Resource.AiAssistant_ExportHistory,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void NewSessionButton_Click(object sender, RoutedEventArgs e)
    {
        await CreateNewSessionAsync();
    }
    
    private void SessionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.ListBox listBox && listBox.SelectedItem is ChatSession session)
        {
            CurrentSession = session;
        }
    }
    
    private void SessionItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBlock textBlock && textBlock.DataContext is ChatSession session)
        {
            CurrentSession = session;
        }
    }
    
    private void PromptTemplateSelected(object sender, SelectionChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.ComboBox comboBox && comboBox.SelectedItem is PromptTemplate template)
        {
            if (_chatInputBox != null)
            {
                _chatInputBox.Text = template.Template;
                _chatInputBox.Focus();
            }
        }
    }
    
    private async void RegenerateMessageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.Button button && button.Tag is ChatMessage message && !message.IsUser)
        {
            // Find the user message before this AI message
            var messageIndex = ChatHistory.IndexOf(message);
            if (messageIndex > 0)
            {
                var userMessage = ChatHistory[messageIndex - 1];
                if (userMessage.IsUser)
                {
                    // Remove the old AI message
                    ChatHistory.Remove(message);
                    CurrentSession?.Messages.Remove(message);
                    
                    // Regenerate response
                    await SendChatMessageInternal(userMessage.Content);
                }
            }
        }
    }
    
    private async Task CreateNewSessionAsync()
    {
        var session = new ChatSession
        {
            Title = Resource.AiAssistant_NewConversation,
            CreatedAt = DateTime.Now,
            LastUpdatedAt = DateTime.Now
        };
        ChatSessions.Add(session);
        CurrentSession = session;
        UpdateSessionGroups();
        await SaveSessionsAsync();
        UpdateWelcomePanelVisibility();
    }
    
    private void UpdateWelcomePanelVisibility()
    {
        if (_welcomePanel == null) return;
        
        var shouldShow = ChatHistory.Count == 0;
        var isCurrentlyVisible = _welcomePanel.Visibility == Visibility.Visible;
        
        if (shouldShow && !isCurrentlyVisible)
        {
            // Show with fade in animation
            _welcomePanel.Visibility = Visibility.Visible;
            _welcomePanel.Opacity = 0;
            var fadeIn = (System.Windows.Media.Animation.Storyboard)FindResource("WelcomeFadeInAnimation");
            if (fadeIn != null)
            {
                fadeIn.Begin(_welcomePanel);
            }
            else
            {
                _welcomePanel.Opacity = 1;
            }
        }
        else if (!shouldShow && isCurrentlyVisible)
        {
            // Hide with fade out animation
            var fadeOut = (System.Windows.Media.Animation.Storyboard)FindResource("WelcomeFadeOutAnimation");
            if (fadeOut != null)
            {
                fadeOut.Completed += (s, e) =>
                {
                    if (_welcomePanel != null && ChatHistory.Count > 0)
                    {
                        _welcomePanel.Visibility = Visibility.Collapsed;
                    }
                };
                fadeOut.Begin(_welcomePanel);
            }
            else
            {
                _welcomePanel.Visibility = Visibility.Collapsed;
            }
        }
    }
    
    private async void QuickActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.Button button && button.Tag is string action && _chatInputBox != null)
        {
            var userInput = _chatInputBox.Text?.Trim() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(userInput))
            {
                // If input is empty, just set a prompt template
                var prompt = action switch
                {
                    "write" => "请帮我写作，主题是：",
                    "translate" => "请帮我翻译以下内容：",
                    "code" => "请帮我编写代码，需求是：",
                    "search" => "请帮我深入研究以下主题：",
                    "document" => "请帮我生成文档，主题是：",
                    "summarize" => "请帮我总结以下内容：",
                    "image" => "请帮我生成图像，描述是：",
                    "video" => "请帮我生成视频，描述是：",
                    "more" => "",
                    _ => ""
                };
                
                if (!string.IsNullOrEmpty(prompt))
                {
                    _chatInputBox.Text = prompt;
                    _chatInputBox.Focus();
                    _chatInputBox.CaretIndex = prompt.Length;
                }
            }
            else
            {
                // If input exists, execute the action directly
                await ExecuteQuickAction(action, userInput);
            }
        }
    }

    private async Task ExecuteQuickAction(string action, string input)
    {
        if (_aiService == null || CurrentSession == null) return;

        string result = string.Empty;
        string userMessage = input;

        try
        {
            IsProcessing = true;
            _currentCancellationTokenSource?.Cancel();
            _currentCancellationTokenSource?.Dispose();
            _currentCancellationTokenSource = new CancellationTokenSource();

            result = action switch
            {
                "translate" => await _aiService.TranslateAsync(input, "Chinese", _currentCancellationTokenSource.Token),
                "code" => await _aiService.GenerateCodeAsync(input, "C#", _currentCancellationTokenSource.Token),
                "search" => await _aiService.SearchAsync(input, _currentCancellationTokenSource.Token),
                "document" => await _aiService.GenerateDocumentAsync(input, "", _currentCancellationTokenSource.Token),
                "summarize" => await _aiService.SummarizeAsync(input, _currentCancellationTokenSource.Token),
                "image" or "video" => "此功能即将推出",
                _ => await _aiService.ChatAsync(input, _currentCancellationTokenSource.Token)
            };

            // Add user message
            CurrentSession.Messages.Add(new ChatMessage
            {
                IsUser = true,
                Content = userMessage,
                Timestamp = DateTime.Now
            });
            ChatHistory.Add(CurrentSession.Messages.Last());

            // Add AI response
            CurrentSession.Messages.Add(new ChatMessage
            {
                IsUser = false,
                Content = result,
                Timestamp = DateTime.Now
            });
            ChatHistory.Add(CurrentSession.Messages.Last());

            // Clear input
            if (_chatInputBox != null)
            {
                _chatInputBox.Text = string.Empty;
            }

            await SaveChatHistoryAsync();
            await SaveSessionsAsync();
            ScrollChatToBottom();
            UpdateWelcomePanelVisibility();
        }
        catch (OperationCanceledException)
        {
            // Operation was cancelled, ignore
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"操作失败: {ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
        }
    }
    
    private void DeepThinkingButton_Click(object sender, RoutedEventArgs e)
    {
        if (_chatInputBox != null)
        {
            var currentText = _chatInputBox.Text ?? string.Empty;
            _chatInputBox.Text = $"[深度思考模式] {currentText}";
            _chatInputBox.Focus();
            _chatInputBox.CaretIndex = _chatInputBox.Text.Length;
        }
    }
    
    private void InternetSearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (_chatInputBox != null)
        {
            var currentText = _chatInputBox.Text ?? string.Empty;
            _chatInputBox.Text = $"[联网搜索] {currentText}";
            _chatInputBox.Focus();
            _chatInputBox.CaretIndex = _chatInputBox.Text.Length;
        }
    }
    
    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement edit functionality
        System.Windows.MessageBox.Show(
            Resource.AiAssistant_Edit + " " + Resource.AiAssistant_FeatureComingSoon,
            Resource.AiAssistant_FeatureComingSoon,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
    
    private void VoiceButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement voice input functionality
        System.Windows.MessageBox.Show(
            Resource.AiAssistant_Voice + " " + Resource.AiAssistant_FeatureComingSoon,
            Resource.AiAssistant_FeatureComingSoon,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void FunctionItem_Click(object sender, RoutedEventArgs e)
    {
        string? functionName = null;
        
        if (sender is TextBlock textBlock)
        {
            functionName = textBlock.Text;
        }
        else if (sender is System.Windows.Controls.Button button)
        {
            // Extract text from button content
            if (button.Content is Grid grid)
            {
                // Find the TextBlock in the second column (index 1) which contains the function name
                var textBlocks = grid.Children.OfType<TextBlock>().ToList();
                if (textBlocks.Count >= 2)
                {
                    // The second TextBlock (index 1) should be the function name
                    functionName = textBlocks[1].Text;
                }
            }
            else if (button.Content is string content)
            {
                functionName = content;
            }
        }
        
        if (!string.IsNullOrEmpty(functionName))
        {
            // Handle function item click - could navigate to specific function or insert template
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Function item clicked: {functionName}");
        }
    }

    private void AtButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement @ mention functionality
        if (_chatInputBox != null)
        {
            var currentText = _chatInputBox.Text ?? string.Empty;
            _chatInputBox.Text = $"{currentText}@";
            _chatInputBox.Focus();
            _chatInputBox.CaretIndex = _chatInputBox.Text.Length;
        }
    }

    private void SidebarToggleButton_Click(object sender, RoutedEventArgs e)
    {
        var sidebarPanel = this.FindName("_sidebarPanel") as Border;
        var toggleButton = this.FindName("_sidebarToggleButton") as Wpf.Ui.Controls.Button;
        
        if (sidebarPanel != null)
        {
            sidebarPanel.Visibility = sidebarPanel.Visibility == Visibility.Visible 
                ? Visibility.Collapsed 
                : Visibility.Visible;
            
            // 当侧边栏隐藏时显示ToolTip，显示时隐藏ToolTip
            if (toggleButton != null)
            {
                if (sidebarPanel.Visibility == Visibility.Collapsed)
                {
                    toggleButton.ToolTip = "展开/折叠侧边栏";
                }
                else
                {
                    toggleButton.ToolTip = null;
                }
            }
        }
    }

    private void ToolbarToggleButton_Click(object sender, RoutedEventArgs e)
    {
        var toolbarPanel = this.FindName("_toolbarPanel") as StackPanel;
        if (toolbarPanel != null)
        {
            toolbarPanel.Visibility = toolbarPanel.Visibility == Visibility.Visible 
                ? Visibility.Collapsed 
                : Visibility.Visible;
        }
    }

    private void ChatInputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Update placeholder visibility
        var placeholder = this.FindName("_chatInputPlaceholder") as TextBlock;
        if (placeholder != null && _chatInputBox != null)
        {
            placeholder.Visibility = string.IsNullOrWhiteSpace(_chatInputBox.Text) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }
    }
    
    private async Task SaveSessionsAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(SessionsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var sessionsData = ChatSessions.Select(s => new SessionData
            {
                Id = s.Id,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                LastUpdatedAt = s.LastUpdatedAt,
                Messages = s.Messages.Select(m => new ChatMessageData
                {
                    IsUser = m.IsUser,
                    Content = m.Content,
                    Timestamp = m.Timestamp
                }).ToList()
            }).ToList();

            var json = JsonSerializer.Serialize(sessionsData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(SessionsFilePath, json);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error saving sessions: {ex.Message}", ex);
        }
    }

    private async Task LoadSessionsAsync()
    {
        try
        {
            if (File.Exists(SessionsFilePath))
            {
                var json = await File.ReadAllTextAsync(SessionsFilePath);
                var sessionsData = JsonSerializer.Deserialize<List<SessionData>>(json);
                
                if (sessionsData != null)
                {
                    ChatSessions.Clear();
                    foreach (var data in sessionsData)
                    {
                        var session = new ChatSession
                        {
                            Id = data.Id,
                            Title = data.Title,
                            CreatedAt = data.CreatedAt,
                            LastUpdatedAt = data.LastUpdatedAt
                        };
                        
                        foreach (var msgData in data.Messages)
                        {
                            session.Messages.Add(new ChatMessage
                            {
                                IsUser = msgData.IsUser,
                                Content = msgData.Content,
                                Timestamp = msgData.Timestamp
                            });
                        }
                        
                        ChatSessions.Add(session);
                    }
                    
                    // Group sessions by time period
                    UpdateSessionGroups();
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading sessions: {ex.Message}", ex);
        }
    }
    
    private void UpdateSessionGroups()
    {
        SessionGroups.Clear();
        
        var now = DateTime.Now;
        var today = now.Date;
        var yesterday = today.AddDays(-1);
        var sevenDaysAgo = today.AddDays(-7);
        var thirtyDaysAgo = today.AddDays(-30);
        
        // Group sessions
        var todaySessions = ChatSessions.Where(s => s.LastUpdatedAt.Date == today).OrderByDescending(s => s.LastUpdatedAt).ToList();
        var yesterdaySessions = ChatSessions.Where(s => s.LastUpdatedAt.Date == yesterday).OrderByDescending(s => s.LastUpdatedAt).ToList();
        var weekSessions = ChatSessions.Where(s => s.LastUpdatedAt >= sevenDaysAgo && s.LastUpdatedAt < yesterday).OrderByDescending(s => s.LastUpdatedAt).ToList();
        var monthSessions = ChatSessions.Where(s => s.LastUpdatedAt >= thirtyDaysAgo && s.LastUpdatedAt < sevenDaysAgo).OrderByDescending(s => s.LastUpdatedAt).ToList();
        
        if (todaySessions.Any())
        {
            var group = new SessionGroup { GroupName = "今天" };
            foreach (var session in todaySessions)
            {
                group.Sessions.Add(session);
            }
            SessionGroups.Add(group);
        }
        
        if (yesterdaySessions.Any())
        {
            var group = new SessionGroup { GroupName = "昨天" };
            foreach (var session in yesterdaySessions)
            {
                group.Sessions.Add(session);
            }
            SessionGroups.Add(group);
        }
        
        if (weekSessions.Any())
        {
            var group = new SessionGroup { GroupName = "7天内" };
            foreach (var session in weekSessions)
            {
                group.Sessions.Add(session);
            }
            SessionGroups.Add(group);
        }
        
        if (monthSessions.Any())
        {
            var group = new SessionGroup { GroupName = "30天内" };
            foreach (var session in monthSessions)
            {
                group.Sessions.Add(session);
            }
            SessionGroups.Add(group);
        }
    }
    
    private async Task SendChatMessageInternal(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return;

        if (_aiService == null)
        {
            System.Windows.MessageBox.Show(
                Resource.AiAssistant_Error_ConnectionFailedDescription,
                Resource.AiAssistant_Error_ConnectionFailed,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Cancel previous operation safely
        if (_currentCancellationTokenSource != null)
        {
            try
            {
                if (!_currentCancellationTokenSource.Token.IsCancellationRequested)
                {
                    _currentCancellationTokenSource.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }
            
            try
            {
                _currentCancellationTokenSource.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }
        }
        _currentCancellationTokenSource = new CancellationTokenSource();

        IsProcessing = true;
        SetButtonsEnabled(false);

        // Add typing indicator for AI response
        var aiMessage = new ChatMessage
        {
            IsUser = false,
            Content = "正在思考...",
            Timestamp = DateTime.Now,
            IsTyping = true
        };
        ChatHistory.Add(aiMessage);
        CurrentSession?.Messages.Add(aiMessage);
        ScrollChatToBottomSmooth();

        try
        {
            // Use streaming for character-by-character display
            var accumulatedText = string.Empty;
            var lastDisplayedLength = 0;
            
            await foreach (var chunk in _aiService.ChatStreamAsync(prompt, _currentCancellationTokenSource.Token))
            {
                if (_currentCancellationTokenSource.Token.IsCancellationRequested)
                    break;

                accumulatedText += chunk;
                
                // Update UI on dispatcher thread
                await Dispatcher.InvokeAsync(() =>
                {
                    // Clear typing indicator when first chunk arrives
                    if (aiMessage.IsTyping && !string.IsNullOrEmpty(accumulatedText))
                    {
                        aiMessage.IsTyping = false;
                        aiMessage.Content = string.Empty;
                    }
                    
                    // Display accumulated text with typing effect
                    var currentContent = aiMessage.Content ?? string.Empty;
                    var newContent = accumulatedText;
                    
                    // Add characters one by one for typing effect
                    if (newContent.Length > currentContent.Length)
                    {
                        var toAdd = newContent.Substring(currentContent.Length);
                        foreach (var character in toAdd)
                        {
                            if (_currentCancellationTokenSource?.Token.IsCancellationRequested == true)
                                break;
                                
                            aiMessage.Content = (aiMessage.Content ?? string.Empty) + character;
                        }
                        
                        // Scroll to bottom periodically with smooth animation
                        if (aiMessage.Content != null && aiMessage.Content.Length - lastDisplayedLength >= 10)
                        {
                            ScrollChatToBottomSmooth();
                            lastDisplayedLength = aiMessage.Content.Length;
                        }
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);
                
                // Small delay to create typing effect (adjust speed: lower = faster)
                await Task.Delay(5, _currentCancellationTokenSource.Token);
            }
            
            // Ensure final content is set and scroll to bottom
            await Dispatcher.InvokeAsync(async () =>
            {
                aiMessage.Content = accumulatedText;
                ScrollChatToBottom();
                
                // Update session
                if (CurrentSession != null)
                {
                    CurrentSession.LastUpdatedAt = DateTime.Now;
                }
                
                // Save sessions after message is complete
                await SaveSessionsAsync();
            });
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                ChatHistory.Remove(aiMessage);
                CurrentSession?.Messages.Remove(aiMessage);
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Chat failed: {ex.Message}", ex);

            await Dispatcher.InvokeAsync(() =>
            {
                aiMessage.Content = $"{Resource.AiAssistant_Error_RequestFailed}: {ex.Message}";
                var errorMessage = string.Format(Resource.AiAssistant_Error_RequestFailedDescription, ex.Message);
                System.Windows.MessageBox.Show(
                    errorMessage,
                    Resource.AiAssistant_Error_RequestFailed,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
        }
        finally
        {
            await Dispatcher.InvokeAsync(() =>
            {
                IsProcessing = false;
                SetButtonsEnabled(true);
                ScrollChatToBottom();
            });
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Chat message model for conversation history
/// </summary>
public class ChatMessage : INotifyPropertyChanged
{
    private string _content = string.Empty;
    private bool _isLiked;
    private bool _isTyping;

    public bool IsUser { get; set; }
    
    public string Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                OnPropertyChanged();
            }
        }
    }
    
    public DateTime Timestamp { get; set; }
    
    public string FormattedTimestamp => Timestamp.ToString("HH:mm");

    public bool IsLiked
    {
        get => _isLiked;
        set
        {
            if (_isLiked != value)
            {
                _isLiked = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsTyping
    {
        get => _isTyping;
        set
        {
            if (_isTyping != value)
            {
                _isTyping = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Chat message data for serialization
/// </summary>
internal class ChatMessageData
{
    public bool IsUser { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Session data for serialization
/// </summary>
internal class SessionData
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public List<ChatMessageData> Messages { get; set; } = new();
}

/// <summary>
/// Group of sessions by time period
/// </summary>
public class SessionGroup : INotifyPropertyChanged
{
    private string _groupName = string.Empty;
    
    public string GroupName
    {
        get => _groupName;
        set
        {
            if (_groupName != value)
            {
                _groupName = value;
                OnPropertyChanged();
            }
        }
    }
    
    public ObservableCollection<ChatSession> Sessions { get; } = new();
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}



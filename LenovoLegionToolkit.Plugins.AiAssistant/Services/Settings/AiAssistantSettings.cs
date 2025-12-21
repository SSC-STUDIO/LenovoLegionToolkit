using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.AiAssistant.Services;

namespace LenovoLegionToolkit.Plugins.AiAssistant.Services.Settings;

/// <summary>
/// AI Assistant settings manager
/// </summary>
public class AiAssistantSettings
{
    private static readonly string SettingsFilePath = Path.Combine(
        Folders.AppData,
        "plugins",
        "ai-assistant",
        "settings.json");

    private SettingsData _data = new();
    private readonly object _saveLock = new();
    private CancellationTokenSource? _saveCancellationTokenSource;
    private bool _isLoading;

    public AiProvider SelectedProvider
    {
        get => _data.SelectedProvider;
        set
        {
            _data.SelectedProvider = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public string OpenAiApiKey
    {
        get => _data.OpenAiApiKey ?? string.Empty;
        set
        {
            _data.OpenAiApiKey = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public string OpenAiModel
    {
        get => _data.OpenAiModel ?? "gpt-3.5-turbo";
        set
        {
            _data.OpenAiModel = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public string OllamaBaseUrl
    {
        get => _data.OllamaBaseUrl ?? "http://localhost:11434";
        set
        {
            _data.OllamaBaseUrl = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public string OllamaModel
    {
        get => _data.OllamaModel ?? "llama2";
        set
        {
            _data.OllamaModel = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool AutoStartOllama
    {
        get => _data.AutoStartOllama;
        set
        {
            _data.AutoStartOllama = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public string? OllamaExecutablePath
    {
        get => _data.OllamaExecutablePath;
        set
        {
            _data.OllamaExecutablePath = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public bool UseBuiltInOllama
    {
        get => _data.UseBuiltInOllama;
        set
        {
            _data.UseBuiltInOllama = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public string DeepSeekApiKey
    {
        get => _data.DeepSeekApiKey ?? string.Empty;
        set
        {
            _data.DeepSeekApiKey = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    public string DeepSeekModel
    {
        get => _data.DeepSeekModel ?? "deepseek-chat";
        set
        {
            _data.DeepSeekModel = value;
            if (!_isLoading)
                _ = SaveAsyncDelayed();
        }
    }

    private async Task SaveAsyncDelayed()
    {
        lock (_saveLock)
        {
            // Cancel previous save operation
            _saveCancellationTokenSource?.Cancel();
            _saveCancellationTokenSource = new CancellationTokenSource();
        }

        try
        {
            // Wait a bit to batch multiple property changes
            await Task.Delay(500, _saveCancellationTokenSource.Token);
            
            // Save if not cancelled
            if (!_saveCancellationTokenSource.Token.IsCancellationRequested)
            {
                await SaveAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when a new save is triggered
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in delayed save: {ex.Message}", ex);
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            _isLoading = true;
            if (File.Exists(SettingsFilePath))
            {
                var json = await File.ReadAllTextAsync(SettingsFilePath);
                _data = JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading settings: {ex.Message}", ex);
            _data = new SettingsData();
        }
        finally
        {
            _isLoading = false;
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error saving settings: {ex.Message}", ex);
        }
    }

    private class SettingsData
    {
        public AiProvider SelectedProvider { get; set; } = AiProvider.OpenAI;
        public string? OpenAiApiKey { get; set; }
        public string? OpenAiModel { get; set; } = "gpt-3.5-turbo";
        public string? OllamaBaseUrl { get; set; } = "http://localhost:11434";
        public string? OllamaModel { get; set; } = "llama2";
        public bool AutoStartOllama { get; set; } = true;
        public string? OllamaExecutablePath { get; set; }
        public bool UseBuiltInOllama { get; set; } = true; // Default to using built-in Ollama if available
        public string? DeepSeekApiKey { get; set; }
        public string? DeepSeekModel { get; set; } = "deepseek-chat";
    }
}



using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.AiAssistant.Services;

namespace LenovoLegionToolkit.Plugins.AiAssistant.Services.Ollama;

/// <summary>
/// Ollama local model service implementation
/// </summary>
public class OllamaService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _model;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OllamaService(string baseUrl = "http://localhost:11434", string model = "llama2")
    {
        _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromMinutes(5) // Ollama can take longer for large models
        };
    }

    private HttpClient CreateShortTimeoutClient()
    {
        return new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(10) // Shorter timeout for connection tests
        };
    }

    public async Task<string> ChatAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Ollama Chat request: {prompt.Substring(0, Math.Min(50, prompt.Length))}...");

            var requestBody = new OllamaGenerateRequest
            {
                Model = _model,
                Prompt = prompt,
                Stream = false
            };

            var response = await _httpClient.PostAsJsonAsync("/api/generate", requestBody, JsonOptions, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Ollama API error: {response.StatusCode} - {errorContent}");
                throw new HttpRequestException($"Ollama API request failed with status {response.StatusCode}: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(JsonOptions, cancellationToken);
            var content = result?.Response ?? string.Empty;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Ollama Chat response received: {content.Length} characters");

            return content;
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Ollama Chat timeout: Request took longer than {_httpClient.Timeout.TotalMinutes} minutes");
            throw new TimeoutException($"Ollama request timed out after {_httpClient.Timeout.TotalMinutes} minutes", ex);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Ollama Chat error: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<string> GenerateDocumentAsync(string topic, string context, CancellationToken cancellationToken = default)
    {
        var prompt = $"Write a comprehensive document about '{topic}'. Context: {context}. Please provide a well-structured document with clear sections.";
        return await ChatAsync(prompt, cancellationToken);
    }

    public async Task<string> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var prompt = $"Please provide a detailed answer to the following question: {query}. If you don't know the answer, please say so.";
        return await ChatAsync(prompt, cancellationToken);
    }

    public async Task<string> GenerateCodeAsync(string description, string language, CancellationToken cancellationToken = default)
    {
        var prompt = $"Generate {language} code for the following requirement: {description}. Please provide complete, working code with comments explaining the logic.";
        return await ChatAsync(prompt, cancellationToken);
    }

    public async Task<string> ExplainCodeAsync(string code, string language, CancellationToken cancellationToken = default)
    {
        var prompt = $"Explain the following {language} code in detail, including what it does, how it works, and any important aspects:\n\n```{language}\n{code}\n```";
        return await ChatAsync(prompt, cancellationToken);
    }

    public async Task<string> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken = default)
    {
        var prompt = $"Translate the following text to {targetLanguage}. Only provide the translation, without any additional explanations:\n\n{text}";
        return await ChatAsync(prompt, cancellationToken);
    }

    public async Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default)
    {
        var prompt = $"Please provide a concise summary of the following text:\n\n{text}";
        return await ChatAsync(prompt, cancellationToken);
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(string prompt, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Ollama Chat stream request: {prompt.Substring(0, Math.Min(50, prompt.Length))}...");

        var requestBody = new OllamaGenerateRequest
        {
            Model = _model,
            Prompt = prompt,
            Stream = true
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/generate")
        {
            Content = JsonContent.Create(requestBody, options: JsonOptions)
        };

        HttpResponseMessage? response = null;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Ollama API error: {response.StatusCode} - {errorContent}");
                throw new HttpRequestException($"Ollama API request failed with status {response.StatusCode}: {errorContent}");
            }
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Ollama Chat stream timeout: Request took longer than {_httpClient.Timeout.TotalMinutes} minutes");
            throw new TimeoutException($"Ollama request timed out after {_httpClient.Timeout.TotalMinutes} minutes", ex);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Ollama Chat stream error: {ex.Message}", ex);
            throw;
        }

        if (response == null) yield break;

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? accumulatedContent = null;
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            OllamaGenerateResponse? result = null;
            try
            {
                result = JsonSerializer.Deserialize<OllamaGenerateResponse>(line, JsonOptions);
            }
            catch (JsonException)
            {
                // Skip invalid JSON lines
                continue;
            }

            if (result?.Response != null)
            {
                accumulatedContent = (accumulatedContent ?? string.Empty) + result.Response;
                yield return result.Response;
            }
            
            // Check if this is the final response
            if (result?.Done == true)
                break;
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Ollama Chat stream completed: {(accumulatedContent?.Length ?? 0)} characters");
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var testClient = CreateShortTimeoutClient();
        
        try
        {
            // First, test if Ollama service is running by checking /api/tags
            var tagsResponse = await testClient.GetAsync("/api/tags", cancellationToken);
            if (!tagsResponse.IsSuccessStatusCode)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Ollama service not available: {tagsResponse.StatusCode}");
                throw new HttpRequestException($"Ollama 服务不可用 (HTTP {tagsResponse.StatusCode})。请确保 Ollama 服务正在运行。");
            }

            // Parse response to check available models
            var tagsContent = await tagsResponse.Content.ReadAsStringAsync(cancellationToken);
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Ollama /api/tags response: {tagsContent}");

            // Then, try a simple chat request to verify the model is available
            try
            {
                var testPrompt = "hi";
                var requestBody = new OllamaGenerateRequest
                {
                    Model = _model,
                    Prompt = testPrompt,
                    Stream = false
                };
                
                var generateResponse = await testClient.PostAsJsonAsync("/api/generate", requestBody, JsonOptions, cancellationToken);
                
                if (!generateResponse.IsSuccessStatusCode)
                {
                    var errorContent = await generateResponse.Content.ReadAsStringAsync(cancellationToken);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Ollama model test failed: {generateResponse.StatusCode} - {errorContent}");
                    
                    if (generateResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new HttpRequestException($"模型 '{_model}' 不存在。请检查模型名称是否正确，或使用 'ollama list' 查看可用模型。");
                    }
                    throw new HttpRequestException($"无法使用模型 '{_model}' (HTTP {generateResponse.StatusCode}): {errorContent}");
                }
                
                return true;
            }
            catch (HttpRequestException)
            {
                throw; // Re-throw HTTP exceptions with better messages
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Ollama model '{_model}' test failed: {ex.Message}");
                throw new HttpRequestException($"无法连接到模型 '{_model}'：{ex.Message}", ex);
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Ollama connection test timeout");
            throw new TimeoutException($"连接超时：无法在10秒内连接到 Ollama 服务 ({_baseUrl})。请确保 Ollama 服务正在运行并且地址正确。", ex);
        }
        catch (HttpRequestException ex)
        {
            var errorMsg = ex.Message.ToLowerInvariant();
            if (errorMsg.Contains("connection refused") || 
                errorMsg.Contains("no connection could be made") || 
                errorMsg.Contains("拒绝连接") ||
                errorMsg.Contains("无法连接") ||
                errorMsg.Contains("connection reset"))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Ollama connection refused: {ex.Message}");
                throw new HttpRequestException($"无法连接到 Ollama 服务 ({_baseUrl})。\n\n可能的原因：\n1. Ollama 服务未运行，请启动 Ollama\n2. 地址或端口不正确\n3. 防火墙阻止了连接", ex);
            }
            // Re-throw other HTTP exceptions (they may already have good messages from above)
            throw;
        }
        catch (Exception ex)
        {
            var exMsg = ex.Message.ToLowerInvariant();
            if (exMsg.Contains("connection refused") || 
                exMsg.Contains("no connection could be made") || 
                exMsg.Contains("拒绝连接") ||
                exMsg.Contains("无法连接"))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Ollama connection refused: {ex.Message}");
                throw new HttpRequestException($"无法连接到 Ollama 服务 ({_baseUrl})。\n\n可能的原因：\n1. Ollama 服务未运行，请启动 Ollama\n2. 地址或端口不正确\n3. 防火墙阻止了连接", ex);
            }
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Ollama connection test failed: {ex.Message}", ex);
            throw new HttpRequestException($"连接测试失败：{ex.Message}\n\n请检查：\n1. Ollama 服务是否正在运行\n2. 地址是否正确 ({_baseUrl})\n3. 模型名称是否正确 ({_model})", ex);
        }
    }

    private class OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;
    }

    private class OllamaGenerateResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("context")]
        public long[]? Context { get; set; }

        [JsonPropertyName("total_duration")]
        public long? TotalDuration { get; set; }

        [JsonPropertyName("load_duration")]
        public long? LoadDuration { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; set; }

        [JsonPropertyName("prompt_eval_duration")]
        public long? PromptEvalDuration { get; set; }

        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; }

        [JsonPropertyName("eval_duration")]
        public long? EvalDuration { get; set; }
    }
}



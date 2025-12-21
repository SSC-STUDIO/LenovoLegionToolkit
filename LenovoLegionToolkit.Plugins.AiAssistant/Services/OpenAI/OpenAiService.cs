using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.AiAssistant.Services;

namespace LenovoLegionToolkit.Plugins.AiAssistant.Services.OpenAI;

/// <summary>
/// OpenAI API service implementation
/// </summary>
public class OpenAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private const string ApiBaseUrl = "https://api.openai.com/v1";

    public OpenAiService(string apiKey, string model = "gpt-3.5-turbo")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("OpenAI API key cannot be empty", nameof(apiKey));

        _apiKey = apiKey;
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(ApiBaseUrl),
            Timeout = TimeSpan.FromMinutes(2)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<string> ChatAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"OpenAI Chat request: {prompt.Substring(0, Math.Min(50, prompt.Length))}...");

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 2000
            };

            var response = await _httpClient.PostAsJsonAsync("/chat/completions", requestBody, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(cancellationToken: cancellationToken);
            var content = result?.choices?[0]?.message?.content ?? string.Empty;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"OpenAI Chat response received: {content.Length} characters");

            return content;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"OpenAI Chat error: {ex.Message}", ex);
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
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"OpenAI Chat stream request: {prompt.Substring(0, Math.Min(50, prompt.Length))}...");

        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = 2000,
            stream = true
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/chat/completions")
        {
            Content = JsonContent.Create(requestBody)
        };

        HttpResponseMessage? response = null;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"OpenAI Chat stream error: {ex.Message}", ex);
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

            if (line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase))
            {
                var jsonData = line.Substring(6).Trim();
                if (jsonData == "[DONE]") break;

                string? contentValue = null;
                try
                {
                    using var doc = JsonDocument.Parse(jsonData);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var firstChoice = choices[0];
                        if (firstChoice.TryGetProperty("delta", out var delta) &&
                            delta.TryGetProperty("content", out var content))
                        {
                            contentValue = content.GetString();
                        }
                    }
                }
                catch (JsonException)
                {
                    // Skip invalid JSON lines
                    continue;
                }

                if (!string.IsNullOrEmpty(contentValue))
                {
                    accumulatedContent = (accumulatedContent ?? string.Empty) + contentValue;
                    yield return contentValue;
                }
            }
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"OpenAI Chat stream completed: {(accumulatedContent?.Length ?? 0)} characters");
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ChatAsync("Hello", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"OpenAI connection test failed: {ex.Message}", ex);
            return false;
        }
    }

    private class OpenAiChatResponse
    {
        public OpenAiChoice[]? choices { get; set; }
    }

    private class OpenAiChoice
    {
        public OpenAiMessage? message { get; set; }
    }

    private class OpenAiMessage
    {
        public string? content { get; set; }
    }
}



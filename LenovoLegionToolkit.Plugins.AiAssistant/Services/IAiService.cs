using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Plugins.AiAssistant.Services;

/// <summary>
/// AI service interface for unified AI operations
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Chat with AI
    /// </summary>
    Task<string> ChatAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Chat with AI using streaming (character by character)
    /// </summary>
    IAsyncEnumerable<string> ChatStreamAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate document based on topic and context
    /// </summary>
    Task<string> GenerateDocumentAsync(string topic, string context, CancellationToken cancellationToken = default);

    /// <summary>
    /// AI search/query
    /// </summary>
    Task<string> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate code based on description and language
    /// </summary>
    Task<string> GenerateCodeAsync(string description, string language, CancellationToken cancellationToken = default);

    /// <summary>
    /// Explain code
    /// </summary>
    Task<string> ExplainCodeAsync(string code, string language, CancellationToken cancellationToken = default);

    /// <summary>
    /// Translate text to target language
    /// </summary>
    Task<string> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Summarize text
    /// </summary>
    Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test connection to AI service
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}



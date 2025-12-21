using LenovoLegionToolkit.Plugins.AiAssistant.Services.DeepSeek;
using LenovoLegionToolkit.Plugins.AiAssistant.Services.Ollama;
using LenovoLegionToolkit.Plugins.AiAssistant.Services.OpenAI;
using LenovoLegionToolkit.Plugins.AiAssistant.Services.Settings;

namespace LenovoLegionToolkit.Plugins.AiAssistant.Services;

/// <summary>
/// Factory class for creating AI service instances
/// </summary>
public static class AiServiceFactory
{
    /// <summary>
    /// Create AI service instance based on provider and settings
    /// </summary>
    public static IAiService CreateService(AiProvider provider, AiAssistantSettings settings)
    {
        return provider switch
        {
            AiProvider.OpenAI => new OpenAiService(settings.OpenAiApiKey, settings.OpenAiModel),
            AiProvider.Ollama => new OllamaService(settings.OllamaBaseUrl, settings.OllamaModel),
            AiProvider.DeepSeek => new DeepSeekService(settings.DeepSeekApiKey, settings.DeepSeekModel),
            _ => throw new System.NotSupportedException($"AI provider {provider} is not supported")
        };
    }
}



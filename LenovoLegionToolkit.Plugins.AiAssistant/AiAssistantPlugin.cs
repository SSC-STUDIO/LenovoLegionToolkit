using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Plugins.SDK;
using PluginConstants = LenovoLegionToolkit.Lib.Plugins.PluginConstants;

namespace LenovoLegionToolkit.Plugins.AiAssistant;

/// <summary>
/// AI Assistant plugin for AI-powered features
/// </summary>
[Plugin(
    id: PluginConstants.AiAssistant,
    name: "AI Assistant",
    version: "1.0.0",
    description: "AI-powered assistant with support for OpenAI and Ollama, providing chat, search, document generation, code assistance, translation, and summarization features",
    author: "LenovoLegionToolkit Team",
    MinimumHostVersion = "1.0.0",
    Icon = "Sparkle24"
)]
public class AiAssistantPlugin : PluginBase
{
    public override string Id => PluginConstants.AiAssistant;
    public override string Name => Resource.AiAssistant_PageTitle;
    public override string Description => Resource.AiAssistant_PageDescription;
    public override string Icon => "Sparkle24";
    public override bool IsSystemPlugin => false; // Third-party plugin, can be uninstalled

    /// <summary>
    /// Plugin provides feature extensions and UI pages
    /// </summary>
    public override object? GetFeatureExtension()
    {
        // Return AI Assistant page (implements IPluginPage interface)
        return new AiAssistantPluginPage();
    }

    /// <summary>
    /// Plugin provides settings page
    /// </summary>
    public override object? GetSettingsPage()
    {
        // Return AI Assistant settings page
        return new AiAssistantSettingsPluginPage();
    }
}

/// <summary>
/// AI Assistant plugin page provider
/// </summary>
public class AiAssistantPluginPage : IPluginPage
{
    // Return empty string to hide title in PluginPageWrapper, as we show it in the page content with description
    public string PageTitle => string.Empty;
    public string PageIcon => string.Empty; // No icon required

    public object CreatePage()
    {
        // Return AI Assistant page control
        return new AiAssistantPage();
    }
}

/// <summary>
/// AI Assistant settings plugin page provider
/// </summary>
public class AiAssistantSettingsPluginPage : IPluginPage
{
    public string PageTitle => string.Empty;
    public string PageIcon => string.Empty;

    public object CreatePage()
    {
        // Return AI Assistant settings page control
        return new AiAssistantSettingsPage();
    }
}



namespace LenovoLegionToolkit.Plugins.AiAssistant.Models;

/// <summary>
/// Represents a prompt template for quick access
/// </summary>
public class PromptTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
}


using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Script;

/// <summary>
/// Script manager service interface
/// </summary>
public interface IScriptManagerService : IDisposable
{
    /// <summary>
    /// Whether script injection is enabled
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enable or disable script injection
    /// </summary>
    void SetEnabled(bool enabled);

    /// <summary>
    /// Get all available scripts
    /// </summary>
    Task<IEnumerable<ScriptInfo>> GetScriptsAsync();

    /// <summary>
    /// Load script content for injection
    /// </summary>
    Task<string?> LoadScriptContentAsync(string scriptId);
}

/// <summary>
/// Script information
/// </summary>
public class ScriptInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Path { get; set; }
    public bool IsEnabled { get; set; }
}


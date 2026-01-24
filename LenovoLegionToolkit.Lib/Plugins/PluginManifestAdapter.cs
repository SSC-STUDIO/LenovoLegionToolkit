using System;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Adapter class to convert PluginManifest to IPlugin
/// </summary>
public class PluginManifestAdapter : IPlugin
{
    private readonly PluginManifest _manifest;

    public PluginManifestAdapter(PluginManifest manifest)
    {
        _manifest = manifest;
    }

    public string Id => _manifest.Id;
    public string Name => _manifest.Name;
    public string Description => _manifest.Description;
    public string Icon => _manifest.Icon;
    public bool IsSystemPlugin => _manifest.IsSystemPlugin;
    public string[]? Dependencies => _manifest.Dependencies;

    public void OnInstalled()
    {
    }

    public void OnUninstalled()
    {
    }

    public void OnShutdown()
    {
    }

    public void Stop()
    {
    }
}
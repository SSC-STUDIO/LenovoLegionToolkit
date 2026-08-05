// ============================================================================
// FROZEN EXTERNAL PLUGIN ABI — DO NOT MODIFY SIGNATURES
// ============================================================================
// This project is NOT part of the host solution build graph. It is the
// compile-time ABI contract for plugins built outside the host application
// (Plugins/Official), which references
// UniversalDeviceToolkit.Plugins.Abstractions.dll via HintPath.
//
// It intentionally shares the namespace (UniversalDeviceToolkit.Lib.Plugins)
// with the legacy in-Lib contract (UniversalDeviceToolkit.Lib\Plugins\
// LegacyPluginContracts.cs) while living in a different assembly and a
// different TFM (net10.0, portable). This is a deliberate DUAL-ABI design.
//
// RULES:
//   1. Never remove, rename, or re-sign a member declared in this project.
//   2. New members may only be ADDED, and only if the matching type in
//      UniversalDeviceToolkit.Lib\Plugins\LegacyPluginContracts.cs is extended
//      identically.
//   3. Keep the namespace and type names in lock-step with the legacy
//      in-Lib contract. The external plugin repo must keep compiling without
//      any changes.
// ============================================================================
namespace UniversalDeviceToolkit.Lib.Plugins;

/// <summary>
/// Plugin interface, defines basic plugin information and behavior
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Plugin unique identifier
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Plugin name (for display)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plugin description
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Plugin icon identifier (can be a symbol name, file path, or resource key)
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Whether it's a system base plugin (base plugins cannot be uninstalled in some cases)
    /// </summary>
    bool IsSystemPlugin { get; }

    /// <summary>
    /// Whether it depends on other plugins
    /// </summary>
    string[]? Dependencies { get; }

    /// <summary>
    /// Called when the plugin is installed
    /// </summary>
    void OnInstalled();

    /// <summary>
    /// Called when the plugin is uninstalled
    /// </summary>
    void OnUninstalled();

    /// <summary>
    /// Called when the application is shutting down
    /// </summary>
    void OnShutdown();

    /// <summary>
    /// Called before plugin update or uninstallation to stop any running processes
    /// </summary>
    void Stop();
}

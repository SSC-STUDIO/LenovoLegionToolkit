using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Plugins.SDK
{
    /// <summary>
    /// Base class for all Lenovo Legion Toolkit plugins
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        /// <inheritdoc />
        public abstract string Id { get; }

        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public abstract string Version { get; }

        /// <inheritdoc />
        public abstract string Author { get; }

        /// <inheritdoc />
        public virtual string? Description => null;

        /// <inheritdoc />
        public virtual string? RepositoryUrl => null;

        /// <inheritdoc />
        public virtual IReadOnlyList<PluginDependency> Dependencies => Array.Empty<PluginDependency>();

        /// <inheritdoc />
        public virtual bool IsSystemPlugin => false;

        /// <summary>
        /// Called when the plugin is being initialized
        /// </summary>
        public virtual Task OnInitializingAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public virtual void OnInstalled() { }

        /// <inheritdoc />
        public virtual void OnUninstalled() { }

        /// <inheritdoc />
        public virtual void OnShutdown() { }

        /// <inheritdoc />
        public virtual void Stop() { }

        /// <summary>
        /// Gets the plugin manifest
        /// </summary>
        public virtual PluginManifest GetManifest()
        {
            return new PluginManifest
            {
                Id = Id,
                Name = Name,
                Version = Version,
                Author = Author,
                Description = Description,
                RepositoryUrl = RepositoryUrl,
                Dependencies = Dependencies,
                IsSystemPlugin = IsSystemPlugin,
                MinLLTVersion = "3.6.0"
            };
        }
    }

    /// <summary>
    /// Interface for plugins that support state persistence
    /// </summary>
    public interface IStatefulPlugin : IPlugin
    {
        /// <summary>
        /// Current state version
        /// </summary>
        int StateVersion { get; }

        /// <summary>
        /// Serializes the plugin state
        /// </summary>
        byte[] SerializeState();

        /// <summary>
        /// Deserializes and restores the plugin state
        /// </summary>
        bool DeserializeState(byte[] stateData, string previousVersion);
    }

    /// <summary>
    /// Plugin interface
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Unique plugin identifier
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Display name of the plugin
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Plugin version (SemVer)
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Plugin author
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Plugin description
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Repository URL
        /// </summary>
        string? RepositoryUrl { get; }

        /// <summary>
        /// Plugin dependencies
        /// </summary>
        IReadOnlyList<PluginDependency> Dependencies { get; }

        /// <summary>
        /// Whether this is a system plugin
        /// </summary>
        bool IsSystemPlugin { get; }

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
        /// Called to stop the plugin
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// Plugin dependency definition
    /// </summary>
    public class PluginDependency
    {
        /// <summary>
        /// Plugin ID of the dependency
        /// </summary>
        public string PluginId { get; set; } = string.Empty;

        /// <summary>
        /// Minimum required version
        /// </summary>
        public string? MinVersion { get; set; }

        /// <summary>
        /// Maximum allowed version
        /// </summary>
        public string? MaxVersion { get; set; }

        /// <summary>
        /// Whether this dependency is optional
        /// </summary>
        public bool IsOptional { get; set; }
    }

    /// <summary>
    /// Plugin manifest
    /// </summary>
    public class PluginManifest
    {
        /// <summary>
        /// Plugin ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Plugin name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Plugin version
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Plugin author
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Plugin description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Repository URL
        /// </summary>
        public string? RepositoryUrl { get; set; }

        /// <summary>
        /// Dependencies
        /// </summary>
        public IReadOnlyList<PluginDependency> Dependencies { get; set; } = Array.Empty<PluginDependency>();

        /// <summary>
        /// Whether this is a system plugin
        /// </summary>
        public bool IsSystemPlugin { get; set; }

        /// <summary>
        /// Minimum LLT version required
        /// </summary>
        public string MinLLTVersion { get; set; } = "3.6.0";
    }
}

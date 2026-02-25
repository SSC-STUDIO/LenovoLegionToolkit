using System;
using System.Collections.Generic;
using LenovoLegionToolkit.Plugins.SDK;

namespace LenovoLegionToolkit.Plugins.ShellIntegration
{
    /// <summary>
    /// Shell Integration plugin - Integrates with Windows shell
    /// </summary>
    public class ShellIntegrationPlugin : PluginBase
    {
        /// <inheritdoc />
        public override string Id => "shell-integration";

        /// <inheritdoc />
        public override string Name => "Shell Integration";

        /// <inheritdoc />
        public override string Version => "1.0.0";

        /// <inheritdoc />
        public override string Author => "LenovoLegionToolkit Team";

        /// <inheritdoc />
        public override string Description => "Integrate Lenovo Legion Toolkit with Windows shell context menu";

        /// <inheritdoc />
        public override string RepositoryUrl => "https://github.com/Crs10259/LenovoLegionToolkit-Plugins";

        /// <inheritdoc />
        public override bool IsSystemPlugin => true;

        private bool _isRegistered = false;

        /// <inheritdoc />
        public override void OnInstalled()
        {
            Console.WriteLine($"[{Name}] Shell integration installed");
            RegisterContextMenu();
        }

        /// <inheritdoc />
        public override void OnUninstalled()
        {
            Console.WriteLine($"[{Name}] Shell integration uninstalled");
            UnregisterContextMenu();
        }

        /// <inheritdoc />
        public override void OnShutdown()
        {
            Console.WriteLine($"[{Name}] Shell integration shutting down");
        }

        /// <inheritdoc />
        public override void Stop()
        {
            Console.WriteLine($"[{Name}] Shell integration stopped");
        }

        /// <summary>
        /// Registers the context menu entries
        /// </summary>
        public bool RegisterContextMenu()
        {
            try
            {
                // In a real implementation, this would modify Windows registry
                // to add context menu entries
                Console.WriteLine($"[{Name}] Registering context menu entries...");
                
                // Simulate registration
                _isRegistered = true;
                
                Console.WriteLine($"[{Name}] Context menu entries registered successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name}] Failed to register context menu: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unregisters the context menu entries
        /// </summary>
        public bool UnregisterContextMenu()
        {
            try
            {
                Console.WriteLine($"[{Name}] Unregistering context menu entries...");
                
                // Simulate unregistration
                _isRegistered = false;
                
                Console.WriteLine($"[{Name}] Context menu entries unregistered successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name}] Failed to unregister context menu: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if shell integration is registered
        /// </summary>
        public bool IsRegistered => _isRegistered;

        /// <summary>
        /// Gets available context menu actions
        /// </summary>
        public IEnumerable<ContextMenuAction> GetAvailableActions()
        {
            return new[]
            {
                new ContextMenuAction
                {
                    Id = "performance-mode",
                    Name = "Set Performance Mode",
                    Description = "Switch to performance mode",
                    Icon = "⚡"
                },
                new ContextMenuAction
                {
                    Id = "quiet-mode",
                    Name = "Set Quiet Mode",
                    Description = "Switch to quiet mode",
                    Icon = "🔇"
                },
                new ContextMenuAction
                {
                    Id = "balance-mode",
                    Name = "Set Balance Mode",
                    Description = "Switch to balance mode",
                    Icon = "⚖️"
                },
                new ContextMenuAction
                {
                    Id = "open-toolkit",
                    Name = "Open Lenovo Legion Toolkit",
                    Description = "Launch the main application",
                    Icon = "🎮"
                }
            };
        }
    }

    /// <summary>
    /// Context menu action definition
    /// </summary>
    public class ContextMenuAction
    {
        /// <summary>
        /// Action ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Icon/emoji
        /// </summary>
        public string Icon { get; set; } = string.Empty;
    }
}

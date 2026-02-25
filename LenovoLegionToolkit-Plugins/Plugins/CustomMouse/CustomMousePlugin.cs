using System;
using System.Collections.Generic;
using LenovoLegionToolkit.Plugins.SDK;

namespace LenovoLegionToolkit.Plugins.CustomMouse
{
    /// <summary>
    /// Custom Mouse plugin - Allows customization of mouse settings
    /// </summary>
    public class CustomMousePlugin : PluginBase, IStatefulPlugin
    {
        /// <inheritdoc />
        public override string Id => "custom-mouse";

        /// <inheritdoc />
        public override string Name => "Custom Mouse";

        /// <inheritdoc />
        public override string Version => "1.0.0";

        /// <inheritdoc />
        public override string Author => "LenovoLegionToolkit Team";

        /// <inheritdoc />
        public override string Description => "Customize mouse settings including DPI, polling rate, and button mappings";

        /// <inheritdoc />
        public override string RepositoryUrl => "https://github.com/Crs10259/LenovoLegionToolkit-Plugins";

        /// <inheritdoc />
        public int StateVersion => 1;

        private MouseSettings _settings = new();
        private bool _isInitialized = false;

        /// <summary>
        /// Current mouse settings
        /// </summary>
        public MouseSettings Settings => _settings;

        /// <inheritdoc />
        public override void OnInstalled()
        {
            Console.WriteLine($"[{Name}] Plugin installed");
            // Initialize default settings
            _settings = new MouseSettings
            {
                Dpi = 1600,
                PollingRate = 1000,
                ButtonMappings = new Dictionary<int, MouseButtonAction>()
            };
        }

        /// <inheritdoc />
        public override void OnUninstalled()
        {
            Console.WriteLine($"[{Name}] Plugin uninstalled");
            _isInitialized = false;
        }

        /// <inheritdoc />
        public override void OnShutdown()
        {
            Console.WriteLine($"[{Name}] Plugin shutting down");
            // Save settings before shutdown
            SaveSettings();
        }

        /// <inheritdoc />
        public override void Stop()
        {
            Console.WriteLine($"[{Name}] Plugin stopped");
        }

        /// <summary>
        /// Sets the mouse DPI
        /// </summary>
        public bool SetDpi(int dpi)
        {
            if (dpi < 100 || dpi > 16000)
            {
                Console.WriteLine($"[{Name}] Invalid DPI value: {dpi}");
                return false;
            }

            _settings.Dpi = dpi;
            Console.WriteLine($"[{Name}] DPI set to {dpi}");
            return true;
        }

        /// <summary>
        /// Sets the polling rate
        /// </summary>
        public bool SetPollingRate(int rate)
        {
            var validRates = new[] { 125, 250, 500, 1000 };
            if (!Array.Exists(validRates, r => r == rate))
            {
                Console.WriteLine($"[{Name}] Invalid polling rate: {rate}");
                return false;
            }

            _settings.PollingRate = rate;
            Console.WriteLine($"[{Name}] Polling rate set to {rate}Hz");
            return true;
        }

        /// <summary>
        /// Maps a mouse button to an action
        /// </summary>
        public void MapButton(int buttonId, MouseButtonAction action)
        {
            _settings.ButtonMappings[buttonId] = action;
            Console.WriteLine($"[{Name}] Button {buttonId} mapped to {action}");
        }

        /// <inheritdoc />
        public byte[] SerializeState()
        {
            // Simple JSON serialization
            var json = System.Text.Json.JsonSerializer.Serialize(_settings);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        /// <inheritdoc />
        public bool DeserializeState(byte[] stateData, string previousVersion)
        {
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(stateData);
                var settings = System.Text.Json.JsonSerializer.Deserialize<MouseSettings>(json);
                if (settings != null)
                {
                    _settings = settings;
                    Console.WriteLine($"[{Name}] State restored from version {previousVersion}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name}] Failed to deserialize state: {ex.Message}");
            }
            return false;
        }

        private void SaveSettings()
        {
            // Settings are automatically saved through state serialization
            Console.WriteLine($"[{Name}] Settings saved");
        }
    }

    /// <summary>
    /// Mouse settings data
    /// </summary>
    public class MouseSettings
    {
        /// <summary>
        /// Mouse DPI
        /// </summary>
        public int Dpi { get; set; } = 1600;

        /// <summary>
        /// Polling rate in Hz
        /// </summary>
        public int PollingRate { get; set; } = 1000;

        /// <summary>
        /// Button mappings
        /// </summary>
        public Dictionary<int, MouseButtonAction> ButtonMappings { get; set; } = new();
    }

    /// <summary>
    /// Mouse button actions
    /// </summary>
    public enum MouseButtonAction
    {
        /// <summary>
        /// No action
        /// </summary>
        None,

        /// <summary>
        /// Left click
        /// </summary>
        LeftClick,

        /// <summary>
        /// Right click
        /// </summary>
        RightClick,

        /// <summary>
        /// Middle click
        /// </summary>
        MiddleClick,

        /// <summary>
        /// DPI switch
        /// </summary>
        DpiSwitch,

        /// <summary>
        /// Profile switch
        /// </summary>
        ProfileSwitch,

        /// <summary>
        /// Macro execution
        /// </summary>
        Macro
    }
}

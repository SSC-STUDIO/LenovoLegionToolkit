using System.Collections.Generic;
using LenovoLegionToolkit.Plugins.SDK;

namespace LenovoLegionToolkit.Plugins.CustomMouse
{
    public class CustomMousePlugin : LenovoLegionToolkit.Plugins.SDK.PluginBase
    {
        public override string Id => "custom-mouse";

        public override string Name => "Custom Mouse";

        public override string Description => "Customize mouse settings including DPI, polling rate, and button mappings";

        public override string Icon => "Mouse24";

        public override bool IsSystemPlugin => false;

        private MouseSettings _settings = new();

        public MouseSettings Settings => _settings;

        public override void OnInstalled()
        {
            _settings = new MouseSettings
            {
                Dpi = 1600,
                PollingRate = 1000
            };
        }

        public bool SetDpi(int dpi)
        {
            if (dpi < 100 || dpi > 16000)
                return false;
            _settings.Dpi = dpi;
            return true;
        }

        public bool SetPollingRate(int rate)
        {
            var validRates = new[] { 125, 250, 500, 1000 };
            if (!System.Array.Exists(validRates, r => r == rate))
                return false;
            _settings.PollingRate = rate;
            return true;
        }
    }

    public class MouseSettings
    {
        public int Dpi { get; set; } = 1600;
        public int PollingRate { get; set; } = 1000;
        public Dictionary<int, int> ButtonMappings { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LenovoLegionToolkit.Lib.Settings;

public class ApplicationSettings : AbstractSettings<ApplicationSettings.ApplicationSettingsStore>
{
    public class Notifications
    {
        public bool UpdateAvailable { get; set; } = true;
        public bool CapsNumLock { get; set; }
        public bool FnLock { get; set; }
        public bool TouchpadLock { get; set; } = true;
        public bool KeyboardBacklight { get; set; } = true;
        public bool CameraLock { get; set; } = true;
        public bool Microphone { get; set; } = true;
        public bool PowerMode { get; set; }
        public bool RefreshRate { get; set; } = true;
        public bool ACAdapter { get; set; }
        public bool SmartKey { get; set; }
        public bool AutomationNotification { get; set; } = true;
    }

    public class ApplicationSettingsStore
    {
        public Theme Theme { get; set; }
        public RGBColor? AccentColor { get; set; }
        public AccentColorSource AccentColorSource { get; set; }
        public PowerModeMappingMode PowerModeMappingMode { get; set; } = PowerModeMappingMode.WindowsPowerMode;
        public Dictionary<PowerModeState, Guid> PowerPlans { get; set; } = [];
        public Dictionary<PowerModeState, WindowsPowerMode> PowerModes { get; set; } = [];
        public bool MinimizeToTray { get; set; } = true;
        public bool MinimizeOnClose { get; set; }
        public WindowSize? WindowSize { get; set; }
        public bool DontShowNotifications { get; set; }
        public NotificationPosition NotificationPosition { get; set; } = NotificationPosition.BottomCenter;
        public NotificationDuration NotificationDuration { get; set; } = NotificationDuration.Normal;
        public bool NotificationAlwaysOnTop { get; set; }
        public bool NotificationOnAllScreens { get; set; }
        public Notifications Notifications { get; set; } = new();
        public TemperatureUnit TemperatureUnit { get; set; }
        public List<RefreshRate> ExcludedRefreshRates { get; set; } = [];
        public WarrantyInfo? WarrantyInfo { get; set; }
        public Guid? SmartKeySinglePressActionId { get; set; }
        public Guid? SmartKeyDoublePressActionId { get; set; }
        public List<Guid> SmartKeySinglePressActionList { get; set; } = [];
        public List<Guid> SmartKeyDoublePressActionList { get; set; } = [];
        public bool SynchronizeBrightnessToAllPowerPlans { get; set; }
        public ModifierKey SmartFnLockFlags { get; set; }
        public bool ResetBatteryOnSinceTimerOnReboot { get; set; }
        public bool DisableUnsupportedHardwareWarning { get; set; }
        public List<CustomCleanupRule> CustomCleanupRules { get; set; } = [];
    }

    public ApplicationSettings() : base("settings.json")
    {
        JsonSerializerSettings.Converters.Add(new LegacyPowerPlanInstanceIdToGuidConverter());
    }
}

public class CustomCleanupRule
{
    public string DirectoryPath { get; set; } = string.Empty;
    public List<string> Extensions { get; set; } = [];
    public bool Recursive { get; set; } = true;
}

internal class LegacyPowerPlanInstanceIdToGuidConverter : JsonConverter // Introduced in 2.12.0
{
    public override bool CanWrite => false;

    public override bool CanConvert(Type objectType) => objectType == typeof(Guid);

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) => throw new InvalidOperationException();

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var value = reader.Value?.ToString() ?? string.Empty;

        const string prefix = "Microsoft:PowerPlan\\{";
        const string suffix = "}";

        var prefixIndex = value.IndexOf(prefix, StringComparison.InvariantCulture);
        var suffixIndex = prefixIndex >= 0
            ? value.IndexOf(suffix, prefixIndex + prefix.Length, StringComparison.InvariantCulture)
            : -1;

        // Ensure suffix is found after prefix (GUID can immediately follow prefix)
        if (prefixIndex >= 0 && suffixIndex >= prefixIndex + prefix.Length)
        {
            var start = prefixIndex + prefix.Length;
            var length = suffixIndex - start;
            // Only extract if we have a non-empty substring and valid bounds
            if (length > 0 && start >= 0 && start + length <= value.Length)
            {
                value = value.Substring(start, length);
            }
            else
            {
                // Invalid format: prefix and suffix found but no valid content between them
                return Guid.Empty;
            }
        }

        // Handle case where the expected format is not found or value is not a valid GUID
        if (string.IsNullOrWhiteSpace(value))
            return Guid.Empty;

        if (Guid.TryParse(value, out var guid))
            return guid;

        // If parsing fails, return empty GUID instead of throwing FormatException
        // This allows the application to continue with a default value
        return Guid.Empty;
    }
}

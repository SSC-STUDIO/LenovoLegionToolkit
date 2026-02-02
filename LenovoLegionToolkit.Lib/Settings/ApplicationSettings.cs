using System;
using System.Collections.Generic;
using System.IO;
using LenovoLegionToolkit.Lib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public WindowBackdropStyle WindowBackdropStyle { get; set; } = WindowBackdropStyle.Windows;
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
        public bool AnimationsEnabled { get; set; } = true;
        public double AnimationSpeed { get; set; } = 2.0;
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
        public bool ExtensionsEnabled { get; set; } = false;
        public List<string> InstalledExtensions { get; set; } = [];
        public List<string> PendingDeletionExtensions { get; set; } = [];
        public List<string>? SelectedCleanupActions { get; set; }
        public List<string>? SelectedOptimizationActions { get; set; }
        public int LastWindowsOptimizationPageMode { get; set; }
        public Dictionary<string, bool> NavigationItemsVisibility { get; set; } = new()
        {
            { "keyboard", true },
            { "battery", true },
            { "automation", true },
            { "macro", true },
            { "windowsOptimization", true },
            { "pluginExtensions", true },

            { "about", true }
        };
    }

    public ApplicationSettings() : base("settings.json")
    {
        JsonSerializerSettings.Converters.Add(new LegacyPowerPlanInstanceIdToGuidConverter());
    }

    public override ApplicationSettingsStore? LoadStore()
    {
        var store = base.LoadStore();
        var settingsStorePath = Path.Combine(Folders.AppData, "settings.json");
        
        if (store is null)
            return null;
        
        return store;
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
        var originalValue = reader.Value?.ToString() ?? string.Empty;
        var value = originalValue;

        const string prefix = "Microsoft:PowerPlan\\{";
        const string suffix = "}";

        var prefixIndex = value.IndexOf(prefix, StringComparison.InvariantCulture);

        // Validate prefixIndex and calculate safe start position before calling IndexOf for suffix
        // This prevents integer overflow and bounds issues when calculating prefixIndex + prefix.Length
        int suffixIndex = -1;
        if (prefixIndex >= 0)
        {
            // Calculate the start position for suffix search, checking for overflow and bounds
            var suffixStartPos = prefixIndex + prefix.Length;
            if (suffixStartPos >= 0 && suffixStartPos <= value.Length && suffixStartPos >= prefixIndex)
            {
                suffixIndex = value.IndexOf(suffix, suffixStartPos, StringComparison.InvariantCulture);
            }
        }

        // Ensure suffix is found after prefix (GUID can immediately follow prefix)
        // Must check suffixIndex >= 0 first to ensure suffix was actually found before using it in calculations
        if (prefixIndex >= 0 && suffixIndex >= 0 && suffixIndex >= prefixIndex + prefix.Length)
        {
            var start = prefixIndex + prefix.Length;
            var length = suffixIndex - start;

            // Validate bounds: start must be within string, and start + length must not exceed string length
            // Additional validation is redundant here since we already validated suffixStartPos above,
            // but kept for defensive programming and clarity
            if (start >= 0 && start < value.Length && length >= 0 && start + length <= value.Length)
            {
                if (length > 0)
                {
                    value = value.Substring(start, length);
                }
                else
                {
                    // Invalid format: prefix and suffix found but no content between them
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"LegacyPowerPlanInstanceIdToGuidConverter: Invalid format - prefix and suffix found but no GUID content between them. Original value: '{originalValue}'");
                    return Guid.Empty;
                }
            }
            else
            {
                // Invalid bounds: substring extraction would be out of range
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"LegacyPowerPlanInstanceIdToGuidConverter: Invalid bounds for substring extraction. Start: {start}, Length: {length}, Value length: {value.Length}. Original value: '{originalValue}'");
                return Guid.Empty;
            }
        }

        // Handle case where the expected format is not found or value is not a valid GUID
        if (string.IsNullOrWhiteSpace(value))
        {
            if (Log.Instance.IsTraceEnabled && !string.IsNullOrWhiteSpace(originalValue))
                Log.Instance.Trace($"LegacyPowerPlanInstanceIdToGuidConverter: Empty or whitespace GUID value after extraction. Original value: '{originalValue}'");
            return Guid.Empty;
        }

        if (Guid.TryParse(value, out var guid))
            return guid;

        // If parsing fails, log the error but return empty GUID to allow the application to continue
        // This maintains backward compatibility while alerting to potential data corruption
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"LegacyPowerPlanInstanceIdToGuidConverter: Failed to parse GUID from value '{value}' (original: '{originalValue}'). Returning Guid.Empty. This may indicate corrupt settings data.");

        return Guid.Empty;
    }
}

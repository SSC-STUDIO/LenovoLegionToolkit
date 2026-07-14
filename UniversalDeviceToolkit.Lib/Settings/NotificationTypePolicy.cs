using System;
using System.Collections.Generic;

namespace UniversalDeviceToolkit.Lib.Settings;

/// <summary>
/// Per-notification-type policy: enable, optional persistence hint, and severity.
/// Position remains a global setting (default bottom-right); OSD stays separate.
/// </summary>
public sealed class NotificationTypePolicy
{
    public bool Enabled { get; set; } = true;
    public bool Persist { get; set; }
    public NotificationPriority Severity { get; set; } = NotificationPriority.Normal;
}

public static class NotificationTypePolicyStore
{
    public static NotificationTypePolicy GetOrDefault(
        Dictionary<string, NotificationTypePolicy>? policies,
        string key,
        bool legacyEnabled = true)
    {
        if (policies is not null &&
            policies.TryGetValue(key, out var policy) &&
            policy is not null)
        {
            return policy;
        }

        return new NotificationTypePolicy
        {
            Enabled = legacyEnabled,
            Persist = false,
            Severity = NotificationPriority.Normal
        };
    }

    public static Dictionary<string, NotificationTypePolicy> CreateDefaults() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["UpdateAvailable"] = new() { Enabled = true },
            ["CapsNumLock"] = new() { Enabled = false },
            ["FnLock"] = new() { Enabled = false },
            ["TouchpadLock"] = new() { Enabled = true },
            ["KeyboardBacklight"] = new() { Enabled = true },
            ["CameraLock"] = new() { Enabled = true },
            ["Microphone"] = new() { Enabled = true },
            ["PowerMode"] = new() { Enabled = false },
            ["RefreshRate"] = new() { Enabled = true },
            ["ACAdapter"] = new() { Enabled = false },
            ["SmartKey"] = new() { Enabled = false },
            ["AutomationNotification"] = new() { Enabled = true },
        };
}

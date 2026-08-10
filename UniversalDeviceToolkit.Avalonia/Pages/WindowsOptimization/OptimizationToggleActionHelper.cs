using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalDeviceToolkit.Avalonia.Pages.WindowsOptimization;

internal static class OptimizationToggleActionHelper
{
    private const string EnableSuffix = ".enable";
    private const string DisableSuffix = ".disable";

    public static bool IsEnableAction(string key) =>
        key.EndsWith(EnableSuffix, StringComparison.OrdinalIgnoreCase);

    public static bool IsDisableAction(string key) =>
        key.EndsWith(DisableSuffix, StringComparison.OrdinalIgnoreCase);

    public static bool IsToggleAction(string key) =>
        IsEnableAction(key) || IsDisableAction(key);

    public static string? GetTogglePairBaseKey(string key)
    {
        if (IsEnableAction(key))
            return key[..^EnableSuffix.Length];

        if (IsDisableAction(key))
            return key[..^DisableSuffix.Length];

        return null;
    }

    public static string ResolveTargetActionKey(string actionKey, bool desiredSelected)
    {
        if (IsEnableAction(actionKey))
            return desiredSelected ? actionKey : actionKey[..^EnableSuffix.Length] + DisableSuffix;

        if (IsDisableAction(actionKey))
            return desiredSelected ? actionKey[..^DisableSuffix.Length] + EnableSuffix : actionKey;

        return actionKey;
    }

    public static IReadOnlyList<(OptimizationActionViewModel Enable, OptimizationActionViewModel Disable)> FindTogglePairs(
        IEnumerable<OptimizationActionViewModel> actions)
    {
        var actionList = actions.ToList();
        var byKey = actionList.ToDictionary(action => action.Key, StringComparer.OrdinalIgnoreCase);
        var pairs = new List<(OptimizationActionViewModel Enable, OptimizationActionViewModel Disable)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var action in actionList)
        {
            var baseKey = GetTogglePairBaseKey(action.Key);
            if (baseKey is null || !seen.Add(baseKey))
                continue;

            var enableKey = baseKey + EnableSuffix;
            var disableKey = baseKey + DisableSuffix;
            if (byKey.TryGetValue(enableKey, out var enable) && byKey.TryGetValue(disableKey, out var disable))
                pairs.Add((enable, disable));
        }

        return pairs;
    }

    public static void ApplyTogglePairPresentation(
        bool? featureEnabled,
        OptimizationActionViewModel enable,
        OptimizationActionViewModel disable)
    {
        if (!featureEnabled.HasValue)
        {
            // Do not present an unknown feature as disabled. Keep one stable
            // row visible so a plugin with no state probe cannot create two
            // contradictory checkboxes.
            enable.IsVisible = true;
            disable.IsVisible = false;
            enable.IsEnabled = false;
            disable.IsEnabled = false;
            enable.CanEdit = false;
            disable.CanEdit = false;
            enable.IsSelected = false;
            disable.IsSelected = false;
            enable.IsApplied = null;
            disable.IsApplied = null;
            return;
        }

        enable.IsVisible = !featureEnabled.Value;
        disable.IsVisible = featureEnabled.Value;
        enable.IsEnabled = true;
        disable.IsEnabled = true;
        enable.CanEdit = true;
        disable.CanEdit = true;

        enable.IsSelected = false;
        disable.IsSelected = featureEnabled.Value;

        // The visible row represents the current feature state. The hidden
        // counterpart is never a pending change until it becomes visible.
        enable.IsApplied = false;
        disable.IsApplied = featureEnabled;
    }

    public static (OptimizationActionViewModel Enable, OptimizationActionViewModel Disable)? FindTogglePair(
        OptimizationActionViewModel action,
        IEnumerable<OptimizationActionViewModel> actions)
    {
        var baseKey = GetTogglePairBaseKey(action.Key);
        if (baseKey is null)
            return null;

        foreach (var pair in FindTogglePairs(actions))
        {
            if (string.Equals(pair.Enable.Key, action.Key, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pair.Disable.Key, action.Key, StringComparison.OrdinalIgnoreCase))
            {
                return pair;
            }
        }

        return null;
    }

    public static bool GetRecommendedSelectedState(
        OptimizationActionViewModel action,
        IEnumerable<OptimizationActionViewModel> actions)
    {
        var pair = FindTogglePair(action, actions);
        if (pair is null)
            return action.Recommended;

        // A pair describes one feature state. Prefer the side explicitly marked
        // recommended instead of reading the recommendation from the visible row.
        if (pair.Value.Enable.Recommended != pair.Value.Disable.Recommended)
            return pair.Value.Enable.Recommended;

        return action.Recommended;
    }
}

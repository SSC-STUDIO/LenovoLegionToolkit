using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;

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
        bool featureEnabled,
        OptimizationActionViewModel enable,
        OptimizationActionViewModel disable)
    {
        enable.IsVisible = !featureEnabled;
        disable.IsVisible = featureEnabled;

        enable.IsSelected = false;
        disable.IsSelected = featureEnabled;
    }

    public static (OptimizationActionViewModel Enable, OptimizationActionViewModel Disable)? FindTogglePair(
        OptimizationActionViewModel action,
        IEnumerable<OptimizationActionViewModel> actions)
    {
        var baseKey = GetTogglePairBaseKey(action.Key);
        if (baseKey is null)
            return null;

        var pairs = FindTogglePairs(actions);
        return pairs.FirstOrDefault(pair =>
            string.Equals(pair.Enable.Key, action.Key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(pair.Disable.Key, action.Key, StringComparison.OrdinalIgnoreCase));
    }
}
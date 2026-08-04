using System.Globalization;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Stable action-key rules shared by Avalonia feature pages and their host bridge.
/// Keeping parsing here prevents a UI refresh from silently accepting an action for
/// a different macro slot or an invalid key.
/// </summary>
public static class FeatureActionContract
{
    public const string MacroRecordPrefix = "macro-record:";
    public const string MacroPlayPrefix = "macro-key:";
    public const string OptimizationApplyRecommendedActionKey = "optimization-apply-recommended";
    public const string CleanupScanActionKey = "cleanup-scan";
    public const string CleanupRunActionKey = "cleanup-run";
    public const string CleanupClearActionKey = "cleanup-clear";

    private static readonly ulong[] MacroKeys =
        [0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69];

    public static bool TryParseMacroRecordKey(string actionKey, out ulong key) =>
        TryParseMacroKey(actionKey, MacroRecordPrefix, out key);

    public static bool TryParseMacroPlayKey(string actionKey, out ulong key) =>
        TryParseMacroKey(actionKey, MacroPlayPrefix, out key);

    /// <summary>
    /// A checkbox can represent an optimization action only when the host can
    /// revert it. One-way actions must remain command buttons in every client.
    /// </summary>
    public static bool IsToggleAction(bool hasRollback) => hasRollback;

    public static bool IsCleanupAction(string actionKey) =>
        actionKey.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase);

    private static bool TryParseMacroKey(string actionKey, string prefix, out ulong key)
    {
        key = 0;
        if (!actionKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        return ulong.TryParse(
                   actionKey[prefix.Length..],
                   NumberStyles.HexNumber,
                   CultureInfo.InvariantCulture,
                   out key)
               && MacroKeys.Contains(key);
    }
}

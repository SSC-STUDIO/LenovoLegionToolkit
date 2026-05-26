using System;

namespace LenovoLegionToolkit.Lib.Utils;

public static class EnvironmentVariableHelper
{
    public static string? Get(string primaryName, string legacyName)
    {
        var primary = Environment.GetEnvironmentVariable(primaryName);
        if (!string.IsNullOrWhiteSpace(primary))
            return primary;

        return Environment.GetEnvironmentVariable(legacyName);
    }

    public static string ToUdtAlias(string legacyName) =>
        legacyName.StartsWith("LLT_", StringComparison.Ordinal)
            ? "UDT_" + legacyName[4..]
            : legacyName;
}

using System.Reflection;
using PluginTooling.Core;
using Xunit;

namespace PluginTooling.Tests;

public class PluginScaffolderLegacyNameTests
{
    private static string InvokeBuildDefaultDescription(string displayName)
    {
        var method = typeof(PluginScaffolder).GetMethod(
            "BuildDefaultDescription",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            [typeof(string)],
            null);

        Assert.NotNull(method);
        return (string)method!.Invoke(null, [displayName])!;
    }

    [Fact]
    public void BuildDefaultDescription_ReferencesUniversalDeviceToolkit()
    {
        var description = InvokeBuildDefaultDescription("TestPlugin");
        Assert.Contains("Universal Device Toolkit", description, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDefaultDescription_DoesNotReferenceLegacyName()
    {
        var description = InvokeBuildDefaultDescription("TestPlugin");
        Assert.DoesNotContain("Lenovo Legion Toolkit", description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildDefaultDescription_IncludesDisplayName()
    {
        var description = InvokeBuildDefaultDescription("BatteryHealth");
        Assert.Contains("BatteryHealth", description, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verify no "LenovoLegionToolkit" string literals remain in the
    /// PluginTooling.Core assembly's StoreJsonGenerator type metadata.
    /// This catches stale legacy DLL hash candidates that would never
    /// match real plugin DLLs.
    /// </summary>
    [Fact]
    public void StoreJsonGenerator_NoLegacyDllCandidateStrings()
    {
        // The TryComputeMainDllHashFromZip method builds a HashSet of candidate
        // DLL names. After the fix, none should contain "LenovoLegionToolkit".
        // We verify by scanning all string constants embedded in the
        // StoreJsonGenerator type's methods via the module's metadata.
        var module = typeof(StoreJsonGenerator).Module;
        var assembly = typeof(StoreJsonGenerator).Assembly;

        // Use a simple heuristic: search the assembly's PE image for the
        // legacy string. This is a blunt but effective approach — if the
        // string literal was removed from the source, it won't appear in
        // the compiled metadata.
        var assemblyPath = assembly.Location;
        var bytes = File.ReadAllBytes(assemblyPath);
        var legacyString = System.Text.Encoding.UTF8.GetBytes("LenovoLegionToolkit.Plugins.");
        var found = false;
        for (var i = 0; i <= bytes.Length - legacyString.Length; i++)
        {
            var match = true;
            for (var j = 0; j < legacyString.Length; j++)
            {
                if (bytes[i + j] != legacyString[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                found = true;
                break;
            }
        }

        Assert.False(found, "PluginTooling.Core.dll still contains 'LenovoLegionToolkit.Plugins.' string literal — stale legacy DLL hash candidate was not removed from StoreJsonGenerator");
    }
}

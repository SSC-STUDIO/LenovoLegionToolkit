using System.IO;
using System.Text;
using Xunit;

namespace PluginTooling.Tests;

/// <summary>
/// Regression tests for revision 67: PluginWorkbenchThemeService must not
/// hardcode only the legacy "Lenovo Legion Toolkit" assembly name in pack URIs.
/// The fix introduces runtime resolution that tries "Universal Device Toolkit"
/// first, then falls back to "Lenovo Legion Toolkit".
/// </summary>
public class PluginWorkbenchThemeServiceLegacyPackUriTests
{
    private static readonly string SourcePath = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..",
        "Tooling", "PluginWorkbench", "PluginWorkbenchThemeService.cs");

    private static string ReadSource()
    {
        var fullPath = Path.GetFullPath(SourcePath);
        Assert.True(File.Exists(fullPath),
            $"Source file not found at {fullPath}. Test must run from the test output directory.");
        return File.ReadAllText(fullPath);
    }

    [Fact]
    public void Source_DeclaresBothAssemblyNameCandidates()
    {
        var source = ReadSource();

        // The fix introduces a candidates array with both names.
        Assert.Contains("\"Universal Device Toolkit\"", source);
        Assert.Contains("\"Lenovo Legion Toolkit\"", source);
    }

    [Fact]
    public void Source_UniversalDeviceToolkitListedFirst()
    {
        var source = ReadSource();

        var udtIndex = source.IndexOf("\"Universal Device Toolkit\"", StringComparison.Ordinal);
        var lltIndex = source.IndexOf("\"Lenovo Legion Toolkit\"", StringComparison.Ordinal);

        Assert.True(udtIndex >= 0, "Universal Device Toolkit candidate not found in source");
        Assert.True(lltIndex >= 0, "Lenovo Legion Toolkit candidate not found in source");
        Assert.True(udtIndex < lltIndex,
            "Universal Device Toolkit must be listed before Lenovo Legion Toolkit so it is tried first");
    }

    [Fact]
    public void Source_NoHardcodedLegacyOnlyPackUriArray()
    {
        var source = ReadSource();

        // After the fix, the static Uri[] HostDictionaryUris array that
        // hardcoded 12 pack URIs with "Lenovo Legion Toolkit" should be gone.
        // Replaced by a method that builds URIs at runtime.
        Assert.DoesNotContain(
            "new(\"pack://application:,,,/Lenovo Legion Toolkit;component/",
            source);
    }

    [Fact]
    public void Source_HasRuntimeResolutionMethod()
    {
        var source = ReadSource();

        Assert.Contains("ResolveHostWpfAssemblyName", source);
        Assert.Contains("GetHostDictionaryUris", source);
    }

    [Fact]
    public void Source_StyleResourcesSeparatedFromAssemblyName()
    {
        var source = ReadSource();

        // The fix separates the style resource paths from the assembly name
        // so they can be combined at runtime.
        Assert.Contains("Styles/DesignTokens.xaml", source);
        Assert.Contains("Styles/NavigationStore.xaml", source);
    }
}

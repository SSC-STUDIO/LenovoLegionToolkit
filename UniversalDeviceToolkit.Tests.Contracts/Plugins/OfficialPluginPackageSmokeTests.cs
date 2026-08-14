using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

/// <summary>
/// Smoke: when the local monorepo catalog and release assets are present, verify
/// catalog integrity digests match the packaged ZIPs and main DLLs.
/// Skips cleanly when generated assets are not available.
/// </summary>
[Trait("Category", TestCategories.Guard)]
public class OfficialPluginPackageSmokeTests
{
    [Fact]
    public void OfficialReleaseAssets_ShouldMatchStoreJsonIntegrityHashes_WhenAvailable()
    {
        var pluginsRoot = FindPluginsRepositoryRoot();
        if (pluginsRoot is null)
        {
            // Core-only checkouts (Release CI without generated plugin assets) skip — host still
            // fail-closes official installs without hashes at runtime.
            return;
        }

        var storePath = Path.Combine(pluginsRoot, "Plugins", ".build", "catalog", "store.json");
        var assetsRoot = Path.Combine(pluginsRoot, "Plugins", ".build", "release-assets");
        if (!File.Exists(storePath))
        {
            return;
        }

        if (!Directory.Exists(assetsRoot))
        {
            // A checkout without packaged assets (for example, a host-only build that
            // stages runtime assemblies but never packages locally) has nothing to
            // compare hashes against here; runtime still fail-closes without hashes.
            return;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(storePath));
        var plugins = doc.RootElement.GetProperty("plugins");
        var checkedCount = 0;

        foreach (var plugin in plugins.EnumerateArray())
        {
            var status = plugin.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : "Active";
            if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                continue;

            var id = plugin.GetProperty("id").GetString()!;
            var version = plugin.GetProperty("version").GetString()!;
            var zipHash = plugin.TryGetProperty("zipHash", out var zh) ? zh.GetString() : null;
            var fileHash = plugin.TryGetProperty("fileHash", out var fh) ? fh.GetString() : null;

            zipHash.Should().NotBeNullOrWhiteSpace($"Active plugin {id} must have zipHash in store.json");
            fileHash.Should().NotBeNullOrWhiteSpace($"Active plugin {id} must have fileHash in store.json");

            var zipPath = Path.Combine(assetsRoot, $"{id}-v{version}.zip");
            if (!File.Exists(zipPath))
            {
                // Store entry without local asset — skip file comparison for this id.
                continue;
            }

            var actualZip = ComputeSha256Hex(zipPath);
            actualZip.Should().BeEquivalentTo(zipHash, because: $"store zipHash must match {Path.GetFileName(zipPath)}");

            var actualDll = TryComputeMainDllHashFromZip(zipPath, id);
            actualDll.Should().NotBeNull($"ZIP {zipPath} must contain a main plugin DLL");
            actualDll.Should().BeEquivalentTo(fileHash, because: $"store fileHash must match main DLL in {Path.GetFileName(zipPath)}");

            // Host verifier accepts matching expected/actual digests.
            PluginPackageIntegrity.TryVerifyExpectedHash(zipHash, actualZip, requireWhenMissing: true, out var zipFail)
                .Should().BeTrue(zipFail);
            PluginPackageIntegrity.TryVerifyExpectedHash(fileHash, actualDll, requireWhenMissing: true, out var dllFail)
                .Should().BeTrue(dllFail);

            checkedCount++;
        }

        checkedCount.Should().BeGreaterThan(0, "at least one Active plugin with local release-assets should be verified");
    }

    private static string ComputeSha256Hex(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string? TryComputeMainDllHashFromZip(string zipPath, string pluginId)
    {
        var folder = ToPascal(pluginId);
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            $"UniversalDeviceToolkit.Plugins.{folder}.dll",
            $"LenovoLegionToolkit.Plugins.{folder}.dll",
            $"UniversalDeviceToolkit.Plugins.{pluginId}.dll",
            $"LenovoLegionToolkit.Plugins.{pluginId}.dll",
            $"UniversalDeviceToolkit.Plugins.{pluginId.Replace("-", "", StringComparison.Ordinal)}.dll",
            $"LenovoLegionToolkit.Plugins.{pluginId.Replace("-", "", StringComparison.Ordinal)}.dll",
        };

        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue;
            if (!candidates.Contains(Path.GetFileName(entry.FullName)))
                continue;

            using var stream = entry.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return Convert.ToHexString(SHA256.HashData(ms.ToArray())).ToLowerInvariant();
        }

        return null;
    }

    private static string ToPascal(string pluginId)
    {
        var parts = pluginId.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Concat(parts.Select(static p =>
            p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..]));
    }

    private static string? FindPluginsRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "UniversalDeviceToolkit.sln")) &&
                Directory.Exists(Path.Combine(dir.FullName, "Plugins", "Official")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}

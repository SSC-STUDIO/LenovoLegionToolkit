using System;
using System.IO;
using System.Threading.Tasks;
using LenovoLegionToolkit.Plugins.ViveTool.Services;
using Xunit;

namespace LenovoLegionToolkit.Plugins.ViveTool.Tests;

public class ViveToolServicePathTests
{
    [Fact]
    public async Task GetViveToolPathAsync_UsesBundledRuntimeByDefault()
    {
        var service = new ViveToolService();
        await service.SetViveToolPathAsync(string.Empty);

        var resolvedPath = await service.GetViveToolPathAsync();
        var assemblyDir = Path.GetDirectoryName(typeof(ViveToolService).Assembly.Location) ?? AppContext.BaseDirectory;
        var bundledPath = Path.Combine(assemblyDir, "Bundled", ViveToolService.ViveToolExeName);

        Assert.True(File.Exists(bundledPath));
        Assert.NotNull(resolvedPath);
        Assert.Equal(Path.GetFullPath(bundledPath), Path.GetFullPath(resolvedPath!));
    }

    [Fact]
    public async Task GetViveToolPathAsync_PrefersUserSpecifiedPath()
    {
        var service = new ViveToolService();
        var tempDir = Path.Combine(Path.GetTempPath(), "llt-vivetool-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var customPath = Path.Combine(tempDir, ViveToolService.ViveToolExeName);
        await File.WriteAllTextAsync(customPath, "test");

        try
        {
            var setResult = await service.SetViveToolPathAsync(customPath);
            var resolvedPath = await service.GetViveToolPathAsync();

            Assert.True(setResult);
            Assert.NotNull(resolvedPath);
            Assert.Equal(Path.GetFullPath(customPath), Path.GetFullPath(resolvedPath!));
        }
        finally
        {
            await service.SetViveToolPathAsync(string.Empty);
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // cleanup best effort
            }
        }
    }
}

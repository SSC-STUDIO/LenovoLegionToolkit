using System;
using System.IO;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Plugins.ViveTool.Services;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Tests;

public class ViveToolServicePathTests
{
    [Fact]
    public async Task GetViveToolPathAsync_UsesBundledRuntimeByDefault()
    {
        var service = new ViveToolService();
        await service.SetViveToolPathAsync(string.Empty);

        var resolvedPath = await service.GetViveToolPathAsync();
        var assemblyDir = Path.GetDirectoryName(typeof(ViveToolService).Assembly.Location) ?? AppContext.BaseDirectory;
        var bundledPath = Path.Combine(assemblyDir, "Bundled", ViveToolPathService.ViveToolExeName);

        Assert.True(File.Exists(bundledPath));
        Assert.NotNull(resolvedPath);
        Assert.Equal(Path.GetFullPath(bundledPath), Path.GetFullPath(resolvedPath!));
    }

    [Fact]
    public async Task GetViveToolPathAsync_PrefersUserSpecifiedPath()
    {
        var service = new ViveToolService();
        await using var runtime = await ViveToolTestRuntimeHelper.CreateCompleteRuntimeScopeAsync();

        try
        {
            var setResult = await service.SetViveToolPathAsync(runtime.ExePath);
            var resolvedPath = await service.GetViveToolPathAsync();

            Assert.True(setResult);
            Assert.NotNull(resolvedPath);
            Assert.Equal(Path.GetFullPath(runtime.ExePath), Path.GetFullPath(resolvedPath!));
        }
        finally
        {
            await service.SetViveToolPathAsync(string.Empty);
        }
    }
}

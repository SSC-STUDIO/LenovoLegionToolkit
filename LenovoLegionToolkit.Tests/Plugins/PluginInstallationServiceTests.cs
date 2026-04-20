using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using Moq;
using Xunit;

namespace LenovoLegionToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginInstallationServiceTests : TemporaryFileTestBase
{
    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldMarkImportedPluginAsInstalled()
    {
        // Arrange
        const string pluginId = "test-local-plugin";
        var pluginManager = new Mock<IPluginManager>();
        var service = new PluginInstallationService(pluginManager.Object);
        var pluginsRoot = CreateTempDirectory();
        var zipPath = CreatePluginZipPackage(pluginId);

        // Act
        var result = await service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        // Assert
        result.Should().BeTrue();
        pluginManager.Verify(manager => manager.InstallPlugin(pluginId), Times.Once);
    }

    [Fact]
    public async Task ExtractAndInstallPluginAsync_ShouldPlaceImportedFilesUnderLocalPluginDirectory()
    {
        // Arrange
        const string pluginId = "test-local-plugin";
        var pluginManager = new Mock<IPluginManager>();
        var service = new PluginInstallationService(pluginManager.Object);
        var pluginsRoot = CreateTempDirectory();
        var zipPath = CreatePluginZipPackage(pluginId);

        // Act
        var result = await service.ExtractAndInstallPluginAsync(zipPath, pluginsRoot);

        // Assert
        result.Should().BeTrue();

        var installedPluginDirectory = Path.Combine(pluginsRoot, "local", pluginId);
        Directory.Exists(installedPluginDirectory).Should().BeTrue();
        File.Exists(Path.Combine(installedPluginDirectory, "plugin.json")).Should().BeTrue();
        Directory.GetFiles(installedPluginDirectory, "*.dll", SearchOption.TopDirectoryOnly)
            .Should()
            .NotBeEmpty();
    }

    private string CreatePluginZipPackage(string pluginId)
    {
        var packageDirectory = CreateTempDirectory();
        var packageRoot = Path.Combine(packageDirectory, "package");
        Directory.CreateDirectory(packageRoot);

        var assemblySourcePath = Assembly.GetExecutingAssembly().Location;
        var assemblyFileName = Path.GetFileName(assemblySourcePath);
        File.Copy(assemblySourcePath, Path.Combine(packageRoot, assemblyFileName), overwrite: true);
        File.WriteAllText(
            Path.Combine(packageRoot, "plugin.json"),
            $$"""
              {
                "id": "{{pluginId}}",
                "name": "Test Local Plugin",
                "version": "1.0.0"
              }
              """);

        var zipPath = Path.Combine(packageDirectory, $"{pluginId}.zip");
        ZipFile.CreateFromDirectory(packageRoot, zipPath);
        return zipPath;
    }
}

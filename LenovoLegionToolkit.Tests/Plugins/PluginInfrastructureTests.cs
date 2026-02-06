using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace LenovoLegionToolkit.Tests.Settings;

[Trait("Category", TestCategories.Settings)]
[Trait("Category", TestCategories.Unit)]
public class ApplicationSettingsPluginTests : SettingsTestBase
{
    [Fact]
    public void PluginUpdateSettings_ShouldHaveDefaultValues()
    {
        // Arrange
        var store = CreateMockSettingsStore();

        // Assert
        store.CheckPluginUpdatesOnStartup.Should().BeTrue();
        store.AutoDownloadPluginUpdates.Should().BeFalse();
        store.NotifyOnPluginUpdate.Should().BeTrue();
        store.PluginUpdateCheckFrequencyHours.Should().Be(24);
        store.LastPluginUpdateCheckTime.Should().BeNull();
        store.PendingPluginUpdates.Should().NotBeNull();
        store.PendingPluginUpdates.Should().BeEmpty();
    }

    [Fact]
    public void PluginUpdateSettings_CanBeModified()
    {
        // Arrange
        var store = CreateMockSettingsStore();

        // Act
        store.CheckPluginUpdatesOnStartup = false;
        store.AutoDownloadPluginUpdates = true;
        store.NotifyOnPluginUpdate = false;
        store.PluginUpdateCheckFrequencyHours = 48;
        store.LastPluginUpdateCheckTime = DateTime.Now;
        store.PendingPluginUpdates.Add("plugin1");
        store.PendingPluginUpdates.Add("plugin2");

        // Assert
        store.CheckPluginUpdatesOnStartup.Should().BeFalse();
        store.AutoDownloadPluginUpdates.Should().BeTrue();
        store.NotifyOnPluginUpdate.Should().BeFalse();
        store.PluginUpdateCheckFrequencyHours.Should().Be(48);
        store.LastPluginUpdateCheckTime.Should().NotBeNull();
        store.PendingPluginUpdates.Should().HaveCount(2);
    }

    [Fact]
    public void SettingsStoreBuilder_CreatesCorrectStore()
    {
        // Arrange & Act
        var store = Builder.SettingsStore()
            .WithExtensionsEnabled(true)
            .WithCheckPluginUpdatesOnStartup(false)
            .WithAutoDownloadPluginUpdates(true)
            .WithNotifyOnPluginUpdate(false)
            .WithPluginUpdateCheckFrequency(12)
            .WithInstalledExtensions("plugin1", "plugin2")
            .WithPendingUpdates("plugin3")
            .Build();

        // Assert
        store.ExtensionsEnabled.Should().BeTrue();
        store.CheckPluginUpdatesOnStartup.Should().BeFalse();
        store.AutoDownloadPluginUpdates.Should().BeTrue();
        store.NotifyOnPluginUpdate.Should().BeFalse();
        store.PluginUpdateCheckFrequencyHours.Should().Be(12);
        store.InstalledExtensions.Should().Contain("plugin1");
        store.InstalledExtensions.Should().Contain("plugin2");
        store.PendingPluginUpdates.Should().Contain("plugin3");
    }
}

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginManifestAdapterTests
{
    [Fact]
    public void PluginManifestAdapter_ShouldAdaptManifest()
    {
        // Arrange
        var manifest = Builder.PluginManifest()
            .WithId("test-plugin")
            .WithName("Test Plugin")
            .WithDescription("Test description")
            .WithVersion("1.0.0")
            .WithAuthor("Test Author")
            .Build();

        // Act
        var adapter = new PluginManifestAdapter(manifest);

        // Assert
        adapter.Id.Should().Be("test-plugin");
        adapter.Name.Should().Be("Test Plugin");
        adapter.Description.Should().Be("Test description");
        adapter.Icon.Should().BeEmpty();
        adapter.IsSystemPlugin.Should().BeFalse();
    }
}

[Trait("Category", TestCategories.Utils)]
[Trait("Category", TestCategories.Unit)]
public class TestDataGeneratorTests
{
    [Fact]
    public void GenerateUniqueString_ShouldReturnUniqueValues()
    {
        // Arrange & Act
        var value1 = TestDataGenerator.GenerateUniqueString("Test");
        var value2 = TestDataGenerator.GenerateUniqueString("Test");

        // Assert
        value1.Should().NotBe(value2);
        value1.Should().StartWith("Test_");
    }

    [Fact]
    public void GenerateVersion_ShouldReturnCorrectVersion()
    {
        // Arrange & Act
        var version = TestDataGenerator.GenerateVersion(2, 14, 0);

        // Assert
        version.Major.Should().Be(2);
        version.Minor.Should().Be(14);
        version.Build.Should().Be(0);
        version.Revision.Should().Be(0);
    }

    [Fact]
    public void CreateUniqueList_ShouldCreateCorrectCount()
    {
        // Arrange
        var count = 5;

        // Act
        var list = TestDataGenerator.CreateUniqueList(count, i => i);

        // Assert
        list.Should().HaveCount(count);
    }

    [Fact]
    public void GenerateRandomBytes_ShouldReturnCorrectLength()
    {
        // Arrange
        var length = 100;

        // Act
        var bytes = TestDataGenerator.GenerateRandomBytes(length);

        // Assert
        bytes.Should().HaveCount(length);
    }

    [Fact]
    public void GenerateRandomString_ShouldReturnCorrectLength()
    {
        // Arrange
        var length = 20;

        // Act
        var str = TestDataGenerator.GenerateRandomString(length);

        // Assert
        str.Should().HaveLength(length);
    }
}

[Trait("Category", TestCategories.Utils)]
[Trait("Category", TestCategories.Unit)]
public class AsyncTestHelpersTests
{
    [Fact]
    public async Task RetryAsync_ShouldSucceedOnFirstTry()
    {
        // Arrange
        var callCount = 0;

        // Act
        await AsyncTestHelpers.RetryAsync(async () =>
        {
            callCount++;
            await Task.CompletedTask;
        }, maxRetries: 3);

        // Assert
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task RetryAsync_ShouldRetryOnFailure()
    {
        // Arrange
        var callCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await AsyncTestHelpers.RetryAsync(async () =>
            {
                callCount++;
                throw new InvalidOperationException();
            }, maxRetries: 3, delayMs: 1);
        });

        callCount.Should().Be(3);
    }
}

[Trait("Category", TestCategories.Utils)]
[Trait("Category", TestCategories.Unit)]
public class TestAssertionsTests
{
    [Fact]
    public void ShouldBeSuccessful_ShouldNotThrowForSuccessfulAction()
    {
        // Arrange
        Action action = () => { };

        // Act & Assert
        action.ShouldBeSuccessful();
    }

    [Fact]
    public void ShouldFailWith_ShouldThrowForNonMatchingException()
    {
        // Arrange
        Action action = () => throw new InvalidOperationException();

        // Act & Assert
        action.ShouldFailWith<InvalidOperationException>();
    }

    [Fact]
    public void ShouldContain_ShouldPassWhenElementExists()
    {
        // Arrange
        var list = new List<string> { "a", "b", "c" };

        // Act & Assert
        list.ShouldContain("b");
    }

    [Fact]
    public void ShouldNotContain_ShouldPassWhenElementDoesNotExist()
    {
        // Arrange
        var list = new List<string> { "a", "b", "c" };

        // Act & Assert
        list.ShouldNotContain("d");
    }

    [Fact]
    public void ShouldHaveProperty_ShouldPassWhenPropertyExists()
    {
        // Arrange
        var obj = new TestClass { Id = 1, Name = "Test" };

        // Act & Assert
        obj.ShouldHaveProperty<TestClass>("Id");
        obj.ShouldHaveProperty<TestClass>("Name");
    }

    [Fact]
    public void ShouldHaveMethod_ShouldPassWhenMethodExists()
    {
        // Arrange
        var obj = new TestClass();

        // Act & Assert
        obj.ShouldHaveMethod<TestClass>("DoSomething");
    }

    private class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public void DoSomething() { }
    }
}

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class MockFactoryTests
{
    [Fact]
    public void CreateMockPlugin_ShouldReturnPluginWithDefaultValues()
    {
        // Arrange & Act
        var plugin = MockFactory.CreateMockPlugin();

        // Assert
        plugin.Id.Should().Be("TestPlugin");
        plugin.Name.Should().Be("Test Plugin");
        plugin.Description.Should().Be("Test description");
        plugin.Icon.Should().Be("Apps24");
        plugin.IsSystemPlugin.Should().BeFalse();
        plugin.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void CreateMockPlugin_WithCustomValues_ShouldReturnPluginWithCustomValues()
    {
        // Arrange & Act
        var plugin = MockFactory.CreateMockPlugin(
            id: "custom-plugin",
            name: "Custom Plugin",
            description: "Custom description",
            icon: "Settings24",
            isSystemPlugin: true,
            dependencies: new[] { "dep1" }
        );

        // Assert
        plugin.Id.Should().Be("custom-plugin");
        plugin.Name.Should().Be("Custom Plugin");
        plugin.Description.Should().Be("Custom description");
        plugin.Icon.Should().Be("Settings24");
        plugin.IsSystemPlugin.Should().BeTrue();
        plugin.Dependencies.Should().Contain("dep1");
    }

    [Fact]
    public void CreateMockPluginMetadata_ShouldReturnMetadataWithDefaultValues()
    {
        // Arrange & Act
        var metadata = MockFactory.CreateMockPluginMetadata();

        // Assert
        metadata.Id.Should().Be("TestPlugin");
        metadata.Version.Should().Be("1.0.0");
        metadata.MinimumHostVersion.Should().Be("1.0.0");
        metadata.Author.Should().Be("Test Author");
    }

    [Fact]
    public void CreateMockPluginManifest_ShouldReturnManifest()
    {
        // Arrange & Act
        var manifest = MockFactory.CreateMockPluginManifest(
            id: "test-manifest",
            version: "2.0.0",
            minimumHostVersion: "2.14.0",
            downloadUrl: "https://example.com/test.zip"
        );

        // Assert
        manifest.Id.Should().Be("test-manifest");
        manifest.Version.Should().Be("2.0.0");
        manifest.MinimumHostVersion.Should().Be("2.14.0");
        manifest.DownloadUrl.Should().Be("https://example.com/test.zip");
        manifest.IsSystemPlugin.Should().BeFalse();
    }
}

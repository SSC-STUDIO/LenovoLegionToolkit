using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LenovoLegionToolkit.Tests.Plugins;

[TestClass]
[TestCategory(TestCategories.Plugin)]
public class PluginConfigurationTests : TemporaryFileTestBase
{
    private const string TestPluginId = "test-plugin-config";
    private string _configDir = null!;
    private PluginConfiguration _config = null!;

    protected override void Setup()
    {
        _configDir = CreateTempDirectory();
        _config = new PluginConfiguration(TestPluginId);
    }

    protected override void Cleanup()
    {
        _config?.Clear();
        base.Cleanup();
    }

    [TestMethod]
    public void GetValue_WhenKeyDoesNotExist_ShouldReturnDefault()
    {
        var value = _config.GetValue("nonexistent", "default");

        value.Should().Be("default");
    }

    [TestMethod]
    public void GetValue_WhenKeyExists_ShouldReturnCorrectValue()
    {
        _config.SetValue("test-key", "test-value");

        var value = _config.GetValue("test-key", "default");

        value.Should().Be("test-value");
    }

    [TestMethod]
    public void GetValue_WithIntValue_ShouldReturnCorrectValue()
    {
        _config.SetValue("int-key", 42);

        var value = _config.GetValue<int>("int-key");

        value.Should().Be(42);
    }

    [TestMethod]
    public void GetValue_WithBoolValue_ShouldReturnCorrectValue()
    {
        _config.SetValue("bool-key", true);

        var value = _config.GetValue<bool>("bool-key");

        value.Should().BeTrue();
    }

    [TestMethod]
    public void SetValue_ShouldUpdateValue()
    {
        _config.SetValue("key", "value1");
        _config.SetValue("key", "value2");

        var value = _config.GetValue("key", "");

        value.Should().Be("value2");
    }

    [TestMethod]
    public void HasKey_WhenKeyExists_ShouldReturnTrue()
    {
        _config.SetValue("existing-key", "value");

        _config.HasKey("existing-key").Should().BeTrue();
    }

    [TestMethod]
    public void HasKey_WhenKeyDoesNotExist_ShouldReturnFalse()
    {
        _config.HasKey("nonexistent-key").Should().BeFalse();
    }

    [TestMethod]
    public void RemoveKey_ShouldRemoveKey()
    {
        _config.SetValue("key-to-remove", "value");
        _config.RemoveKey("key-to-remove");

        _config.HasKey("key-to-remove").Should().BeFalse();
    }

    [TestMethod]
    public void Clear_ShouldRemoveAllKeys()
    {
        _config.SetValue("key1", "value1");
        _config.SetValue("key2", "value2");
        _config.Clear();

        _config.HasKey("key1").Should().BeFalse();
        _config.HasKey("key2").Should().BeFalse();
    }

    [TestMethod]
    public async Task SaveAsync_ShouldPersistConfiguration()
    {
        _config.SetValue("persistent-key", "persistent-value");
        await _config.SaveAsync();

        var newConfig = new PluginConfiguration(TestPluginId);
        var value = newConfig.GetValue("persistent-key", "default");

        value.Should().Be("persistent-value");
    }

    [TestMethod]
    public async Task ReloadAsync_ShouldLoadLatestConfiguration()
    {
        _config.SetValue("key1", "value1");
        await _config.SaveAsync();

        var anotherConfig = new PluginConfiguration(TestPluginId);
        anotherConfig.SetValue("key2", "value2");
        await anotherConfig.SaveAsync();

        await _config.ReloadAsync();

        _config.HasKey("key2").Should().BeTrue();
    }
}

[TestClass]
[TestCategory(TestCategories.Plugin)]
public class PluginStateTests : UnitTestBase
{
    [TestMethod]
    public void PluginState_ShouldHaveCorrectValues()
    {
        ((int)PluginState.NotInstalled).Should().Be(0);
        ((int)PluginState.Installed).Should().Be(1);
        ((int)PluginState.Enabled).Should().Be(2);
        ((int)PluginState.Disabled).Should().Be(3);
        ((int)PluginState.Error).Should().Be(4);
    }

    [TestMethod]
    public void PluginStateChangedEventArgs_ShouldSetPropertiesCorrectly()
    {
        var args = new PluginStateChangedEventArgs(
            "test-plugin",
            PluginState.Installed,
            PluginState.Enabled,
            "Test error");

        args.PluginId.Should().Be("test-plugin");
        args.OldState.Should().Be(PluginState.Installed);
        args.NewState.Should().Be(PluginState.Enabled);
        args.ErrorMessage.Should().Be("Test error");
    }

    [TestMethod]
    public void PluginStateChangedEventArgs_WithoutError_ShouldHaveNullErrorMessage()
    {
        var args = new PluginStateChangedEventArgs(
            "test-plugin",
            PluginState.Disabled,
            PluginState.Enabled);

        args.ErrorMessage.Should().BeNull();
    }
}

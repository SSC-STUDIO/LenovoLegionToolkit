using System;
using System.Linq;
using LenovoLegionToolkit.Plugins.CustomMouse;
using Xunit;

namespace LenovoLegionToolkit.Plugins.CustomMouse.Tests
{
    public class CustomMousePluginTests
    {
        [Fact]
        public void Plugin_ImplementsIStatefulPlugin()
        {
            // Arrange
            var plugin = new CustomMousePlugin();

            // Act & Assert
            Assert.IsAssignableFrom<IStatefulPlugin>(plugin);
        }

        [Fact]
        public void Plugin_HasCorrectMetadata()
        {
            // Arrange
            var plugin = new CustomMousePlugin();

            // Act & Assert
            Assert.Equal("custom-mouse", plugin.Id);
            Assert.Equal("Custom Mouse", plugin.Name);
            Assert.Equal("1.0.0", plugin.Version);
            Assert.Equal("LenovoLegionToolkit Team", plugin.Author);
            Assert.NotNull(plugin.Description);
            Assert.NotNull(plugin.RepositoryUrl);
            Assert.False(plugin.IsSystemPlugin);
        }

        [Fact]
        public void Plugin_DefaultSettings_AreCorrect()
        {
            // Arrange
            var plugin = new CustomMousePlugin();

            // Act
            plugin.OnInstalled();

            // Assert
            Assert.Equal(1600, plugin.Settings.Dpi);
            Assert.Equal(1000, plugin.Settings.PollingRate);
            Assert.Empty(plugin.Settings.ButtonMappings);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(800)]
        [InlineData(1600)]
        [InlineData(3200)]
        [InlineData(16000)]
        public void SetDpi_ValidValues_ReturnsTrue(int dpi)
        {
            // Arrange
            var plugin = new CustomMousePlugin();
            plugin.OnInstalled();

            // Act
            var result = plugin.SetDpi(dpi);

            // Assert
            Assert.True(result);
            Assert.Equal(dpi, plugin.Settings.Dpi);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(99)]
        [InlineData(16001)]
        [InlineData(-100)]
        public void SetDpi_InvalidValues_ReturnsFalse(int dpi)
        {
            // Arrange
            var plugin = new CustomMousePlugin();
            plugin.OnInstalled();

            // Act
            var result = plugin.SetDpi(dpi);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(125)]
        [InlineData(250)]
        [InlineData(500)]
        [InlineData(1000)]
        public void SetPollingRate_ValidValues_ReturnsTrue(int rate)
        {
            // Arrange
            var plugin = new CustomMousePlugin();
            plugin.OnInstalled();

            // Act
            var result = plugin.SetPollingRate(rate);

            // Assert
            Assert.True(result);
            Assert.Equal(rate, plugin.Settings.PollingRate);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(126)]
        [InlineData(1001)]
        [InlineData(-1)]
        public void SetPollingRate_InvalidValues_ReturnsFalse(int rate)
        {
            // Arrange
            var plugin = new CustomMousePlugin();
            plugin.OnInstalled();

            // Act
            var result = plugin.SetPollingRate(rate);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void MapButton_AddsButtonMapping()
        {
            // Arrange
            var plugin = new CustomMousePlugin();
            plugin.OnInstalled();

            // Act
            plugin.MapButton(1, MouseButtonAction.LeftClick);
            plugin.MapButton(2, MouseButtonAction.RightClick);

            // Assert
            Assert.Equal(2, plugin.Settings.ButtonMappings.Count);
            Assert.Equal(MouseButtonAction.LeftClick, plugin.Settings.ButtonMappings[1]);
            Assert.Equal(MouseButtonAction.RightClick, plugin.Settings.ButtonMappings[2]);
        }

        [Fact]
        public void MapButton_UpdatesExistingMapping()
        {
            // Arrange
            var plugin = new CustomMousePlugin();
            plugin.OnInstalled();
            plugin.MapButton(1, MouseButtonAction.LeftClick);

            // Act
            plugin.MapButton(1, MouseButtonAction.DpiSwitch);

            // Assert
            Assert.Single(plugin.Settings.ButtonMappings);
            Assert.Equal(MouseButtonAction.DpiSwitch, plugin.Settings.ButtonMappings[1]);
        }

        [Fact]
        public void StateVersion_IsCorrect()
        {
            // Arrange
            var plugin = new CustomMousePlugin();

            // Act & Assert
            Assert.Equal(1, plugin.StateVersion);
        }

        [Fact]
        public void SerializeState_ReturnsNonEmptyData()
        {
            // Arrange
            var plugin = new CustomMousePlugin();
            plugin.OnInstalled();
            plugin.SetDpi(3200);
            plugin.SetPollingRate(500);
            plugin.MapButton(1, MouseButtonAction.LeftClick);

            // Act
            var stateData = plugin.SerializeState();

            // Assert
            Assert.NotNull(stateData);
            Assert.NotEmpty(stateData);
        }

        [Fact]
        public void DeserializeState_RestoresCorrectValues()
        {
            // Arrange
            var plugin = new CustomMousePlugin();
            plugin.OnInstalled();
            plugin.SetDpi(3200);
            plugin.SetPollingRate(500);
            plugin.MapButton(1, MouseButtonAction.LeftClick);

            var stateData = plugin.SerializeState();

            // Create new plugin instance and restore state
            var newPlugin = new CustomMousePlugin();
            newPlugin.OnInstalled();

            // Act
            var result = newPlugin.DeserializeState(stateData, "1.0.0");

            // Assert
            Assert.True(result);
            Assert.Equal(3200, newPlugin.Settings.Dpi);
            Assert.Equal(500, newPlugin.Settings.PollingRate);
            Assert.Single(newPlugin.Settings.ButtonMappings);
            Assert.Equal(MouseButtonAction.LeftClick, newPlugin.Settings.ButtonMappings[1]);
        }

        [Fact]
        public void DeserializeState_InvalidData_ReturnsFalse()
        {
            // Arrange
            var plugin = new CustomMousePlugin();
            plugin.OnInstalled();

            var invalidData = new byte[] { 0xFF, 0xFE, 0xFD };

            // Act
            var result = plugin.DeserializeState(invalidData, "1.0.0");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetManifest_ReturnsCorrectManifest()
        {
            // Arrange
            var plugin = new CustomMousePlugin();

            // Act
            var manifest = plugin.GetManifest();

            // Assert
            Assert.Equal("custom-mouse", manifest.Id);
            Assert.Equal("Custom Mouse", manifest.Name);
            Assert.Equal("1.0.0", manifest.Version);
            Assert.Equal("LenovoLegionToolkit Team", manifest.Author);
            Assert.NotNull(manifest.Description);
            Assert.NotNull(manifest.RepositoryUrl);
            Assert.False(manifest.IsSystemPlugin);
            Assert.Equal("3.6.0", manifest.MinLLTVersion);
        }

        [Fact]
        public void Plugin_Lifecycle_InstallAndUninstall()
        {
            // Arrange
            var plugin = new CustomMousePlugin();

            // Act - Install
            plugin.OnInstalled();
            Assert.NotNull(plugin.Settings);

            // Act - Uninstall
            plugin.OnUninstalled();

            // Assert - No exception thrown
            Assert.True(true);
        }

        [Fact]
        public void Plugin_Lifecycle_ShutdownAndStop()
        {
            // Arrange
            var plugin = new CustomMousePlugin();
            plugin.OnInstalled();

            // Act & Assert - No exceptions
            plugin.OnShutdown();
            plugin.Stop();

            Assert.True(true);
        }
    }
}

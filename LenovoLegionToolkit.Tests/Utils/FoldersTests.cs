using System;
using System.IO;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace LenovoLegionToolkit.Tests.Utils;

[Trait("Category", TestCategories.Unit)]
public class FoldersTests
{
    #region Program Property Tests

    [Fact]
    public void Program_ShouldReturnNonEmptyPath()
    {
        // Act
        var path = Folders.Program;

        // Assert
        path.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Program_ShouldReturnRootedPath()
    {
        // Act
        var path = Folders.Program;

        // Assert
        Path.IsPathRooted(path).Should().BeTrue();
    }

    [Fact]
    public void Program_ShouldReturnValidDirectory()
    {
        // Act
        var path = Folders.Program;

        // Assert
        Directory.Exists(path).Should().BeTrue();
    }

    #endregion

    #region AppData Property Tests

    [Fact]
    public void AppData_ShouldReturnNonEmptyPath()
    {
        // Act
        var path = Folders.AppData;

        // Assert
        path.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AppData_ShouldReturnRootedPath()
    {
        // Act
        var path = Folders.AppData;

        // Assert
        Path.IsPathRooted(path).Should().BeTrue();
    }

    [Fact]
    public void AppData_ShouldContainLenovoLegionToolkit()
    {
        // Act
        var path = Folders.AppData;

        // Assert
        path.Should().Contain("LenovoLegionToolkit");
    }

    [Fact]
    public void AppData_ShouldBeUnderLocalApplicationData()
    {
        // Arrange
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Act
        var path = Folders.AppData;

        // Assert
        path.Should().StartWith(localAppData);
    }

    [Fact]
    public void AppData_ShouldCreateDirectoryIfNotExists()
    {
        // Arrange - AppData folder is created on first access
        // We test that calling it multiple times works
        var path1 = Folders.AppData;

        // Act
        var path2 = Folders.AppData;

        // Assert
        path1.Should().Be(path2);
        Directory.Exists(path1).Should().BeTrue();
    }

    [Fact]
    public void AppData_WhenCalledMultipleTimes_ShouldReturnSamePath()
    {
        // Act
        var path1 = Folders.AppData;
        var path2 = Folders.AppData;
        var path3 = Folders.AppData;

        // Assert
        path1.Should().Be(path2);
        path2.Should().Be(path3);
    }

    [Fact]
    public void AppData_ShouldHonorEnvironmentOverride()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        var overrideDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, overrideDirectory);

        try
        {
            // Act
            var appData = Folders.AppData;

            // Assert
            appData.Should().Be(Path.GetFullPath(overrideDirectory));
            Directory.Exists(appData).Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, originalValue);
            if (Directory.Exists(overrideDirectory))
                Directory.Delete(overrideDirectory, recursive: true);
        }
    }

    #endregion

    #region Temp Property Tests

    [Fact]
    public void Temp_ShouldReturnNonEmptyPath()
    {
        // Act
        var path = Folders.Temp;

        // Assert
        path.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Temp_ShouldReturnRootedPath()
    {
        // Act
        var path = Folders.Temp;

        // Assert
        Path.IsPathRooted(path).Should().BeTrue();
    }

    [Fact]
    public void Temp_ShouldContainLenovoLegionToolkit()
    {
        // Act
        var path = Folders.Temp;

        // Assert
        path.Should().Contain("LenovoLegionToolkit");
    }

    [Fact]
    public void Temp_ShouldBeUnderSystemTempPath()
    {
        // Arrange
        var tempPath = Path.GetTempPath();

        // Act
        var path = Folders.Temp;

        // Assert
        path.Should().StartWith(tempPath);
    }

    [Fact]
    public void Temp_ShouldCreateDirectoryIfNotExists()
    {
        // Arrange - Temp folder is created on first access
        var path1 = Folders.Temp;

        // Act
        var path2 = Folders.Temp;

        // Assert
        path1.Should().Be(path2);
        Directory.Exists(path1).Should().BeTrue();
    }

    [Fact]
    public void Temp_WhenCalledMultipleTimes_ShouldReturnSamePath()
    {
        // Act
        var path1 = Folders.Temp;
        var path2 = Folders.Temp;
        var path3 = Folders.Temp;

        // Assert
        path1.Should().Be(path2);
        path2.Should().Be(path3);
    }

    #endregion

    #region Path Uniqueness Tests

    [Fact]
    public void AppData_AndTemp_ShouldBeDifferent()
    {
        // Act
        var appData = Folders.AppData;
        var temp = Folders.Temp;

        // Assert
        appData.Should().NotBe(temp);
    }

    [Fact]
    public void AppData_AndProgram_ShouldBeDifferent()
    {
        // Act
        var appData = Folders.AppData;
        var program = Folders.Program;

        // Assert
        appData.Should().NotBe(program);
    }

    #endregion

    #region Directory Creation Tests

    [Fact]
    public void AppData_ShouldBeWritable()
    {
        // Arrange
        var appData = Folders.AppData;
        var testFileName = Path.Combine(appData, $"test_{Guid.NewGuid()}.txt");

        // Act
        try
        {
            File.WriteAllText(testFileName, "test");
            var exists = File.Exists(testFileName);

            // Assert
            exists.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(testFileName))
                File.Delete(testFileName);
        }
    }

    [Fact]
    public void Temp_ShouldBeWritable()
    {
        // Arrange
        var temp = Folders.Temp;
        var testFileName = Path.Combine(temp, $"test_{Guid.NewGuid()}.txt");

        // Act
        try
        {
            File.WriteAllText(testFileName, "test");
            var exists = File.Exists(testFileName);

            // Assert
            exists.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(testFileName))
                File.Delete(testFileName);
        }
    }

    #endregion
}

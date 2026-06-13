using FluentAssertions;
using LenovoLegionToolkit.Lib.PackageDownloader;
using Xunit;

namespace UniversalDeviceToolkit.Tests.PackageDownloader;

[Trait("Category", TestCategories.Unit)]
public sealed class AbstractPackageDownloaderTests
{
    [Fact]
    public void TryExtractExpectedSha256_WithNamedSha256Line_ShouldUseMatchingPackageHash()
    {
        // Arrange
        var otherHash = new string('b', 64);
        var packageHash = new string('a', 64);
        var sidecar = $"""
                      {otherHash} unrelated-driver.exe
                      SHA256 (driver.exe) = {packageHash}
                      """;

        // Act
        var result = AbstractPackageDownloader.TryExtractExpectedSha256(sidecar, ["driver.exe"]);

        // Assert
        result.Should().Be(packageHash);
    }

    [Fact]
    public void TryExtractExpectedSha256_WithSingleRawHash_ShouldAcceptHash()
    {
        // Arrange
        var packageHash = new string('c', 64);

        // Act
        var result = AbstractPackageDownloader.TryExtractExpectedSha256(packageHash, ["driver.exe"]);

        // Assert
        result.Should().Be(packageHash);
    }
}

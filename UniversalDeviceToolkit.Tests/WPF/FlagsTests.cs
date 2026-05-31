using FluentAssertions;
using UniversalDeviceToolkit.WPF;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

public sealed class FlagsTests
{
    [Fact]
    public void Constructor_ParsesPublicBooleanFlags()
    {
        var flags = new Flags(
        [
            "--trace",
            "--disable-update-checker"
        ]);

        flags.IsTraceEnabled.Should().BeTrue();
        flags.DisableUpdateChecker.Should().BeTrue();
    }

    [Fact]
    public void StringValue_WithEqualsSeparatedProxyArgument_ReturnsValue()
    {
        var result = Flags.StringValue(["--proxy-username=codex"], "--proxy-username");

        result.Should().Be("codex");
    }

    [Fact]
    public void StringValue_WithSpaceSeparatedProxyArgument_ReturnsValue()
    {
        var result = Flags.StringValue(["--proxy-username", "codex"], "--proxy-username");

        result.Should().Be("codex");
    }

    [Fact]
    public void StringValue_WithMissingValue_ReturnsNull()
    {
        var result = Flags.StringValue(["--proxy-username", "--disable-update-checker"], "--proxy-username");

        result.Should().BeNull();
    }
}

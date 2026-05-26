using FluentAssertions;
using UniversalDeviceToolkit.WPF;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

public sealed class FlagsTests
{
    [Fact]
    public void StringValue_WithEqualsSeparatedArgument_ReturnsValue()
    {
        var result = Flags.StringValue(["--single-instance-key=codex"], Flags.SingleInstanceKeySwitch);

        result.Should().Be("codex");
    }

    [Fact]
    public void StringValue_WithSpaceSeparatedArgument_ReturnsValue()
    {
        var result = Flags.StringValue(["--single-instance-key", "codex"], Flags.SingleInstanceKeySwitch);

        result.Should().Be("codex");
    }

    [Fact]
    public void StringValue_WithMissingValue_ReturnsNull()
    {
        var result = Flags.StringValue(["--single-instance-key", "--disable-update-checker"], Flags.SingleInstanceKeySwitch);

        result.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithSpaceSeparatedSingleInstanceArguments_DoesNotThrow()
    {
        var flags = new Flags(
        [
            "--single-instance-key",
            "codex",
            "--ipc-pipe-name",
            "codex-pipe"
        ]);

        flags.SingleInstanceKey.Should().Be("codex");
        flags.IpcPipeName.Should().Be("codex-pipe");
    }
}

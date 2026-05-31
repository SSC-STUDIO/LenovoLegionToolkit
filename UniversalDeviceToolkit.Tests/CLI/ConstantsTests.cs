using FluentAssertions;
using UniversalDeviceToolkit.CLI.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests.CLI;

[Trait("Category", TestCategories.Unit)]
public class ConstantsTests
{
    [Fact]
    public void DefaultPipeName_IsStable()
    {
        Constants.DEFAULT_PIPE_NAME.Should().Be("LenovoLegionToolkit-IPC-0");
    }
}

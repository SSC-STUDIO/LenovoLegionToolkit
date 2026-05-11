using FluentAssertions;
using LenovoLegionToolkit.CLI.Lib;
using Xunit;

namespace LenovoLegionToolkit.Tests.CLI;

[Trait("Category", TestCategories.Unit)]
public class ConstantsTests
{
    [Fact]
    public void DefaultPipeName_IsStable()
    {
        Constants.DEFAULT_PIPE_NAME.Should().Be("LenovoLegionToolkit-IPC-0");
    }

    [Fact]
    public void PipeNameEnvironmentVariable_IsDocumentedName()
    {
        Constants.PIPE_NAME_ENVIRONMENT_VARIABLE.Should().Be("LLT_IPC_PIPE_NAME");
    }
}

using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Shared.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class AboutPageViewModelTests
{
    [Fact]
    public void ApplicationFolderCommands_ResolveTheSharedHostDirectories()
    {
        AboutPageViewModel.GetAppDataPath().Should().Be(Folders.AppData);
        AboutPageViewModel.GetTempPath().Should().Be(Folders.Temp);
    }
}

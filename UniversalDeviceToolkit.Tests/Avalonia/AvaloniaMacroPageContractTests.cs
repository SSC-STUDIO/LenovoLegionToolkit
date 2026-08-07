using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class AvaloniaMacroPageContractTests
{
    [Fact]
    public void MacroPage_ShouldMatchWpfSequenceAvailabilityMovementRecordingAndLiveProgress()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Pages", "MacroPage.cs"));

        Assert.Contains("IsEnabled = slot.EventCount > 0 && !isRecording", source, StringComparison.Ordinal);
        Assert.Contains("MacroRecordingMode.KeyboardMouseMovement", source, StringComparison.Ordinal);
        Assert.Contains("Task.Delay(TimeSpan.FromSeconds(3))", source, StringComparison.Ordinal);
        Assert.Contains("MacroRecordingWindow_Preparing_Title", source, StringComparison.Ordinal);
        Assert.Contains("DispatcherTimer", source, StringComparison.Ordinal);
        Assert.Contains("Interval = TimeSpan.FromMilliseconds(250)", source, StringComparison.Ordinal);
        Assert.Contains("state.IsRecording && _isLoaded", source, StringComparison.Ordinal);
        Assert.Contains("OnUnloaded", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}

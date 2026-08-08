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

    [Fact]
    public void MacroPage_ShouldExposePerEventEditingAffordances()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Pages", "MacroPage.cs"));

        Assert.Contains("MacroPage_AddEvent", source, StringComparison.Ordinal);
        Assert.Contains("MacroPage_AddKeyEvent", source, StringComparison.Ordinal);
        Assert.Contains("MacroPage_AddMouseEvent", source, StringComparison.Ordinal);
        Assert.Contains("MacroPage_AddDelayEvent", source, StringComparison.Ordinal);
        Assert.Contains("MacroPage_Capture", source, StringComparison.Ordinal);
        Assert.Contains("MacroPage_RemoveEvent", source, StringComparison.Ordinal);
        Assert.Contains("MacroPage_MoveUp", source, StringComparison.Ordinal);
        Assert.Contains("MacroPage_MoveDown", source, StringComparison.Ordinal);
        Assert.Contains("AddEventButton", source, StringComparison.Ordinal);
        Assert.Contains("CaptureButton", source, StringComparison.Ordinal);
        Assert.Contains("MoveUpButton", source, StringComparison.Ordinal);
        Assert.Contains("MoveDownButton", source, StringComparison.Ordinal);
        Assert.Contains("RemoveButton", source, StringComparison.Ordinal);
        Assert.Contains("DelayEditor", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MacroPage_ShouldPersistEditedSequencesThroughTheSharedHostSavePath()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Pages", "MacroPage.cs"));

        Assert.Contains("SaveMacroSequenceAsync", source, StringComparison.Ordinal);
        Assert.Contains("ClearMacroSequenceAsync", source, StringComparison.Ordinal);
        Assert.Contains("StartMacroRecordingAsync", source, StringComparison.Ordinal);
        Assert.Contains("SaveEditedSequenceAsync", source, StringComparison.Ordinal);
        Assert.Contains("HostOperation.TryExecuteAsync", source, StringComparison.Ordinal);
        Assert.Contains("MacroPage_ActionError", source, StringComparison.Ordinal);
        Assert.Contains("MacroPage_ClearError", source, StringComparison.Ordinal);
        Assert.Contains("MacroPage_OptionsError", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MacroPage_ShouldHostTheEventEditingModelAndKeyCaptureWindow()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Pages", "MacroPage.cs"));
        var capture = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Pages", "Windows", "MacroKeyCaptureWindow.cs"));

        Assert.Contains("MacroEventEditing", page, StringComparison.Ordinal);
        Assert.Contains("MacroKeyCaptureWindow.CaptureAsync", page, StringComparison.Ordinal);
        Assert.Contains("CreateEventRow", page, StringComparison.Ordinal);
        Assert.Contains("CreateAddEventButton", page, StringComparison.Ordinal);
        Assert.Contains("MoveEventUp", page, StringComparison.Ordinal);
        Assert.Contains("MoveEventDown", page, StringComparison.Ordinal);
        Assert.Contains("RemoveEventAt", page, StringComparison.Ordinal);

        Assert.Contains("CaptureAsync", capture, StringComparison.Ordinal);
        Assert.Contains("CountdownSeconds", capture, StringComparison.Ordinal);
        Assert.Contains("TryGetKeyCode", capture, StringComparison.Ordinal);
        Assert.Contains("AvaloniaMacroKeyCaptureCancelButton", capture, StringComparison.Ordinal);
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

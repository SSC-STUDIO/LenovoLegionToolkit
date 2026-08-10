using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UniversalDeviceToolkit.Abstractions.Macro;

namespace UniversalDeviceToolkit.ViewModels;

/// <summary>
/// Host-neutral macro workspace operations. The Windows page adapts its platform
/// services to this interface and shares the same editor state.
/// </summary>
public interface IMacroWorkspace
{
    Task<MacroWorkspaceSnapshot> GetStateAsync(CancellationToken cancellationToken = default);
    Task<bool> SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
    Task<bool> StartRecordingAsync(ulong key, MacroRecordingMode mode, CancellationToken cancellationToken = default);
    Task<bool> StopRecordingAsync(CancellationToken cancellationToken = default);
    Task<bool> PlayAsync(ulong key, CancellationToken cancellationToken = default);
    Task<bool> SetSequenceOptionsAsync(
        ulong key,
        int repeatCount,
        bool ignoreDelays,
        bool interruptOnOtherKey,
        CancellationToken cancellationToken = default);
    Task<bool> SaveSequenceAsync(
        ulong key,
        IReadOnlyList<MacroEventSnapshot> events,
        int repeatCount,
        bool ignoreDelays,
        bool interruptOnOtherKey,
        CancellationToken cancellationToken = default);
    Task<bool> ClearSequenceAsync(ulong key, CancellationToken cancellationToken = default);
}

public enum MacroRecordingMode
{
    Keyboard,
    KeyboardMouse,
    KeyboardMouseMovement,
}

public sealed record MacroEventSnapshot(
    string Source,
    string Direction,
    uint Key,
    int X,
    int Y,
    TimeSpan Delay);

public sealed record MacroSlotSnapshot(
    ulong Key,
    int RepeatCount,
    bool IgnoreDelays,
    bool InterruptOnOtherKey,
    IReadOnlyList<MacroEventSnapshot> Events);

public sealed record MacroWorkspaceSnapshot(
    bool IsEnabled,
    bool IsRecording,
    IReadOnlyList<MacroSlotSnapshot> Slots);

public partial class MacroSlotViewModel : ObservableObject
{
    public MacroSlotViewModel(MacroSlotSnapshot snapshot)
    {
        Key = snapshot.Key;
        RepeatCount = Math.Clamp(snapshot.RepeatCount, 1, 10);
        IgnoreDelays = snapshot.IgnoreDelays;
        InterruptOnOtherKey = snapshot.InterruptOnOtherKey;
        Events = new ObservableCollection<MacroEventSnapshot>(snapshot.Events ?? []);
    }

    public ulong Key { get; }

    public ObservableCollection<MacroEventSnapshot> Events { get; }

    public int EventCount => Events.Count;

    [ObservableProperty]
    private int _repeatCount;

    [ObservableProperty]
    private bool _ignoreDelays;

    [ObservableProperty]
    private bool _interruptOnOtherKey;

    public MacroSlotSnapshot ToSnapshot() => new(
        Key,
        Math.Clamp(RepeatCount, 1, 10),
        IgnoreDelays,
        InterruptOnOtherKey,
        Events.ToArray());

    public bool AddEvent(MacroEventSnapshot macroEvent)
    {
        if (macroEvent is null)
            return false;

        Events.Add(macroEvent);
        OnPropertyChanged(nameof(EventCount));
        return true;
    }

    public bool RemoveEventAt(int index)
    {
        if (index < 0 || index >= Events.Count)
            return false;

        Events.RemoveAt(index);
        OnPropertyChanged(nameof(EventCount));
        return true;
    }

    public bool MoveEvent(int index, int delta)
    {
        var target = index + delta;
        if (index < 0 || target < 0 || index >= Events.Count || target >= Events.Count)
            return false;

        (Events[index], Events[target]) = (Events[target], Events[index]);
        return true;
    }
}

public partial class MacroViewModel : ObservableObject
{
    private readonly IMacroController _controller;
    private readonly IMacroWorkspace? _workspace;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private ulong _selectedKey;

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public MacroViewModel(IMacroController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public MacroViewModel(IMacroController controller, IMacroWorkspace workspace)
        : this(controller)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    public IMacroController Controller => _controller;

    public ObservableCollection<MacroSlotViewModel> Slots { get; } = [];

    public void LoadState()
    {
        IsEnabled = _controller.IsEnabled;
    }

    public void SetEnabled(bool enabled)
    {
        _controller.SetEnabled(enabled);
        IsEnabled = enabled;
    }

    public void SelectKey(ulong key)
    {
        SelectedKey = key;
    }

    public async Task<MacroWorkspaceSnapshot?> LoadWorkspaceAsync(
        CancellationToken cancellationToken = default)
    {
        if (_workspace is null)
        {
            LoadState();
            return null;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var snapshot = await _workspace.GetStateAsync(cancellationToken).ConfigureAwait(false);
            IsEnabled = snapshot.IsEnabled;
            IsRecording = snapshot.IsRecording;
            ReplaceSlots(snapshot.Slots);
            return snapshot;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> SetEnabledAsync(
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        if (_workspace is null)
        {
            SetEnabled(enabled);
            return true;
        }

        var previous = IsEnabled;
        IsEnabled = enabled;
        if (await ExecuteAsync(
                token => _workspace.SetEnabledAsync(enabled, token),
                cancellationToken).ConfigureAwait(false))
            return true;

        IsEnabled = previous;
        return false;
    }

    public async Task<bool> StartRecordingAsync(
        ulong key,
        MacroRecordingMode mode,
        CancellationToken cancellationToken = default)
    {
        if (_workspace is null)
            return false;

        SelectKey(key);
        var accepted = await ExecuteAsync(
            token => _workspace.StartRecordingAsync(key, mode, token),
            cancellationToken).ConfigureAwait(false);
        if (accepted)
            await LoadWorkspaceAsync(cancellationToken).ConfigureAwait(false);
        return accepted;
    }

    public Task<bool> StopRecordingAsync(CancellationToken cancellationToken = default) =>
        ExecuteAndReloadAsync(
            token => _workspace?.StopRecordingAsync(token) ?? Task.FromResult(false),
            cancellationToken);

    public async Task<bool> PlayAsync(
        ulong key,
        CancellationToken cancellationToken = default)
    {
        if (_workspace is null)
            return _controller is { } && _controller.IsEnabled;

        IsPlaying = true;
        try
        {
            return await ExecuteAsync(
                token => _workspace.PlayAsync(key, token),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            IsPlaying = false;
        }
    }

    public Task<bool> SetSequenceOptionsAsync(
        ulong key,
        int repeatCount,
        bool ignoreDelays,
        bool interruptOnOtherKey,
        CancellationToken cancellationToken = default) =>
        SaveOperationAsync(
            token => _workspace?.SetSequenceOptionsAsync(
                key,
                repeatCount,
                ignoreDelays,
                interruptOnOtherKey,
                token) ?? Task.FromResult(false),
            cancellationToken);

    public Task<bool> SaveSequenceAsync(
        MacroSlotViewModel slot,
        CancellationToken cancellationToken = default)
    {
        if (slot is null)
            return Task.FromResult(false);

        return SaveSequenceAsync(slot.ToSnapshot(), cancellationToken);
    }

    public Task<bool> SaveSequenceAsync(
        MacroSlotSnapshot slot,
        CancellationToken cancellationToken = default) =>
        SaveOperationAsync(
            token => _workspace?.SaveSequenceAsync(
                slot.Key,
                slot.Events,
                slot.RepeatCount,
                slot.IgnoreDelays,
                slot.InterruptOnOtherKey,
                token) ?? Task.FromResult(false),
            cancellationToken);

    public Task<bool> ClearSequenceAsync(
        ulong key,
        CancellationToken cancellationToken = default) =>
        SaveOperationAsync(
            token => _workspace?.ClearSequenceAsync(key, token) ?? Task.FromResult(false),
            cancellationToken);

    public MacroSlotViewModel? FindSlot(ulong key) =>
        Slots.FirstOrDefault(slot => slot.Key == key);

    private async Task<bool> SaveOperationAsync(
        Func<CancellationToken, Task<bool>> operation,
        CancellationToken cancellationToken)
    {
        var accepted = await ExecuteAsync(operation, cancellationToken).ConfigureAwait(false);
        if (accepted)
            await LoadWorkspaceAsync(cancellationToken).ConfigureAwait(false);
        return accepted;
    }

    private async Task<bool> ExecuteAndReloadAsync(
        Func<CancellationToken, Task<bool>> operation,
        CancellationToken cancellationToken)
    {
        var accepted = await ExecuteAsync(operation, cancellationToken).ConfigureAwait(false);
        if (accepted)
            await LoadWorkspaceAsync(cancellationToken).ConfigureAwait(false);
        return accepted;
    }

    private async Task<bool> ExecuteAsync(
        Func<CancellationToken, Task<bool>> operation,
        CancellationToken cancellationToken)
    {
        if (_workspace is null)
            return false;

        ErrorMessage = null;
        try
        {
            if (await operation(cancellationToken).ConfigureAwait(false))
                return true;

            ErrorMessage = "The macro operation was rejected by the host.";
            return false;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    private void ReplaceSlots(IEnumerable<MacroSlotSnapshot> snapshots)
    {
        Slots.Clear();
        foreach (var snapshot in snapshots ?? [])
            Slots.Add(new MacroSlotViewModel(snapshot));
    }
}

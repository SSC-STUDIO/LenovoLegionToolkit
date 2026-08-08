using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UniversalDeviceToolkit.ViewModels;

/// <summary>
/// Shared automation workspace persistence contract. UI hosts own only the
/// controls used to edit a draft; loading, ordering, rollback and execution stay here.
/// </summary>
public interface IAutomationWorkspace
{
    Task<AutomationWorkspaceSnapshot> GetStateAsync(CancellationToken cancellationToken = default);
    Task<bool> SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
    Task<bool> SaveAsync(
        IReadOnlyList<AutomationPipelineDraftSnapshot> pipelines,
        CancellationToken cancellationToken = default);
    Task<bool> RunAsync(Guid pipelineId, CancellationToken cancellationToken = default);
}

public sealed record AutomationStepOptionSnapshot(
    string TypeKey,
    string DisplayName,
    string DefaultConfigurationJson);

public sealed record AutomationTriggerOptionSnapshot(
    string Key,
    string DisplayName,
    string? DefaultConfigurationJson = null);

public sealed record AutomationStepSnapshot(
    string TypeKey,
    string DisplayName,
    string ConfigurationJson);

public sealed record AutomationPipelineSnapshot(
    Guid Id,
    string? Name,
    string? IconName,
    string Trigger,
    bool IsAutomatic,
    bool IsExclusive,
    string? TriggerKey,
    string? TriggerConfigurationJson,
    IReadOnlyList<AutomationStepSnapshot> Steps);

public sealed record AutomationWorkspaceSnapshot(
    bool IsEnabled,
    IReadOnlyList<AutomationPipelineSnapshot> Pipelines,
    IReadOnlyList<AutomationTriggerOptionSnapshot> TriggerOptions,
    IReadOnlyList<AutomationStepOptionSnapshot> StepOptions);

public sealed record AutomationPipelineDraftSnapshot(
    Guid? Id,
    string? Name,
    string? IconName,
    bool IsAutomatic,
    bool IsExclusive,
    string? TriggerKey,
    string? TriggerConfigurationJson,
    IReadOnlyList<AutomationStepSnapshot> Steps);

public sealed class AutomationStepViewModel : ObservableObject
{
    public AutomationStepViewModel(AutomationStepSnapshot snapshot)
    {
        TypeKey = snapshot.TypeKey;
        DisplayName = snapshot.DisplayName;
        ConfigurationJson = snapshot.ConfigurationJson;
    }

    public string TypeKey { get; }
    public string DisplayName { get; }

    private string _configurationJson = string.Empty;

    public string ConfigurationJson
    {
        get => _configurationJson;
        set => SetProperty(ref _configurationJson, value);
    }

    public AutomationStepSnapshot ToSnapshot() =>
        new(TypeKey, DisplayName, ConfigurationJson);
}

public sealed class AutomationPipelineViewModel : ObservableObject
{
    public AutomationPipelineViewModel(AutomationPipelineSnapshot snapshot)
    {
        Id = snapshot.Id;
        IsAutomatic = snapshot.IsAutomatic;
        Name = snapshot.Name;
        IconName = snapshot.IconName;
        Trigger = snapshot.Trigger;
        TriggerKey = snapshot.TriggerKey;
        TriggerConfigurationJson = snapshot.TriggerConfigurationJson;
        IsExclusive = snapshot.IsExclusive;
        Steps = new ObservableCollection<AutomationStepViewModel>(
            (snapshot.Steps ?? []).Select(step => new AutomationStepViewModel(step)));
    }

    public Guid Id { get; }
    public bool IsAutomatic { get; }
    public ObservableCollection<AutomationStepViewModel> Steps { get; }

    private string? _name;
    private string? _iconName;
    private string _trigger = string.Empty;
    private string? _triggerKey;
    private string? _triggerConfigurationJson;
    private bool _isExclusive;

    public string? Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string? IconName
    {
        get => _iconName;
        set => SetProperty(ref _iconName, value);
    }

    public string Trigger
    {
        get => _trigger;
        set => SetProperty(ref _trigger, value);
    }

    public string? TriggerKey
    {
        get => _triggerKey;
        set => SetProperty(ref _triggerKey, value);
    }

    public string? TriggerConfigurationJson
    {
        get => _triggerConfigurationJson;
        set => SetProperty(ref _triggerConfigurationJson, value);
    }

    public bool IsExclusive
    {
        get => _isExclusive;
        set => SetProperty(ref _isExclusive, value);
    }

    public AutomationPipelineDraftSnapshot ToDraft(bool isNew = false) => new(
        isNew || Id == Guid.Empty ? null : Id,
        Name,
        string.IsNullOrWhiteSpace(IconName) ? null : IconName.Trim(),
        IsAutomatic,
        IsExclusive,
        TriggerKey,
        TriggerConfigurationJson,
        Steps.Select(step => step.ToSnapshot()).ToArray());

    public bool MoveStep(int index, int delta)
    {
        var target = index + delta;
        if (index < 0 || target < 0 || index >= Steps.Count || target >= Steps.Count)
            return false;

        (Steps[index], Steps[target]) = (Steps[target], Steps[index]);
        return true;
    }
}

public partial class AutomationWorkspaceViewModel : ObservableObject
{
    private readonly IAutomationWorkspace _workspace;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private Guid? _executingPipelineId;

    [ObservableProperty]
    private string? _errorMessage;

    public AutomationWorkspaceViewModel(IAutomationWorkspace workspace)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    public ObservableCollection<AutomationPipelineViewModel> Pipelines { get; } = [];
    public ObservableCollection<AutomationTriggerOptionSnapshot> TriggerOptions { get; } = [];
    public ObservableCollection<AutomationStepOptionSnapshot> StepOptions { get; } = [];

    public async Task<AutomationWorkspaceSnapshot?> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var snapshot = await _workspace.GetStateAsync(cancellationToken).ConfigureAwait(false);
            IsEnabled = snapshot.IsEnabled;
            ReplaceCollection(Pipelines, snapshot.Pipelines, pipeline => new AutomationPipelineViewModel(pipeline));
            ReplaceCollection(TriggerOptions, snapshot.TriggerOptions, option => option);
            ReplaceCollection(StepOptions, snapshot.StepOptions, option => option);
            IsDirty = false;
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
        var previous = IsEnabled;
        IsEnabled = enabled;
        if (await ExecuteAsync(
                token => _workspace.SetEnabledAsync(enabled, token),
                cancellationToken).ConfigureAwait(false))
            return true;

        IsEnabled = previous;
        return false;
    }

    public async Task<bool> SaveAsync(
        IReadOnlyList<AutomationPipelineDraftSnapshot> drafts,
        CancellationToken cancellationToken = default)
    {
        if (drafts is null)
            return false;

        var accepted = await ExecuteAsync(
            token => _workspace.SaveAsync(drafts, token),
            cancellationToken).ConfigureAwait(false);
        if (!accepted)
            return false;

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> RunAsync(
        Guid pipelineId,
        CancellationToken cancellationToken = default)
    {
        if (pipelineId == Guid.Empty)
            return false;

        IsExecuting = true;
        ExecutingPipelineId = pipelineId;
        try
        {
            return await ExecuteAsync(
                token => _workspace.RunAsync(pipelineId, token),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            IsExecuting = false;
            ExecutingPipelineId = null;
        }
    }

    public AutomationPipelineViewModel AddManualPipeline(string? name, string? iconName = null)
    {
        var pipeline = new AutomationPipelineViewModel(new AutomationPipelineSnapshot(
            Guid.Empty,
            name,
            iconName,
            string.Empty,
            false,
            true,
            null,
            null,
            []));
        Pipelines.Insert(0, pipeline);
        MarkDirty();
        return pipeline;
    }

    public AutomationPipelineViewModel AddAutomaticPipeline(
        AutomationTriggerOptionSnapshot option,
        string? name = null)
    {
        if (option is null)
            throw new ArgumentNullException(nameof(option));

        var pipeline = new AutomationPipelineViewModel(new AutomationPipelineSnapshot(
            Guid.Empty,
            name,
            null,
            option.DisplayName,
            true,
            true,
            option.Key,
            option.DefaultConfigurationJson,
            []));
        Pipelines.Insert(0, pipeline);
        MarkDirty();
        return pipeline;
    }

    public bool RemovePipeline(AutomationPipelineViewModel pipeline)
    {
        if (pipeline is null || !Pipelines.Remove(pipeline))
            return false;

        MarkDirty();
        return true;
    }

    public bool MovePipeline(AutomationPipelineViewModel pipeline, int delta)
    {
        if (pipeline is null)
            return false;

        var matching = Pipelines.Where(candidate => candidate.IsAutomatic == pipeline.IsAutomatic).ToList();
        var index = matching.IndexOf(pipeline);
        var target = index + delta;
        if (index < 0 || target < 0 || target >= matching.Count)
            return false;

        var adjacent = matching[target];
        var sourceIndex = Pipelines.IndexOf(pipeline);
        var targetIndex = Pipelines.IndexOf(adjacent);
        (Pipelines[sourceIndex], Pipelines[targetIndex]) = (Pipelines[targetIndex], Pipelines[sourceIndex]);
        MarkDirty();
        return true;
    }

    public void MarkDirty() => IsDirty = true;

    public static IReadOnlyList<AutomationStepOptionSnapshot> GetAvailableStepOptions(
        IEnumerable<AutomationStepOptionSnapshot> options,
        bool isAutomatic) =>
        isAutomatic
            ? options.ToArray()
            : options.Where(option => !string.Equals(option.TypeKey, "QuickAction", StringComparison.Ordinal)).ToArray();

    private async Task<bool> ExecuteAsync(
        Func<CancellationToken, Task<bool>> operation,
        CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        try
        {
            if (await operation(cancellationToken).ConfigureAwait(false))
                return true;

            ErrorMessage = "The automation host rejected this change.";
            return false;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    private static void ReplaceCollection<TSource, TTarget>(
        ObservableCollection<TTarget> target,
        IEnumerable<TSource> source,
        Func<TSource, TTarget> projector)
    {
        target.Clear();
        foreach (var item in source ?? [])
            target.Add(projector(item));
    }
}

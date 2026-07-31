using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Abstractions.Notifications;
using UniversalDeviceToolkit.Abstractions.PackageDownloader;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace UniversalDeviceToolkit.ViewModels;

public partial class PackagesViewModel : ObservableObject
{
    private readonly IHiddenPackagesManager _hiddenPackagesManager;
    private readonly IPackageDownloaderFactory _factory;
    private readonly INotificationService _notificationService;
    private readonly IStringLocalizer _localizer;
    private readonly OS _currentOS;
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _filterDebounceCts;

    [ObservableProperty]
    private ObservableCollection<PackageInfo> _packages = new();

    [ObservableProperty]
    private ObservableCollection<PackageInfo> _filteredPackages = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private float _progress;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private int _sortingIndex = 2;

    [ObservableProperty]
    private bool _onlyShowUpdates;

    [ObservableProperty]
    private string _machineType = string.Empty;

    [ObservableProperty]
    private OS _selectedOS;

    [ObservableProperty]
    private PackageDownloaderType _downloaderType = PackageDownloaderType.Vantage;

    public IReadOnlyList<OS> AvailableOperatingSystems { get; } = Enum.GetValues<OS>();

    public PackagesViewModel(
        IHiddenPackagesManager hiddenPackagesManager,
        IPackageDownloaderFactory factory,
        INotificationService notificationService,
        IStringLocalizer localizer,
        OS currentOS)
    {
        _hiddenPackagesManager = hiddenPackagesManager;
        _factory = factory;
        _notificationService = notificationService;
        _localizer = localizer;
        _currentOS = currentOS;

        Packages.CollectionChanged += OnPackagesCollectionChanged;
        SelectedOS = currentOS;
    }

    public bool HasNoFilteredPackages => FilteredPackages.Count == 0 && Packages.Count > 0;

    public bool HasHiddenPackages => _hiddenPackagesManager.HiddenPackageIds.Count > 0;

    partial void OnIsLoadingChanged(bool value)
    {
        LoadPackagesCommand.NotifyCanExecuteChanged();
        CancelLoadCommand.NotifyCanExecuteChanged();
    }

    partial void OnFilterTextChanged(string value)
    {
        var previous = Interlocked.Exchange(ref _filterDebounceCts, new CancellationTokenSource());
        if (previous is not null)
        {
            try { previous.Cancel(); } catch (ObjectDisposedException) { }
            previous.Dispose();
        }
        _ = DebouncedRefreshFilteredPackagesAsync(_filterDebounceCts!.Token);
    }

    private async Task DebouncedRefreshFilteredPackagesAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(300, token);
            if (!token.IsCancellationRequested)
                RefreshFilteredPackages();
        }
        catch (OperationCanceledException)
        {
        }
    }

    partial void OnSortingIndexChanged(int value) => RefreshFilteredPackages();

    partial void OnOnlyShowUpdatesChanged(bool value) => RefreshFilteredPackages();

    private void OnPackagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshFilteredPackages();
        OnPropertyChanged(nameof(HasNoFilteredPackages));
    }

    private void RefreshFilteredPackages()
    {
        var filtered = GetFilteredPackages();
        FilteredPackages.Clear();
        foreach (var package in filtered)
            FilteredPackages.Add(package);
        OnPropertyChanged(nameof(HasNoFilteredPackages));
    }

    [RelayCommand(CanExecute = nameof(CanLoadPackages))]
    private async Task LoadPackagesAsync()
    {
        if (string.IsNullOrWhiteSpace(MachineType) || MachineType.Length != 4)
            return;

        var previous = Interlocked.Exchange(ref _loadCts, new CancellationTokenSource());
        if (previous is not null)
        {
            try { previous.Cancel(); } catch (ObjectDisposedException) { }
            previous.Dispose();
        }
        var token = _loadCts!.Token;

        IsLoading = true;
        Progress = 0;
        try
        {
            var downloader = _factory.GetInstance(DownloaderType);
            var packages = await downloader.GetPackagesAsync(
                MachineType,
                SelectedOS,
                new Progress<float>(p => Progress = p),
                token);

            Packages = new ObservableCollection<PackageInfo>(packages);
            Packages.CollectionChanged += OnPackagesCollectionChanged;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            await _notificationService.ShowAsync(
                _localizer.GetString("PackagesPage_DownloadFailed_Title", "Download Failed"),
                _localizer.GetString("PackagesPage_DownloadFailed_Message", "Failed to download packages. Please try again."),
                NotificationType.Error);
        }
        finally
        {
            IsLoading = false;
            Interlocked.Exchange(ref _loadCts, null)?.Dispose();
        }
    }

    private bool CanLoadPackages() => !IsLoading;

    [RelayCommand(CanExecute = nameof(CanCancelLoad))]
    private void CancelLoad()
    {
        if (_loadCts is null)
            return;

        try
        {
            if (!_loadCts.IsCancellationRequested)
                _loadCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private bool CanCancelLoad() => IsLoading;

    [RelayCommand]
    private void ClearHiddenPackages()
    {
        _hiddenPackagesManager.ClearHiddenPackages();
        OnPropertyChanged(nameof(HasHiddenPackages));
        RefreshFilteredPackages();
    }

    public List<PackageInfo> GetFilteredPackages()
    {
        IEnumerable<PackageInfo> result = SortingIndex switch
        {
            0 => Packages.OrderBy(p => p.Title),
            1 => Packages.OrderBy(p => p.Category),
            2 => Packages.OrderByDescending(p => p.ReleaseDate),
            _ => Packages.AsEnumerable(),
        };

        result = result.Where(p => !_hiddenPackagesManager.HiddenPackageIds.Contains(p.Id));

        if (!string.IsNullOrWhiteSpace(FilterText))
            result = result.Where(p => p.Index.Contains(FilterText, StringComparison.OrdinalIgnoreCase));

        if (OnlyShowUpdates)
            result = result.Where(p => p.IsUpdate);

        return result.ToList();
    }
}

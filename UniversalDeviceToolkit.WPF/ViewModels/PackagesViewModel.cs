using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.PackageDownloader;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.ViewModels;

public partial class PackagesViewModel : ObservableObject
{
    private readonly PackageDownloaderSettings _settings;
    private readonly PackageDownloaderFactory _factory;
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _filterDebounceCts;

    [ObservableProperty]
    private ObservableCollection<Package> _packages = new();

    [ObservableProperty]
    private ObservableCollection<Package> _filteredPackages = new();

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
    private PackageDownloaderFactory.Type _downloaderType = PackageDownloaderFactory.Type.Vantage;

    public IReadOnlyList<OS> AvailableOperatingSystems { get; } = Enum.GetValues<OS>();

    public PackagesViewModel(PackageDownloaderSettings settings, PackageDownloaderFactory factory)
    {
        _settings = settings;
        _factory = factory;

        Packages.CollectionChanged += OnPackagesCollectionChanged;
        SelectedOS = OSExtensions.GetCurrent();
    }

    public bool HasNoFilteredPackages => FilteredPackages.Count == 0 && Packages.Count > 0;

    public bool HasHiddenPackages => _settings.Store.HiddenPackages.Count > 0;

    partial void OnIsLoadingChanged(bool value)
    {
        LoadPackagesCommand.NotifyCanExecuteChanged();
        CancelLoadCommand.NotifyCanExecuteChanged();
    }

    partial void OnFilterTextChanged(string value)
    {
        var cts = CtsSwap.Replace(ref _filterDebounceCts, new CancellationTokenSource());
        _ = DebouncedRefreshFilteredPackagesAsync(cts.Token);
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

        CtsSwap.Replace(ref _loadCts, new CancellationTokenSource());
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

            Packages = new ObservableCollection<Package>(packages);
            Packages.CollectionChanged += OnPackagesCollectionChanged;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load packages", ex);

            await SnackbarHelper.ShowAsync(Resource.PackagesPage_DownloadFailed_Title,
                Resource.PackagesPage_DownloadFailed_Message,
                SnackbarType.Error);
        }
        finally
        {
            IsLoading = false;
            CtsSwap.Replace(ref _loadCts, null);
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
        _settings.Store.HiddenPackages.Clear();
        _settings.SynchronizeStore();
        OnPropertyChanged(nameof(HasHiddenPackages));
        RefreshFilteredPackages();
    }

    public List<Package> GetFilteredPackages()
    {
        IEnumerable<Package> result = SortingIndex switch
        {
            0 => Packages.OrderBy(p => p.Title),
            1 => Packages.OrderBy(p => p.Category),
            2 => Packages.OrderByDescending(p => p.ReleaseDate),
            _ => Packages.AsEnumerable(),
        };

        result = result.Where(p => !_settings.Store.HiddenPackages.Contains(p.Id));

        if (!string.IsNullOrWhiteSpace(FilterText))
            result = result.Where(p => p.Index.Contains(FilterText, StringComparison.OrdinalIgnoreCase));

        if (OnlyShowUpdates)
            result = result.Where(p => p.IsUpdate);

        return result.ToList();
    }
}

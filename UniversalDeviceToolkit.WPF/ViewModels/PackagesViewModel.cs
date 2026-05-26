using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.PackageDownloader;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalDeviceToolkit.WPF.ViewModels;

public partial class PackagesViewModel : ObservableObject
{
    private readonly PackageDownloaderSettings _settings;
    private readonly PackageDownloaderFactory _factory;

    [ObservableProperty]
    private ObservableCollection<Package> _packages = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private float _progress;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private int _sortingIndex = 2; // 默认按发布日期排�?
    [ObservableProperty]
    private bool _onlyShowUpdates;

    [ObservableProperty]
    private string _machineType = string.Empty;

    [ObservableProperty]
    private OS _selectedOS;

    public PackagesViewModel(PackageDownloaderSettings settings, PackageDownloaderFactory factory)
    {
        _settings = settings;
        _factory = factory;
    }

    [RelayCommand]
    private async Task LoadPackagesAsync()
    {
        if (string.IsNullOrWhiteSpace(MachineType) || MachineType.Length != 4)
            return;

        IsLoading = true;
        try
        {
            var downloader = _factory.GetInstance(PackageDownloaderFactory.Type.Vantage);
            var packages = await downloader.GetPackagesAsync(
                MachineType,
                SelectedOS,
                new Progress<float>(p => Progress = p),
                CancellationToken.None);

            Packages = new ObservableCollection<Package>(packages);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load packages", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CancelLoad()
    {
        // 取消加载逻辑
    }

    public List<Package> GetFilteredPackages()
    {
        var result = SortingIndex switch
        {
            0 => Packages.OrderBy(p => p.Title),
            1 => Packages.OrderBy(p => p.Category),
            2 => Packages.OrderByDescending(p => p.ReleaseDate),
            _ => Packages.AsEnumerable(),
        };

        if (!string.IsNullOrWhiteSpace(FilterText))
            result = result.Where(p => p.Index.Contains(FilterText, StringComparison.OrdinalIgnoreCase));

        if (OnlyShowUpdates)
            result = result.Where(p => p.IsUpdate);

        return result.ToList();
    }
}

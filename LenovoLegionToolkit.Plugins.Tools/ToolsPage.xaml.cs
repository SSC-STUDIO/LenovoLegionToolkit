using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using LenovoLegionToolkit.WPF.Windows;
using SnackbarType = LenovoLegionToolkit.WPF.SnackbarType;
using Newtonsoft.Json;
using Wpf.Ui.Common;

namespace LenovoLegionToolkit.Plugins.Tools;

public partial class ToolsPage : UserControl, INotifyPropertyChanged
{
    private readonly string _toolsDirectory;
    private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly IPluginManager _pluginManager = IoCContainer.Resolve<IPluginManager>();
    private ObservableCollection<ToolCategoryViewModel> _toolCategories = new();
    private ToolCategoryViewModel? _selectedCategory;
    private bool _hasTools;
    private ToolViewModel? _selectedTool;
    private static Dictionary<string, ToolInfoFromJson>? _toolsInfoCache;
    private bool _extensionsEnabled;

    public ObservableCollection<ToolCategoryViewModel> ToolCategories
    {
        get => _toolCategories;
        set
        {
            _toolCategories = value;
            HasTools = value != null && value.Count > 0;
            OnPropertyChanged();
        }
    }

    public ToolCategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (_selectedCategory == value)
                return;

            if (_selectedCategory != null)
                _selectedCategory.IsSelected = false;

            _selectedCategory = value;

            if (_selectedCategory != null)
            {
                _selectedCategory.IsSelected = true;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(FilteredTools));
            
            RefreshExtensionsUI();
        }
    }

    public ObservableCollection<ToolViewModel> FilteredTools
    {
        get
        {
            if (_selectedCategory == null)
                return new ObservableCollection<ToolViewModel>();
            return _selectedCategory.Tools;
        }
    }

    public bool HasTools
    {
        get => _hasTools;
        private set
        {
            if (_hasTools == value)
                return;
            _hasTools = value;
            OnPropertyChanged();
        }
    }

    public ToolsPage()
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _toolsDirectory = Path.Combine(appDirectory, "tools");

        InitializeComponent();
        DataContext = this;
        
        Loaded += ToolsPage_Loaded;
        SetResourceStrings();
    }

    private void SetResourceStrings()
    {
        var resourceManager = Resource.ResourceManager;
        var culture = Resource.Culture;
        
        if (FindName("_titleTextBlock") is TextBlock titleTextBlock)
            titleTextBlock.Text = resourceManager.GetString("ToolsPage_Title", culture) ?? "Tools";
        
        if (FindName("_descriptionTextBlock") is TextBlock descriptionTextBlock)
            descriptionTextBlock.Text = resourceManager.GetString("ToolsPage_Description", culture) ?? "System tools and utilities";
        
        if (FindName("_emptyTitleTextBlock") is TextBlock emptyTitleTextBlock)
            emptyTitleTextBlock.Text = resourceManager.GetString("ToolsPage_Empty", culture) ?? "No tools found";
        
        if (FindName("_emptyDescriptionTextBlock") is TextBlock emptyDescriptionTextBlock)
            emptyDescriptionTextBlock.Text = resourceManager.GetString("ToolsPage_EmptyDescription", culture) ?? "Place tools in the tools directory";
        
        if (FindName("ToolDetailsLaunchButton") is Wpf.Ui.Controls.Button launchButton)
            launchButton.Content = resourceManager.GetString("ToolsPage_Launch_Button", culture) ?? "Launch";
    }

    public bool ExtensionsEnabled
    {
        get => _extensionsEnabled;
        set
        {
            if (_extensionsEnabled == value)
                return;
            _extensionsEnabled = value;
            OnPropertyChanged();
        }
    }

    private async void ToolsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadToolsAsync();
        RefreshExtensionsUI();
    }

    public async void RefreshExtensionsUI()
    {
        var previousExtensionsEnabled = ExtensionsEnabled;
        ExtensionsEnabled = _applicationSettings.Store.ExtensionsEnabled;
        
        if (previousExtensionsEnabled != ExtensionsEnabled)
        {
            await LoadToolsAsync();
            return;
        }
        
        UpdatePluginsUI();
    }

    private void UpdatePluginsUI()
    {
        if (!ExtensionsEnabled)
            return;
    }

    private async Task LoadToolsAsync()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Looking for tools directory: {_toolsDirectory}");

            if (!Directory.Exists(_toolsDirectory))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Tools directory not found: {_toolsDirectory}");

                ToolCategories = new ObservableCollection<ToolCategoryViewModel>();
                return;
            }

            var categories = await Task.Run(async () =>
            {
                LoadToolsInfoCache(_toolsDirectory);

                var result = new Dictionary<string, ToolCategoryViewModel>();
                var categoryDirs = Directory.GetDirectories(_toolsDirectory);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Found {categoryDirs.Length} category directories in {_toolsDirectory}");

                var categoryMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "CPU_Tools", "Hardware_Tools" },
                    { "GPU_Tools", "Hardware_Tools" },
                    { "Memory_Tools", "Hardware_Tools" },
                    { "Storage_Tools", "Hardware_Tools" },
                    { "Gaming_Tools", "Gaming_Tools" },
                    { "Display_Tools", "System_Tools" },
                    { "Peripherals_Tools", "System_Tools" },
                    { "System_Diagnostic_Tools", "System_Tools" },
                    { "Stress_Testing_Tools", "System_Tools" },
                    { "Other_Tools", "System_Tools" }
                };

                var categoryTasks = categoryDirs.Select(async categoryDir =>
                {
                    var physicalCategoryName = Path.GetFileName(categoryDir);
                    var tools = await ScanToolsInDirectoryAsync(categoryDir);

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Category '{physicalCategoryName}': found {tools.Count} tools");

                    if (tools.Count > 0)
                    {
                        var displayCategoryName = categoryMapping.TryGetValue(physicalCategoryName, out var mappedName)
                            ? mappedName
                            : physicalCategoryName;

                        return new
                        {
                            DisplayCategoryName = displayCategoryName,
                            PhysicalCategoryName = physicalCategoryName,
                            Tools = tools
                        };
                    }
                    return null;
                });

                var categoryResults = await Task.WhenAll(categoryTasks);

                var commandToolsByCategory = new Dictionary<string, List<ToolViewModel>>();
                var regularTools = new List<(string DisplayCategoryName, string PhysicalCategoryName, List<ToolViewModel> Tools)>();

                LoadCommandToolsFromJson(_toolsDirectory, commandToolsByCategory);

                foreach (var categoryResult in categoryResults.Where(c => c != null)!)
                {
                    if (categoryResult == null) continue;

                    var commandToolsInCategory = categoryResult.Tools.Where(t => !string.IsNullOrEmpty(t.Command)).ToList();
                    var regularToolsInCategory = categoryResult.Tools.Where(t => string.IsNullOrEmpty(t.Command)).ToList();

                    if (commandToolsInCategory.Count > 0)
                    {
                        if (!commandToolsByCategory.ContainsKey(categoryResult.PhysicalCategoryName))
                        {
                            commandToolsByCategory[categoryResult.PhysicalCategoryName] = new List<ToolViewModel>();
                        }
                        commandToolsByCategory[categoryResult.PhysicalCategoryName].AddRange(commandToolsInCategory);
                    }

                    if (regularToolsInCategory.Count > 0)
                    {
                        regularTools.Add((categoryResult.DisplayCategoryName, categoryResult.PhysicalCategoryName, regularToolsInCategory));
                    }
                }

                foreach (var (displayCategoryName, physicalCategoryName, tools) in regularTools)
                {
                    if (!result.ContainsKey(displayCategoryName))
                    {
                        result[displayCategoryName] = new ToolCategoryViewModel
                        {
                            CategoryName = GetCategoryDisplayName(displayCategoryName),
                            Tools = new ObservableCollection<ToolViewModel>(),
                            SubCategories = new ObservableCollection<ToolSubCategoryViewModel>()
                        };
                    }

                    var subCategory = new ToolSubCategoryViewModel
                    {
                        SubCategoryName = GetCategoryDisplayName(physicalCategoryName),
                        Tools = new ObservableCollection<ToolViewModel>(tools)
                    };
                    result[displayCategoryName].SubCategories.Add(subCategory);

                    foreach (var tool in tools)
                    {
                        result[displayCategoryName].Tools.Add(tool);
                    }
                }

                if (commandToolsByCategory.Count > 0)
                {
                    result["Batch_Tools"] = new ToolCategoryViewModel
                    {
                        CategoryName = GetCategoryDisplayName("Batch_Tools"),
                        Tools = new ObservableCollection<ToolViewModel>(),
                        SubCategories = new ObservableCollection<ToolSubCategoryViewModel>(),
                        IsBatchToolsCategory = true
                    };

                    foreach (var kvp in commandToolsByCategory)
                    {
                        var subCategory = new ToolSubCategoryViewModel
                        {
                            SubCategoryName = GetCategoryDisplayName(kvp.Key),
                            Tools = new ObservableCollection<ToolViewModel>(kvp.Value)
                        };
                        result["Batch_Tools"].SubCategories.Add(subCategory);

                        foreach (var tool in kvp.Value)
                        {
                            result["Batch_Tools"].Tools.Add(tool);
                        }
                    }
                }

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Total display categories with tools: {result.Count}");

                return result.Values
                    .OrderBy(c => c.CategoryName)
                    .ToList();
            });

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var previousSelected = _selectedCategory;
                if (previousSelected != null)
                {
                    previousSelected.IsSelected = false;
                }
                _selectedCategory = null;

                ToolCategories = new ObservableCollection<ToolCategoryViewModel>(categories);
            });

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var batchToolsCategory = ToolCategories?.FirstOrDefault(c => c.IsBatchToolsCategory);

                if (batchToolsCategory != null)
                {
                    SelectedCategory = batchToolsCategory;
                }
                else if (ToolCategories != null && ToolCategories.Count > 0)
                {
                    SelectedCategory = ToolCategories.FirstOrDefault();
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load tools.", ex);

            ToolCategories = new ObservableCollection<ToolCategoryViewModel>();
        }
    }

    private async Task<List<ToolViewModel>> ScanToolsInDirectoryAsync(string directory)
    {
        var tools = new List<ToolViewModel>();

        try
        {
            var subDirs = Directory.GetDirectories(directory);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Scanning directory '{directory}': found {subDirs.Length} subdirectories");

            var toolTasks = subDirs.Select(subDir =>
            {
                var toolName = Path.GetFileName(subDir);
                var toolInfo = LoadToolInfo(subDir, toolName);

                if (!string.IsNullOrEmpty(toolInfo.Command))
                {
                    var toolDisplayName = !string.IsNullOrWhiteSpace(toolInfo.DisplayName)
                        ? toolInfo.DisplayName
                        : !string.IsNullOrWhiteSpace(toolInfo.Name)
                            ? toolInfo.Name
                            : !string.IsNullOrWhiteSpace(toolName)
                                ? toolName
                                : "Unknown Tool";

                    var tool = new ToolViewModel
                    {
                        Name = toolDisplayName,
                        Description = toolInfo.Description ?? GetToolDescription(toolName),
                        ExecutablePath = string.Empty,
                        WorkingDirectory = subDir,
                        IconSource = null,
                        IsBatchFile = false,
                        Command = toolInfo.Command,
                        Version = toolInfo.Version,
                        Author = toolInfo.Author
                    };
                    return Task.FromResult<ToolViewModel?>(tool);
                }

                var toolPath = FindToolExecutable(subDir);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Tool '{toolName}': executable path = {toolPath ?? "NOT FOUND"}");

                if (toolPath != null)
                {
                    var toolDisplayName = !string.IsNullOrWhiteSpace(toolInfo.DisplayName)
                        ? toolInfo.DisplayName
                        : !string.IsNullOrWhiteSpace(toolInfo.Name)
                            ? toolInfo.Name
                            : !string.IsNullOrWhiteSpace(toolName)
                                ? toolName
                                : "Unknown Tool";

                    var tool = new ToolViewModel
                    {
                        Name = toolDisplayName,
                        Description = toolInfo.Description ?? GetToolDescription(toolName),
                        ExecutablePath = toolPath,
                        WorkingDirectory = subDir,
                        IconSource = null,
                        IsBatchFile = false,
                        Command = null,
                        Version = toolInfo.Version,
                        Author = toolInfo.Author
                    };

                    {
                        var iconLoadTask = Task.Run(async () =>
                        {
                            try
                            {
                                var icon = System.Drawing.Icon.ExtractAssociatedIcon(toolPath);
                                if (icon != null)
                                {
                                    var iconSource = await Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        try
                                        {
                                            var imageSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                                icon.Handle,
                                                System.Windows.Int32Rect.Empty,
                                                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                                            imageSource.Freeze();
                                            return imageSource;
                                        }
                                        catch
                                        {
                                            return null;
                                        }
                                        finally
                                        {
                                            icon?.Dispose();
                                        }
                                    });

                                    if (iconSource != null)
                                    {
                                        await Application.Current.Dispatcher.InvokeAsync(() =>
                                        {
                                            tool.IconSource = iconSource;
                                        });
                                    }
                                }
                            }
                            catch
                            {
                            }
                        });

                        _ = iconLoadTask.ContinueWith(t =>
                        {
                            if (t.IsFaulted && Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Failed to load icon for tool: {toolName}", t.Exception);
                        }, TaskContinuationOptions.OnlyOnFaulted);
                    }

                    return Task.FromResult<ToolViewModel?>(tool);
                }
                return Task.FromResult<ToolViewModel?>(null);
            });

            var toolResults = await Task.WhenAll(toolTasks);
            tools.AddRange(toolResults.Where(t => t != null)!);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to scan tools in directory: {directory}", ex);
        }

        return tools;
    }

    private static void LoadToolsInfoCache(string toolsDirectory)
    {
        if (_toolsInfoCache != null)
            return;

        var toolsJsonPath = Path.Combine(toolsDirectory, "tools.json");
        try
        {
            _toolsInfoCache = new Dictionary<string, ToolInfoFromJson>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(toolsJsonPath))
            {
                var jsonContent = File.ReadAllText(toolsJsonPath);
                var toolsData = JsonConvert.DeserializeObject<ToolsJsonData>(jsonContent);

                if (toolsData?.Categories != null)
                {
                    foreach (var category in toolsData.Categories)
                    {
                        if (category.Tools != null)
                        {
                            foreach (var tool in category.Tools)
                            {
                                var key = $"{category.Name}/{tool.Name}";
                                _toolsInfoCache[key] = tool;
                            }
                        }
                    }

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Loaded {_toolsInfoCache.Count} tools from tools.json");
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load tools.json: {toolsJsonPath}", ex);
            _toolsInfoCache = new Dictionary<string, ToolInfoFromJson>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void LoadCommandToolsFromJson(string toolsDirectory, Dictionary<string, List<ToolViewModel>> commandToolsByCategory)
    {
        var toolsJsonPath = Path.Combine(toolsDirectory, "tools.json");
        try
        {
            if (!File.Exists(toolsJsonPath))
                return;

            var jsonContent = File.ReadAllText(toolsJsonPath);
            var toolsData = JsonConvert.DeserializeObject<ToolsJsonData>(jsonContent);

            if (toolsData?.Categories == null)
                return;

            var currentLanguage = Resource.Culture?.Name ?? Thread.CurrentThread.CurrentUICulture.Name;
            var isEnglish = currentLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase) ||
                           string.IsNullOrEmpty(currentLanguage);

            foreach (var category in toolsData.Categories)
            {
                if (category.Tools == null)
                    continue;

                foreach (var toolInfo in category.Tools)
                {
                    if (string.IsNullOrWhiteSpace(toolInfo.Command))
                        continue;

                    var toolFolderPath = Path.Combine(toolsDirectory, category.Name, toolInfo.Name);
                    if (Directory.Exists(toolFolderPath))
                        continue;

                    var toolDisplayName = !string.IsNullOrWhiteSpace(toolInfo.DisplayName)
                        ? toolInfo.DisplayName
                        : !string.IsNullOrWhiteSpace(toolInfo.Name)
                            ? toolInfo.Name
                            : "Unknown Tool";

                    var description = isEnglish && !string.IsNullOrEmpty(toolInfo.DescriptionEn)
                        ? toolInfo.DescriptionEn
                        : toolInfo.Description;

                    var tool = new ToolViewModel
                    {
                        Name = toolDisplayName,
                        Description = description ?? string.Empty,
                        ExecutablePath = string.Empty,
                        WorkingDirectory = toolsDirectory,
                        IconSource = null,
                        IsBatchFile = false,
                        Command = toolInfo.Command,
                        Version = toolInfo.Version,
                        Author = toolInfo.Author
                    };

                    if (!commandToolsByCategory.ContainsKey(category.Name))
                    {
                        commandToolsByCategory[category.Name] = new List<ToolViewModel>();
                    }

                    commandToolsByCategory[category.Name].Add(tool);
                }
            }

            if (Log.Instance.IsTraceEnabled)
            {
                var totalCommandTools = commandToolsByCategory.Values.Sum(tools => tools.Count);
                Log.Instance.Trace($"Loaded {totalCommandTools} command tools from tools.json (without physical folders)");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load command tools from tools.json: {toolsJsonPath}", ex);
        }
    }

    private ToolInfo LoadToolInfo(string toolDirectory, string defaultName)
    {
        try
        {
            if (_toolsInfoCache != null)
            {
                var categoryName = Path.GetFileName(Path.GetDirectoryName(toolDirectory));
                var toolName = Path.GetFileName(toolDirectory);
                var cacheKey = $"{categoryName}/{toolName}";

                if (_toolsInfoCache.TryGetValue(cacheKey, out var cachedInfo))
                {
                    var currentLanguage = Resource.Culture?.Name ?? Thread.CurrentThread.CurrentUICulture.Name;
                    var isEnglish = currentLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase) ||
                                   string.IsNullOrEmpty(currentLanguage);

                    var description = isEnglish && !string.IsNullOrEmpty(cachedInfo.DescriptionEn)
                        ? cachedInfo.DescriptionEn
                        : !isEnglish && !string.IsNullOrEmpty(cachedInfo.DescriptionEn)
                            ? cachedInfo.DescriptionEn
                            : cachedInfo.Description;

                    return new ToolInfo
                    {
                        Name = cachedInfo.Name,
                        DisplayName = cachedInfo.DisplayName,
                        Description = description,
                        Version = cachedInfo.Version,
                        Author = cachedInfo.Author,
                        Command = cachedInfo.Command
                    };
                }
            }

            var jsonFiles = new[] { "tool.json", "info.json" };

            foreach (var jsonFile in jsonFiles)
            {
                var jsonPath = Path.Combine(toolDirectory, jsonFile);
                if (File.Exists(jsonPath))
                {
                    var jsonContent = File.ReadAllText(jsonPath);
                    var toolInfo = JsonConvert.DeserializeObject<ToolInfo>(jsonContent);

                    if (toolInfo != null)
                    {
                        var currentLanguage = Resource.Culture?.Name ?? Thread.CurrentThread.CurrentUICulture.Name;
                        var isEnglish = currentLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase) ||
                                       string.IsNullOrEmpty(currentLanguage);

                        if (isEnglish && !string.IsNullOrEmpty(toolInfo.DescriptionEn))
                        {
                            toolInfo.Description = toolInfo.DescriptionEn;
                        }
                        else if (!isEnglish && !string.IsNullOrEmpty(toolInfo.DescriptionEn))
                        {
                            toolInfo.Description = toolInfo.DescriptionEn;
                        }

                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Loaded tool info from {jsonPath}: Name={toolInfo.Name}, Description={toolInfo.Description}");
                        return toolInfo;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load tool info from directory: {toolDirectory}", ex);
        }

        return new ToolInfo();
    }

    private string? FindToolExecutable(string directory)
    {
        try
        {
            var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.TopDirectoryOnly);
            if (exeFiles.Length > 0)
            {
                var dirName = Path.GetFileName(directory);
                var matchingExe = exeFiles.FirstOrDefault(f =>
                    Path.GetFileNameWithoutExtension(f).Equals(dirName, StringComparison.OrdinalIgnoreCase));

                return matchingExe ?? exeFiles[0];
            }

            var subDirs = Directory.GetDirectories(directory);
            foreach (var subDir in subDirs)
            {
                var subExeFiles = Directory.GetFiles(subDir, "*.exe", SearchOption.TopDirectoryOnly);
                if (subExeFiles.Length > 0)
                {
                    return subExeFiles[0];
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to find executable in directory: {directory}", ex);
        }

        return null;
    }

    private string GetCategoryDisplayName(string categoryName)
    {
        var resourceKey = categoryName switch
        {
            "Hardware_Tools" => "ToolsPage_Category_Hardware_Tools",
            "Gaming_Tools" => "ToolsPage_Category_Gaming_Tools",
            "System_Tools" => "ToolsPage_Category_System_Tools",
            "Batch_Tools" => "ToolsPage_Category_Batch_Tools",
            "Extensions" => "ToolsPage_Category_Extensions",
            "CPU_Tools" => "ToolsPage_Category_CPU_Tools",
            "Display_Tools" => "ToolsPage_Category_Display_Tools",
            "GPU_Tools" => "ToolsPage_Category_GPU_Tools",
            "Memory_Tools" => "ToolsPage_Category_Memory_Tools",
            "Other_Tools" => "ToolsPage_Category_Other_Tools",
            "Peripherals_Tools" => "ToolsPage_Category_Peripherals_Tools",
            "Storage_Tools" => "ToolsPage_Category_Storage_Tools",
            "Stress_Testing_Tools" => "ToolsPage_Category_Stress_Testing_Tools",
            "System_Diagnostic_Tools" => "ToolsPage_Category_System_Diagnostic_Tools",
            _ => null
        };

        if (resourceKey != null)
        {
            var value = Resource.ResourceManager.GetString(resourceKey, Resource.Culture);
            if (!string.IsNullOrEmpty(value))
                return value;

            value = Resource.ResourceManager.GetString(resourceKey, new CultureInfo("en"));
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return categoryName;
    }

    private string GetToolDescription(string toolName)
    {
        var description = toolName
            .Replace("_", " ")
            .Replace("-", " ")
            .Trim();

        if (description.Length > 40)
        {
            description = description.Substring(0, 37) + "...";
        }

        return description;
    }

    private void CategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.Button button || button.Tag is not ToolCategoryViewModel category)
            return;

        SelectedCategory = category;
    }

    private void CategoryRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.RadioButton radioButton || radioButton.Tag is not ToolCategoryViewModel category)
            return;

        SelectedCategory = category;
    }

    private void ToolCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not ToolViewModel tool)
            return;

        if (e.ClickCount == 2)
        {
            LaunchTool(tool);
        }
        else
        {
            SelectedTool = tool;
        }
    }

    private void LaunchTool(ToolViewModel tool)
    {
        if (tool == null)
            return;

        try
        {
            if (!string.IsNullOrEmpty(tool.Command))
            {
                var command = tool.Command.Trim();
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    WorkingDirectory = tool.WorkingDirectory ?? string.Empty,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process.Start(processStartInfo);
            }
            else
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = tool.ExecutablePath,
                    WorkingDirectory = tool.WorkingDirectory,
                    UseShellExecute = true
                };

                Process.Start(processStartInfo);
            }

            SelectedTool = null;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to start tool: {tool.Name}", ex);

            var title = Resource.ResourceManager.GetString("ToolsPage_Title", Resource.Culture) ?? "Tools";
            var errorMsg = Resource.ResourceManager.GetString("ToolsPage_Error_StartTool", Resource.Culture) ?? "Failed to start tool";
            SnackbarHelper.Show(title, $"{errorMsg}: {tool.Name}", SnackbarType.Error);
        }
    }

    private void ToolsScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
            return;

        if (scrollViewer.ScrollableHeight > 0)
        {
            var offset = scrollViewer.VerticalOffset - (e.Delta / 3.0);
            scrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(offset, scrollViewer.ScrollableHeight)));
            e.Handled = true;
        }
    }

    private void ToolsPage_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is Grid grid)
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(grid);
            if (scrollViewer != null && scrollViewer.ScrollableHeight > 0)
            {
                var offset = scrollViewer.VerticalOffset - (e.Delta / 3.0);
                scrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(offset, scrollViewer.ScrollableHeight)));
                e.Handled = true;
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;

            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
                return childOfChild;
        }
        return null;
    }

    public ToolViewModel? SelectedTool
    {
        get => _selectedTool;
        set
        {
            if (_selectedTool == value)
                return;

            _selectedTool = value;
            UpdateToolDetailsPanel();
            OnPropertyChanged();
        }
    }

    private void UpdateToolDetailsPanel()
    {
        if (FindName("ToolDetailsPanel") is not Border toolDetailsPanel)
            return;
            
        if (_selectedTool == null)
        {
            toolDetailsPanel.Visibility = Visibility.Collapsed;
            return;
        }

        toolDetailsPanel.Visibility = Visibility.Visible;

        if (FindName("ToolDetailsIcon") is Image toolDetailsIcon &&
            FindName("ToolDetailsFallbackIcon") is Wpf.Ui.Controls.SymbolIcon toolDetailsFallbackIcon)
        {
            if (_selectedTool.IconSource != null)
            {
                toolDetailsIcon.Source = _selectedTool.IconSource;
                toolDetailsIcon.Visibility = Visibility.Visible;
                toolDetailsFallbackIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                toolDetailsIcon.Visibility = Visibility.Collapsed;
                toolDetailsFallbackIcon.Visibility = Visibility.Visible;
            }
        }

        var toolName = !string.IsNullOrWhiteSpace(_selectedTool.Name)
            ? _selectedTool.Name
            : "Unknown Tool";
            
        if (FindName("ToolDetailsName") is TextBlock toolDetailsName)
            toolDetailsName.Text = toolName;
            
        if (FindName("ToolDetailsDescription") is TextBlock toolDetailsDescription)
            toolDetailsDescription.Text = _selectedTool.Description ?? "";

        var resourceManager = Resource.ResourceManager;
        var culture = Resource.Culture;

        if (FindName("ToolDetailsVersion") is TextBlock toolDetailsVersion)
        {
            if (!string.IsNullOrEmpty(_selectedTool.Version))
            {
                var versionFormat = resourceManager.GetString("ToolsPage_Details_Version", culture) ?? "Version: {0}";
                toolDetailsVersion.Text = string.Format(versionFormat, _selectedTool.Version);
                toolDetailsVersion.Visibility = Visibility.Visible;
            }
            else
            {
                toolDetailsVersion.Visibility = Visibility.Collapsed;
            }
        }

        if (FindName("ToolDetailsAuthor") is TextBlock toolDetailsAuthor)
        {
            if (!string.IsNullOrEmpty(_selectedTool.Author))
            {
                var authorFormat = resourceManager.GetString("ToolsPage_Details_Author", culture) ?? "Author: {0}";
                toolDetailsAuthor.Text = string.Format(authorFormat, _selectedTool.Author);
                toolDetailsAuthor.Visibility = Visibility.Visible;
            }
            else
            {
                toolDetailsAuthor.Visibility = Visibility.Collapsed;
            }
        }

        if (FindName("ToolDetailsPath") is TextBlock toolDetailsPath)
        {
            var hasValidPath = !string.IsNullOrWhiteSpace(_selectedTool.ExecutablePath);
            var isCommandTool = !string.IsNullOrWhiteSpace(_selectedTool.Command);

            if (hasValidPath && !isCommandTool)
            {
                var pathFormat = resourceManager.GetString("ToolsPage_Details_Path", culture) ?? "Path: {0}";
                toolDetailsPath.Text = string.Format(pathFormat, _selectedTool.ExecutablePath);
                toolDetailsPath.Visibility = Visibility.Visible;
            }
            else
            {
                toolDetailsPath.Visibility = Visibility.Collapsed;
            }
        }

        if (FindName("ToolDetailsLaunchButton") is Wpf.Ui.Controls.Button toolDetailsLaunchButton)
            toolDetailsLaunchButton.Visibility = Visibility.Visible;
    }

    private void ToolDetailsLaunchButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTool == null)
            return;

        LaunchTool(_selectedTool);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class ToolSubCategoryViewModel
{
    public string SubCategoryName { get; set; } = string.Empty;
    public ObservableCollection<ToolViewModel> Tools { get; set; } = new();
}

public class ToolCategoryViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public string CategoryName { get; set; } = string.Empty;
    public ObservableCollection<ToolViewModel> Tools { get; set; } = new();
    public ObservableCollection<ToolSubCategoryViewModel> SubCategories { get; set; } = new();
    public bool IsBatchToolsCategory { get; set; } = false;
    public bool IsExtensionsCategory { get; set; } = false;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class ToolViewModel : INotifyPropertyChanged
{
    private ImageSource? _iconSource;

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public bool IsBatchFile { get; set; } = false;
    public string? Command { get; set; }

    public ImageSource? IconSource
    {
        get => _iconSource;
        set
        {
            if (_iconSource == value)
                return;
            _iconSource = value;
            OnPropertyChanged();
        }
    }

    public string? Version { get; set; }
    public string? Author { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class ToolInfo
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("displayName")]
    public string? DisplayName { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("description_en")]
    public string? DescriptionEn { get; set; }

    [JsonProperty("version")]
    public string? Version { get; set; }

    [JsonProperty("author")]
    public string? Author { get; set; }

    [JsonProperty("command")]
    public string? Command { get; set; }
}

public class ToolInfoFromJson
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("displayName")]
    public string? DisplayName { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("description_en")]
    public string? DescriptionEn { get; set; }

    [JsonProperty("version")]
    public string? Version { get; set; }

    [JsonProperty("author")]
    public string? Author { get; set; }

    [JsonProperty("command")]
    public string? Command { get; set; }
}

public class ToolsJsonData
{
    [JsonProperty("categories")]
    public List<ToolCategoryJsonData>? Categories { get; set; }
}

public class ToolCategoryJsonData
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("tools")]
    public List<ToolInfoFromJson>? Tools { get; set; }
}


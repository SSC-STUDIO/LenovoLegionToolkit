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
using System.Windows.Input;
using System.Windows.Media;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Pages;

public partial class ToolsPage : INotifyPropertyChanged
{
    private readonly string _toolsDirectory;
    private ObservableCollection<ToolCategoryViewModel> _toolCategories = new();
    private ToolCategoryViewModel? _selectedCategory;
    private bool _hasTools;
    private ToolViewModel? _selectedTool;
    private static Dictionary<string, ToolInfoFromJson>? _toolsInfoCache;

    public ObservableCollection<ToolCategoryViewModel> ToolCategories
    {
        get => _toolCategories;
        set
        {
            _toolCategories = value;
            HasTools = value != null && value.Count > 0;
            if (value != null && value.Count > 0 && _selectedCategory == null)
            {
                SelectedCategory = value.FirstOrDefault();
            }
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
                _selectedCategory.IsSelected = true;
            
            OnPropertyChanged();
            OnPropertyChanged(nameof(FilteredTools));
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
    }

    private async void ToolsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadToolsAsync();
    }

    private async Task LoadToolsAsync()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Looking for tools directory: {_toolsDirectory}");
            
            // 快速检查目录是否存在（在主线程）
            if (!Directory.Exists(_toolsDirectory))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Tools directory not found: {_toolsDirectory}");
                
                // 发生错误时也设置空集合以显示空状态提示
                ToolCategories = new ObservableCollection<ToolCategoryViewModel>();
                return;
            }

            // 在后台线程执行耗时的文件系统操作和图标提取
            var categories = await Task.Run(async () =>
            {
                // 加载工具信息缓存（从 tools.json）
                LoadToolsInfoCache(_toolsDirectory);
                
                var result = new List<ToolCategoryViewModel>();
                var categoryDirs = Directory.GetDirectories(_toolsDirectory);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Found {categoryDirs.Length} category directories in {_toolsDirectory}");

                // 并行处理每个分类以提高性能
                var categoryTasks = categoryDirs.Select(async categoryDir =>
                {
                    var categoryName = Path.GetFileName(categoryDir);
                    var tools = await ScanToolsInDirectoryAsync(categoryDir);
                    
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Category '{categoryName}': found {tools.Count} tools");
                    
                    if (tools.Count > 0)
                    {
                        return new ToolCategoryViewModel
                        {
                            CategoryName = GetCategoryDisplayName(categoryName),
                            Tools = new ObservableCollection<ToolViewModel>(tools)
                        };
                    }
                    return null;
                });

                var categoryResults = await Task.WhenAll(categoryTasks);
                result.AddRange(categoryResults.Where(c => c != null)!);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Total categories with tools: {result.Count}");

                return result.OrderBy(c => c.CategoryName).ToList();
            });

            // 回到 UI 线程更新界面
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ToolCategories = new ObservableCollection<ToolCategoryViewModel>(categories);
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load tools.", ex);
            
            // 发生错误时也设置空集合以显示空状态提示
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
            
            // 并行处理每个工具以提高性能
            var toolTasks = subDirs.Select(subDir =>
            {
                var toolName = Path.GetFileName(subDir);
                var toolPath = FindToolExecutable(subDir);
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Tool '{toolName}': executable path = {toolPath ?? "NOT FOUND"}");
                
                if (toolPath != null)
                {
                    // 尝试加载工具信息 JSON 文件
                    var toolInfo = LoadToolInfo(subDir, toolName);
                    
                    // 先创建工具对象，不立即提取图标（延迟加载以提高初始加载速度）
                    var tool = new ToolViewModel
                    {
                        Name = toolInfo.Name ?? toolName,
                        Description = toolInfo.Description ?? GetToolDescription(toolName),
                        ExecutablePath = toolPath,
                        WorkingDirectory = subDir,
                        IconSource = null, // 延迟加载图标
                        Version = toolInfo.Version,
                        Author = toolInfo.Author
                    };
                    
                    // 延迟加载图标以提高初始加载速度
                    // 图标将在后台异步加载，不阻塞主加载流程
                    var iconLoadTask = Task.Run(async () =>
                    {
                        try
                        {
                            // 提取图标 - ExtractAssociatedIcon 通常会提取 32x32 的图标
                            var icon = System.Drawing.Icon.ExtractAssociatedIcon(toolPath);
                            if (icon != null)
                            {
                                // 在 UI 线程创建 ImageSource
                                var iconSource = await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    try
                                    {
                                        // 创建位图源，使用高质量渲染
                                        var imageSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                            icon.Handle,
                                            System.Windows.Int32Rect.Empty,
                                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                                        
                                        imageSource.Freeze(); // 冻结以便跨线程使用
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
                                
                                // 更新工具的图标（这会触发 UI 更新）
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
                            // 忽略图标提取错误
                        }
                    });
                    
                    // 配置任务以忽略未观察的异常
                    _ = iconLoadTask.ContinueWith(t =>
                    {
                        if (t.IsFaulted && Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Failed to load icon for tool: {toolName}", t.Exception);
                    }, TaskContinuationOptions.OnlyOnFaulted);
                    
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
                                // 使用 "CategoryName/ToolName" 作为键
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

    private ToolInfo LoadToolInfo(string toolDirectory, string defaultName)
    {
        try
        {
            // 首先尝试从 tools.json 缓存中查找工具信息
            if (_toolsInfoCache != null)
            {
                var categoryName = Path.GetFileName(Path.GetDirectoryName(toolDirectory));
                var toolName = Path.GetFileName(toolDirectory);
                var cacheKey = $"{categoryName}/{toolName}";
                
                if (_toolsInfoCache.TryGetValue(cacheKey, out var cachedInfo))
                {
                    // 根据当前语言返回对应的描述
                    var currentLanguage = Resource.Culture?.Name ?? Thread.CurrentThread.CurrentUICulture.Name;
                    var isEnglish = currentLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase) || 
                                   string.IsNullOrEmpty(currentLanguage);
                    
                    var description = isEnglish && !string.IsNullOrEmpty(cachedInfo.DescriptionEn)
                        ? cachedInfo.DescriptionEn
                        : cachedInfo.Description;
                    
                    return new ToolInfo
                    {
                        Name = cachedInfo.Name,
                        Description = description,
                        Version = cachedInfo.Version,
                        Author = cachedInfo.Author
                    };
                }
            }
            
            // 如果缓存中没有，尝试查找 tool.json 或 info.json
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
                        // 根据当前语言返回对应的描述
                        var currentLanguage = Resource.Culture?.Name ?? Thread.CurrentThread.CurrentUICulture.Name;
                        var isEnglish = currentLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase) || 
                                       string.IsNullOrEmpty(currentLanguage);
                        
                        if (isEnglish && !string.IsNullOrEmpty(toolInfo.DescriptionEn))
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

        // 如果无法加载 JSON，返回空信息（将使用默认值）
        return new ToolInfo();
    }

    private string? FindToolExecutable(string directory)
    {
        try
        {
            // 查找 .exe 文件
            var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.TopDirectoryOnly);
            if (exeFiles.Length > 0)
            {
                // 优先选择与目录名相同的exe文件
                var dirName = Path.GetFileName(directory);
                var matchingExe = exeFiles.FirstOrDefault(f => 
                    Path.GetFileNameWithoutExtension(f).Equals(dirName, StringComparison.OrdinalIgnoreCase));
                
                return matchingExe ?? exeFiles[0];
            }

            // 查找 .bat 文件
            var batFiles = Directory.GetFiles(directory, "*.bat", SearchOption.TopDirectoryOnly);
            if (batFiles.Length > 0)
            {
                return batFiles[0];
            }

            // 递归查找子目录中的exe文件（最多一层）
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
            "CPU_Tools" => "ToolsPage_Category_CPU_Tools",
            "Display_Tools" => "ToolsPage_Category_Display_Tools",
            "Gaming_Tools" => "ToolsPage_Category_Gaming_Tools",
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
            var value = Resource.ResourceManager.GetString(resourceKey);
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return categoryName;
    }

    private string GetToolDescription(string toolName)
    {
        // 根据工具名称生成更友好的描述
        var description = toolName
            .Replace("_", " ")
            .Replace("-", " ")
            .Trim();

        // 如果名称很长，截断并添加省略号
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

    private void ToolCard_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.Border border || border.Tag is not ToolViewModel tool)
            return;

        // 如果是双击，直接启动工具
        if (e.ClickCount == 2)
        {
            LaunchTool(tool);
        }
        else
        {
            // 单击显示详细信息在底部面板
            SelectedTool = tool;
        }
    }

    private void LaunchTool(ToolViewModel tool)
    {
        if (tool == null)
            return;

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = tool.ExecutablePath,
                WorkingDirectory = tool.WorkingDirectory,
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
            
            // 启动成功后，清除选择
            SelectedTool = null;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to start tool: {tool.Name}", ex);
            
            var title = Resource.ResourceManager.GetString("ToolsPage_Title") ?? "Tools";
            var errorMsg = Resource.ResourceManager.GetString("ToolsPage_Error_StartTool") ?? "Failed to start tool";
            SnackbarHelper.Show(title, $"{errorMsg}: {tool.Name}", SnackbarType.Error);
        }
    }

    private void ToolsScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not System.Windows.Controls.ScrollViewer scrollViewer || e.Handled)
            return;

        // 确保滚轮事件被ScrollViewer处理，即使鼠标悬停在子控件上
        e.Handled = true;
        var offset = scrollViewer.VerticalOffset - (e.Delta / 3.0);
        scrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(offset, scrollViewer.ScrollableHeight)));
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
        if (_selectedTool == null)
        {
            ToolDetailsPanel.Visibility = Visibility.Collapsed;
            return;
        }

        ToolDetailsPanel.Visibility = Visibility.Visible;

        // 更新图标
        if (_selectedTool.IconSource != null)
        {
            ToolDetailsIcon.Source = _selectedTool.IconSource;
            ToolDetailsIcon.Visibility = Visibility.Visible;
            ToolDetailsFallbackIcon.Visibility = Visibility.Collapsed;
        }
        else
        {
            ToolDetailsIcon.Visibility = Visibility.Collapsed;
            ToolDetailsFallbackIcon.Visibility = Visibility.Visible;
        }

        // 更新文本信息
        ToolDetailsName.Text = _selectedTool.Name;
        ToolDetailsDescription.Text = _selectedTool.Description ?? "";

        // 更新版本
        if (!string.IsNullOrEmpty(_selectedTool.Version))
        {
            var versionFormat = Resource.ResourceManager.GetString("ToolsPage_Details_Version") ?? "Version: {0}";
            ToolDetailsVersion.Text = string.Format(versionFormat, _selectedTool.Version);
            ToolDetailsVersion.Visibility = Visibility.Visible;
        }
        else
        {
            ToolDetailsVersion.Visibility = Visibility.Collapsed;
        }

        // 更新作者
        if (!string.IsNullOrEmpty(_selectedTool.Author))
        {
            var authorFormat = Resource.ResourceManager.GetString("ToolsPage_Details_Author") ?? "Author: {0}";
            ToolDetailsAuthor.Text = string.Format(authorFormat, _selectedTool.Author);
            ToolDetailsAuthor.Visibility = Visibility.Visible;
        }
        else
        {
            ToolDetailsAuthor.Visibility = Visibility.Collapsed;
        }

        // 更新路径
        var pathFormat = Resource.ResourceManager.GetString("ToolsPage_Details_Path") ?? "Path: {0}";
        ToolDetailsPath.Text = string.Format(pathFormat, _selectedTool.ExecutablePath);
        ToolDetailsPath.Visibility = Visibility.Visible;

        // 显示启动按钮
        ToolDetailsLaunchButton.Visibility = Visibility.Visible;
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

public class ToolCategoryViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public string CategoryName { get; set; } = string.Empty;
    public ObservableCollection<ToolViewModel> Tools { get; set; } = new();

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

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("description_en")]
    public string? DescriptionEn { get; set; }

    [JsonProperty("version")]
    public string? Version { get; set; }

    [JsonProperty("author")]
    public string? Author { get; set; }
}

public class ToolInfoFromJson
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonProperty("description_en")]
    public string? DescriptionEn { get; set; }
    
    [JsonProperty("version")]
    public string? Version { get; set; }
    
    [JsonProperty("author")]
    public string? Author { get; set; }
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


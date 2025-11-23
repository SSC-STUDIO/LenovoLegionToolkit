using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Pages;

public partial class ToolsPage : INotifyPropertyChanged
{
    private readonly string _toolsDirectory;
    private ObservableCollection<ToolCategoryViewModel> _toolCategories = new();
    private ToolCategoryViewModel? _selectedCategory;
    private bool _hasTools;
    private readonly HashSet<string> _shownToolDetails = new();

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
            var toolTasks = subDirs.Select(async subDir =>
            {
                var toolName = Path.GetFileName(subDir);
                var toolPath = FindToolExecutable(subDir);
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Tool '{toolName}': executable path = {toolPath ?? "NOT FOUND"}");
                
                if (toolPath != null)
                {
                    // 在后台线程提取图标（这是最耗时的操作）
                    // 注意：ImageSource 创建需要在 UI 线程，但我们可以先提取 Icon，然后批量创建
                    ImageSource? iconSource = null;
                    System.Drawing.Icon? icon = null;
                    try
                    {
                        // 在后台线程提取 Icon（这是耗时操作）
                        icon = System.Drawing.Icon.ExtractAssociatedIcon(toolPath);
                    }
                    catch
                    {
                        // 忽略图标提取错误
                    }
                    
                    // 尝试加载工具信息 JSON 文件
                    var toolInfo = LoadToolInfo(subDir, toolName);
                    
                    // 创建一个包含 Icon 的临时对象，稍后在 UI 线程创建 ImageSource
                    var tool = new ToolViewModel
                    {
                        Name = toolInfo.Name ?? toolName,
                        Description = toolInfo.Description ?? GetToolDescription(toolName),
                        ExecutablePath = toolPath,
                        WorkingDirectory = subDir,
                        IconSource = null, // 稍后设置
                        Version = toolInfo.Version,
                        Author = toolInfo.Author
                    };
                    
                    // 如果有 Icon，在 UI 线程创建 ImageSource
                    if (icon != null)
                    {
                        iconSource = await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            try
                            {
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
                        tool.IconSource = iconSource;
                    }
                    
                    return tool;
                }
                return null;
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

    private ToolInfo LoadToolInfo(string toolDirectory, string defaultName)
    {
        try
        {
            // 尝试查找 tool.json 或 info.json
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

        // 使用工具的唯一标识符（路径）来跟踪是否已显示详细信息
        var toolKey = tool.ExecutablePath;

        // 如果还没有显示过详细信息，则显示详细信息
        if (!_shownToolDetails.Contains(toolKey))
        {
            _shownToolDetails.Add(toolKey);
            ShowToolDetails(tool);
            return;
        }

        // 第二次点击，启动程序
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = tool.ExecutablePath,
                WorkingDirectory = tool.WorkingDirectory,
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
            
            // 启动成功后，重置状态以便下次可以再次查看详细信息
            _shownToolDetails.Remove(toolKey);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to start tool: {tool.Name}", ex);
            
            var title = Resource.ResourceManager.GetString("ToolsPage_Title") ?? "Tools";
            var errorMsg = Resource.ResourceManager.GetString("ToolsPage_Error_StartTool") ?? "Failed to start tool";
            SnackbarHelper.Show(title, $"{errorMsg}: {tool.Name}", SnackbarType.Error);
            
            // 启动失败时也重置状态
            _shownToolDetails.Remove(toolKey);
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

    private void ShowToolDetails(ToolViewModel tool)
    {
        var title = tool.Name;
        var details = new System.Text.StringBuilder();
        
        details.AppendLine(tool.Description);
        
        if (!string.IsNullOrEmpty(tool.Version))
        {
            details.AppendLine();
            details.AppendLine($"版本: {tool.Version}");
        }
        
        if (!string.IsNullOrEmpty(tool.Author))
        {
            details.AppendLine($"作者: {tool.Author}");
        }
        
        details.AppendLine();
        details.AppendLine($"路径: {tool.ExecutablePath}");
        details.AppendLine();
        details.AppendLine("再次点击以启动程序");
        
        System.Windows.MessageBox.Show(details.ToString(), title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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

public class ToolViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public ImageSource? IconSource { get; set; }
    public string? Version { get; set; }
    public string? Author { get; set; }
}

public class ToolInfo
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("version")]
    public string? Version { get; set; }

    [JsonProperty("author")]
    public string? Author { get; set; }
}


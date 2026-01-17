using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.Plugins.Tools;

public partial class ToolsPage : INotifyPropertyChanged
{
    private readonly ToolsPlugin _plugin;
    private readonly ToolPlugin _toolPlugin;
    private ObservableCollection<ToolCategoryViewModel> _categories = new();
    private ToolCategoryViewModel? _selectedCategory;
    private ObservableCollection<ToolViewModel> _tools = new();

    public ObservableCollection<ToolCategoryViewModel> Categories
    {
        get => _categories;
        set
        {
            _categories = value;
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
                Tools = new ObservableCollection<ToolViewModel>(_selectedCategory.Tools);
            }
            else
            {
                Tools.Clear();
            }

            OnPropertyChanged();
            LoadToolsUI();
        }
    }

    public ObservableCollection<ToolViewModel> Tools
    {
        get => _tools;
        set
        {
            _tools = value;
            OnPropertyChanged();
        }
    }

    public ToolsPage(ToolsPlugin plugin)
    {
        _plugin = plugin;
        _toolPlugin = plugin.GetToolPlugin() ?? throw new InvalidOperationException("ToolPlugin is null");

        InitializeComponent();
        DataContext = this;

        LoadCategories();
        LoadCategoriesUI();
    }

    private void LoadCategories()
    {
        try
        {
            var allTools = _toolPlugin.GetAllTools();
            var categoryGroups = allTools.GroupBy(t => t.Category);

            var categories = categoryGroups.Select(g => new ToolCategoryViewModel
            {
                CategoryName = GetCategoryDisplayName(g.Key),
                CategoryKey = g.Key,
                Tools = new ObservableCollection<ToolViewModel>(g.Select(t => new ToolViewModel
                {
                    Name = t.Name,
                    DisplayName = t.DisplayName,
                    Description = t.Description,
                    Version = t.Version,
                    Author = t.Author,
                    Command = t.Command
                }))
            }).ToList();

            Categories = new ObservableCollection<ToolCategoryViewModel>(categories);

            if (Categories.Count > 0)
                SelectedCategory = Categories[0];
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ToolsPage: Error loading categories: {ex.Message}", ex);
        }
    }

    private void LoadCategoriesUI()
    {
        var categoriesItemsControl = this.FindName("_categoriesItemsControl") as ItemsControl;
        if (categoriesItemsControl == null)
            return;

        categoriesItemsControl.Items.Clear();

        foreach (var category in Categories)
        {
            var button = new Button
            {
                Content = category.CategoryName,
                Style = (Style)FindResource("CategoryButtonStyle"),
                Tag = category
            };

            button.Click += CategoryButton_Click;
            categoriesItemsControl.Items.Add(button);
        }
    }

    private void LoadToolsUI()
    {
        var toolsItemsControl = this.FindName("_toolsItemsControl") as ItemsControl;
        if (toolsItemsControl == null)
            return;

        toolsItemsControl.Items.Clear();

        foreach (var tool in Tools)
        {
            var border = CreateToolCard(tool);
            toolsItemsControl.Items.Add(border);
        }
    }

    private Border CreateToolCard(ToolViewModel tool)
    {
        var border = new Border
        {
            Style = (Style)FindResource("ToolCardButtonStyle"),
            Tag = tool
        };

        border.MouseLeftButtonDown += ToolButton_Click;

        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var icon = new Wpf.Ui.Controls.SymbolIcon
        {
            Symbol = Wpf.Ui.Common.SymbolRegular.Apps24,
            FontSize = 32,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };
        icon.SetResourceReference(Control.ForegroundProperty, "SystemAccentColorBrush");

        stackPanel.Children.Add(icon);

        var nameTextBlock = new TextBlock
        {
            Text = tool.DisplayName,
            FontSize = 12,
            FontWeight = FontWeights.Medium,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 80
        };
        stackPanel.Children.Add(nameTextBlock);

        border.Child = stackPanel;
        return border;
    }

    private string GetCategoryDisplayName(string categoryKey)
    {
        return categoryKey switch
        {
            "CPU_Tools" => "CPU 工具",
            "GPU_Tools" => "显卡工具",
            "Memory_Tools" => "内存工具",
            "Storage_Tools" => "存储工具",
            "Gaming_Tools" => "游戏工具",
            "Display_Tools" => "显示器工具",
            "Peripherals_Tools" => "外设工具",
            "System_Diagnostic_Tools" => "系统诊断工具",
            "Stress_Testing_Tools" => "压力测试工具",
            "Other_Tools" => "其他工具",
            _ => categoryKey
        };
    }

    private void CategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ToolCategoryViewModel category)
        {
            SelectedCategory = category;
        }
    }

    private void ToolButton_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is ToolViewModel tool)
        {
            _toolPlugin.LaunchTool(tool.Name);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class ToolCategoryViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public string CategoryName { get; set; } = "";
    public string CategoryKey { get; set; } = "";
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

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class ToolViewModel : INotifyPropertyChanged
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Version { get; set; } = "";
    public string Author { get; set; } = "";
    public string Command { get; set; } = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
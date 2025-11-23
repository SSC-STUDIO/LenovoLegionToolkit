using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using LenovoLegionToolkit.WPF.Windows;
using SelectedActionViewModel = LenovoLegionToolkit.WPF.Pages.WindowsOptimizationPage.SelectedActionViewModel;

namespace LenovoLegionToolkit.WPF.Windows.Utils
{
    public partial class SelectedActionsWindow : BaseWindow
    {
        private readonly ObservableCollection<SelectedActionViewModel> _selectedActions;

        public ObservableCollection<SelectedActionViewModel> SelectedActions => _selectedActions;
        public string EmptyText { get; }

        public SelectedActionsWindow(ObservableCollection<SelectedActionViewModel> selectedActions, string emptyText)
        {
            InitializeComponent();

            _selectedActions = selectedActions;
            EmptyText = emptyText;

            DataContext = this;

            _selectedActions.CollectionChanged += SelectedActions_CollectionChanged;
            foreach (var action in _selectedActions)
                action.PropertyChanged += Action_PropertyChanged;

            UpdateEmptyState();

            // 禁用鼠标滚轮缩放
            PreviewMouseWheel += OnPreviewMouseWheel;
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 如果按住了 Ctrl 键，禁用缩放功能
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
            }
        }

        private void UpdateEmptyState()
        {
            var hasItems = _selectedActions.Count > 0;
            _actionsList.Visibility = hasItems ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            _emptyTextBlock.Visibility = hasItems ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        private void SelectedActions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (SelectedActionViewModel action in e.OldItems)
                    action.PropertyChanged -= Action_PropertyChanged;
            }

            if (e.NewItems is not null)
            {
                foreach (SelectedActionViewModel action in e.NewItems)
                    action.PropertyChanged += Action_PropertyChanged;
            }

            UpdateEmptyState();
        }

        private void Action_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedActionViewModel.IsSelected))
                UpdateEmptyState();
        }

        private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            PreviewMouseWheel -= OnPreviewMouseWheel;

            base.OnClosed(e);

            _selectedActions.CollectionChanged -= SelectedActions_CollectionChanged;
            foreach (var action in _selectedActions)
                action.PropertyChanged -= Action_PropertyChanged;
        }
    }
}
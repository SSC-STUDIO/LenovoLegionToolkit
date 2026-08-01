using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;

namespace UniversalDeviceToolkit.WPF.Windows.Utils
{
    // TODO(Phase 4b): full portable extraction into UniversalDeviceToolkit.ViewModels
    // is blocked by the WPF-coupled item type SelectedActionViewModel (it touches
    // PackageControl internals). SelectedActionsViewModel stays in the WPF project
    // as the plan's downgrade path until the item model is decoupled.
    public partial class SelectedActionsWindow : BaseWindow
    {
        private readonly SelectedActionsViewModel _viewModel;

        public SelectedActionsWindow(ObservableCollection<SelectedActionViewModel> selectedActions, string emptyText)
        {
            InitializeComponent();

            _viewModel = new SelectedActionsViewModel(selectedActions, emptyText);
            DataContext = _viewModel;

            // Disable mouse wheel zoom
            PreviewMouseWheel += OnPreviewMouseWheel;
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Disable zoom function if Ctrl key is pressed
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
            }
        }

        private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            PreviewMouseWheel -= OnPreviewMouseWheel;
            _viewModel.Dispose();

            base.OnClosed(e);
        }
    }
}

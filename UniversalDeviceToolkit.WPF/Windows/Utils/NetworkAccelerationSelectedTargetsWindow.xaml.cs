using System;
using System.Collections.Generic;
using System.Windows;

namespace UniversalDeviceToolkit.WPF.Windows.Utils;

public partial class NetworkAccelerationSelectedTargetsWindow : BaseWindow
{
    public NetworkAccelerationSelectedTargetsWindow(IReadOnlyList<string> selectedTargets, string emptyText)
    {
        SelectedTargets = selectedTargets ?? throw new ArgumentNullException(nameof(selectedTargets));
        EmptyText = emptyText ?? string.Empty;

        InitializeComponent();
        DataContext = this;
    }

    public IReadOnlyList<string> SelectedTargets { get; }

    public bool HasItems => SelectedTargets.Count > 0;

    public string EmptyText { get; }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}

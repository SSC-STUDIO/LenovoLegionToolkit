using System.Windows;
using System.Windows.Input;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Windows.Utils;

public partial class InputDialogWindow : BaseWindow
{
    private readonly bool _allowEmpty;
    private readonly DebounceDispatcher _debouncer = new();

    public string? InputText { get; private set; }

    public InputDialogWindow(
        string title,
        string? message,
        string? text,
        string? primaryButton,
        string? secondaryButton,
        bool allowEmpty,
        Window? owner = null)
    {
        InitializeComponent();

        Owner = owner;
        Title = title;
        _allowEmpty = allowEmpty;

        _titleTextBlock.Text = title;
        _textBox.PlaceholderText = message ?? string.Empty;
        _textBox.Text = text ?? string.Empty;
        _textBox.SelectionStart = _textBox.Text.Length;
        _textBox.SelectionLength = 0;
        _textBox.Loaded += (_, _) =>
        {
            _textBox.Focus();
            Keyboard.Focus(_textBox);
        };

        _confirmButton.Content = primaryButton;
        _cancelButton.Content = secondaryButton;

        _textBox.TextChanged += (_, _) => _debouncer.Debounce(300, RefreshConfirmButtonState);
        RefreshConfirmButtonState();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        var value = _textBox.Text?.Trim();
        if (!_allowEmpty && string.IsNullOrWhiteSpace(value))
            return;

        InputText = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void RefreshConfirmButtonState() => _confirmButton.IsEnabled = _allowEmpty || !string.IsNullOrWhiteSpace(_textBox.Text);

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ConfirmButton_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            CancelButton_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }
}

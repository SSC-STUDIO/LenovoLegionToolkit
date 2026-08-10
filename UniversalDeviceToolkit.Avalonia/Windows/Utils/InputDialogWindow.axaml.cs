using Avalonia.Input;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Windows.Utils;

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
        _textBox.Watermark = message ?? string.Empty;
        _textBox.Text = text ?? string.Empty;
        _textBox.SelectionStart = _textBox.Text.Length;
        _textBox.CaretIndex = _textBox.Text.Length;
        _textBox.Loaded += (_, _) =>
        {
            _textBox.Focus();
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
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RefreshConfirmButtonState() => _confirmButton.IsEnabled = _allowEmpty || !string.IsNullOrWhiteSpace(_textBox.Text);

    protected override void OnKeyDown(KeyEventArgs e)
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

        base.OnKeyDown(e);
    }
}

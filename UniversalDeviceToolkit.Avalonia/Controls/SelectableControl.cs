using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Controls.Shapes;

namespace UniversalDeviceToolkit.Avalonia.Controls;

public class SelectableControl : UserControl
{
    public class SelectedEventArgs(Func<Control, bool> containsCenter) : EventArgs
    {
        public Func<Control, bool> ContainsCenter { get; } = containsCenter;
    }

    private readonly Grid _grid = new()
    {
        Background = new SolidColorBrush(Colors.Transparent)
    };

    private readonly ContentPresenter _contentPresenter = new();

    private readonly Canvas _canvas = new();

    private readonly Rectangle _selection = new()
    {
        StrokeThickness = 2,
        IsVisible = false
    };

    private bool _mouseDown;
    private Point _mouseDownPosition;

    public Brush Stroke
    {
        get => _selection.Stroke as Brush ?? new SolidColorBrush(Colors.Transparent);
        set => _selection.Stroke = value;
    }

    public Brush Fill
    {
        get => _selection.Fill as Brush ?? new SolidColorBrush(Colors.Transparent);
        set => _selection.Fill = value;
    }

    public event EventHandler<SelectedEventArgs>? Selected;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _contentPresenter.Content = Content;

        _canvas.Children.Add(_selection);

        _grid.Children.Add(_contentPresenter);
        _grid.Children.Add(_canvas);

        _grid.PointerPressed += Grid_OnPointerPressed;
        _grid.PointerMoved += Grid_OnPointerMoved;
        _grid.PointerReleased += Grid_OnPointerReleased;

        Content = _grid;
    }

    private void Grid_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(_grid);
        if (!pointerPoint.Properties.IsLeftButtonPressed)
            return;

        _mouseDown = true;

        // AVALONIA: pointer capture is owned by the pointer, not the input element.
        pointerPoint.Pointer.Capture(_grid);
        _mouseDownPosition = pointerPoint.Position;

        Canvas.SetLeft(_selection, _mouseDownPosition.X);
        Canvas.SetTop(_selection, _mouseDownPosition.Y);

        _selection.Width = 0;
        _selection.Height = 0;

        _selection.IsVisible = true;
    }

    private void Grid_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_mouseDown)
            return;

        var mousePosition = e.GetCurrentPoint(_grid).Position;

        if (_mouseDownPosition.X < mousePosition.X)
        {
            Canvas.SetLeft(_selection, _mouseDownPosition.X);
            _selection.Width = mousePosition.X - _mouseDownPosition.X;
        }
        else
        {
            Canvas.SetLeft(_selection, mousePosition.X);
            _selection.Width = _mouseDownPosition.X - mousePosition.X;
        }

        if (_mouseDownPosition.Y < mousePosition.Y)
        {
            Canvas.SetTop(_selection, _mouseDownPosition.Y);
            _selection.Height = mousePosition.Y - _mouseDownPosition.Y;
        }
        else
        {
            Canvas.SetTop(_selection, mousePosition.Y);
            _selection.Height = _mouseDownPosition.Y - mousePosition.Y;
        }
    }

    private void Grid_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_mouseDown)
            return;

        _mouseDown = false;
        e.GetCurrentPoint(_grid).Pointer.Capture(null);
        _selection.IsVisible = false;

        var mouseUpPosition = e.GetCurrentPoint(_grid).Position;

        var minX = Math.Min(_mouseDownPosition.X, mouseUpPosition.X);
        var minY = Math.Min(_mouseDownPosition.Y, mouseUpPosition.Y);
        var maxX = Math.Max(_mouseDownPosition.X, mouseUpPosition.X);
        var maxY = Math.Max(_mouseDownPosition.Y, mouseUpPosition.Y);

        var rectangle = new Rect(minX, minY, maxX - minX, maxY - minY);

        bool ContainsCenter(Control element)
        {
            var transform = element.TransformToVisual(_grid);
            if (transform is not { } matrix)
                return false;

            var bounds = new Rect(0, 0, element.Bounds.Width, element.Bounds.Height);
            var topLeft = matrix.Transform(bounds.TopLeft);
            var topRight = matrix.Transform(bounds.TopRight);
            var bottomLeft = matrix.Transform(bounds.BottomLeft);
            var bottomRight = matrix.Transform(bounds.BottomRight);

            var elementMinX = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
            var elementMaxX = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
            var elementMinY = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
            var elementMaxY = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));

            var elementCenterX = elementMinX + (elementMaxX - elementMinX) / 2;
            var elementCenterY = elementMinY + (elementMaxY - elementMinY) / 2;

            return rectangle.Contains(new Point(elementCenterX, elementCenterY));
        }

        Selected?.Invoke(this, new SelectedEventArgs(ContainsCenter));
    }
}

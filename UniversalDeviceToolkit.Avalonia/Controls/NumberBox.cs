using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// Defines how the spin buttons used to increment or decrement the
/// <see cref="NumberBox.Value"/> are displayed.
/// </summary>
public enum NumberBoxSpinButtonPlacementMode
{
    Hidden,
    Compact,
    Inline
}

/// <summary>
/// WPF-UI compatible number input box. Derives from <see cref="TextBox"/>, parses
/// <see cref="Text"/> into <see cref="Value"/> and writes <see cref="Value"/> back to
/// <see cref="Text"/> in normalized form.
/// </summary>
public class NumberBox : TextBox
{
    /// <summary>Defines the <see cref="Value"/> property.</summary>
    public static readonly StyledProperty<double?> ValueProperty =
        AvaloniaProperty.Register<NumberBox, double?>(nameof(Value), null, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines the <see cref="MaxDecimalPlaces"/> property.</summary>
    public static readonly StyledProperty<int> MaxDecimalPlacesProperty =
        AvaloniaProperty.Register<NumberBox, int>(nameof(MaxDecimalPlaces), 0);

    /// <summary>Defines the <see cref="SmallChange"/> property.</summary>
    public static readonly StyledProperty<double> SmallChangeProperty =
        AvaloniaProperty.Register<NumberBox, double>(nameof(SmallChange), 1);

    /// <summary>Defines the <see cref="LargeChange"/> property.</summary>
    public static readonly StyledProperty<double> LargeChangeProperty =
        AvaloniaProperty.Register<NumberBox, double>(nameof(LargeChange), 10);

    /// <summary>Defines the <see cref="Maximum"/> property.</summary>
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<NumberBox, double>(nameof(Maximum), 100);

    /// <summary>Defines the <see cref="Minimum"/> property.</summary>
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<NumberBox, double>(nameof(Minimum), 0);

    /// <summary>Defines the <see cref="AcceptsExpression"/> property.</summary>
    public static readonly StyledProperty<bool> AcceptsExpressionProperty =
        AvaloniaProperty.Register<NumberBox, bool>(nameof(AcceptsExpression), false);

    /// <summary>Defines the <see cref="SpinButtonPlacementMode"/> property.</summary>
    public static readonly StyledProperty<NumberBoxSpinButtonPlacementMode> SpinButtonPlacementModeProperty =
        AvaloniaProperty.Register<NumberBox, NumberBoxSpinButtonPlacementMode>(
            nameof(SpinButtonPlacementMode), NumberBoxSpinButtonPlacementMode.Inline);

    /// <summary>Defines the <see cref="ValueChanged"/> routed event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ValueChangedEvent =
        RoutedEvent.Register<NumberBox, RoutedEventArgs>(nameof(ValueChanged), RoutingStrategies.Bubble);

    private bool _syncingFromText;
    private bool _syncingFromValue;

    /// <summary>
    /// Gets or sets the numeric value of the <see cref="NumberBox"/>.
    /// </summary>
    public double? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the number of decimal places used when rounding <see cref="Value"/>.
    /// </summary>
    public int MaxDecimalPlaces
    {
        get => GetValue(MaxDecimalPlacesProperty);
        set => SetValue(MaxDecimalPlacesProperty, value);
    }

    /// <summary>
    /// Gets or sets the value added to or subtracted from <see cref="Value"/> on a small
    /// change (arrow keys).
    /// </summary>
    public double SmallChange
    {
        get => GetValue(SmallChangeProperty);
        set => SetValue(SmallChangeProperty, value);
    }

    /// <summary>
    /// Gets or sets the value added to or subtracted from <see cref="Value"/> on a large
    /// change (PageUp/PageDown keys).
    /// </summary>
    public double LargeChange
    {
        get => GetValue(LargeChangeProperty);
        set => SetValue(LargeChangeProperty, value);
    }

    /// <summary>
    /// Gets or sets the numerical maximum for <see cref="Value"/>.
    /// </summary>
    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>
    /// Gets or sets the numerical minimum for <see cref="Value"/>.
    /// </summary>
    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the control accepts a formulaic expression
    /// as input. Not evaluated; kept for API compatibility.
    /// </summary>
    public bool AcceptsExpression
    {
        get => GetValue(AcceptsExpressionProperty);
        set => SetValue(AcceptsExpressionProperty, value);
    }

    /// <summary>
    /// Gets or sets how the spin buttons are displayed. Visuals are provided by styles;
    /// the value is kept for API compatibility.
    /// </summary>
    public NumberBoxSpinButtonPlacementMode SpinButtonPlacementMode
    {
        get => GetValue(SpinButtonPlacementModeProperty);
        set => SetValue(SpinButtonPlacementModeProperty, value);
    }

    /// <summary>
    /// Occurs after the <see cref="Value"/> changed (live while typing, and finalized when
    /// input is evaluated on Enter or focus loss).
    /// </summary>
    public event EventHandler<RoutedEventArgs>? ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    protected override Type StyleKeyOverride => typeof(global::Avalonia.Controls.TextBox);
    public NumberBox()
    {
        TextChanged += OnTextChangedHandler;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Key.Enter:
                ValidateInput();
                break;
            case Key.Up:
                StepValue(SmallChange);
                e.Handled = true;
                break;
            case Key.Down:
                StepValue(-SmallChange);
                e.Handled = true;
                break;
            case Key.PageUp:
                StepValue(LargeChange);
                e.Handled = true;
                break;
            case Key.PageDown:
                StepValue(-LargeChange);
                e.Handled = true;
                break;
        }
    }

    /// <inheritdoc />
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        ValidateInput();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IconProperty)
        {
            IconHelper.TryConvertStringIcon(this, IconProperty, change.NewValue);
        }
        else if (change.Property == MinimumProperty
                 || change.Property == MaximumProperty
                 || change.Property == MaxDecimalPlacesProperty)
        {
            ValidateInput();
        }
    }

    private void OnValuePropertyChanged(double? oldValue, double? newValue)
    {
        if (!_syncingFromText)
        {
            _syncingFromValue = true;
            try
            {
                SetCurrentValue(TextProperty, FormatValue(ClampAndRound(newValue)));
            }
            finally
            {
                _syncingFromValue = false;
            }
        }

        if (newValue != oldValue)
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
    }

    private void OnTextChangedHandler(object? sender, TextChangedEventArgs e)
    {
        if (_syncingFromValue)
            return;

        if (string.IsNullOrWhiteSpace(Text))
        {
            SetValueFromText(null);
            return;
        }

        if (TryParseNumber(Text, out var parsed))
            SetValueFromText(parsed);
    }

    private void SetValueFromText(double? value)
    {
        _syncingFromText = true;
        try
        {
            SetCurrentValue(ValueProperty, value);
        }
        finally
        {
            _syncingFromText = false;
        }
    }

    private void ValidateInput()
    {
        double? value = null;
        if (TryParseNumber(Text, out var parsed))
            value = ClampAndRound(parsed);

        _syncingFromValue = true;
        try
        {
            SetCurrentValue(TextProperty, FormatValue(value));
            SetCurrentValue(ValueProperty, value);
        }
        finally
        {
            _syncingFromValue = false;
        }
    }

    private void StepValue(double change)
    {
        if (IsReadOnly)
            return;

        var current = Value
                      ?? (Minimum <= 0 && 0 <= Maximum ? 0 : Minimum);

        SetCurrentValue(ValueProperty, ClampAndRound(current + change));
    }

    private double? ClampAndRound(double? value)
    {
        if (value is not double v)
            return null;

        var min = Minimum;
        var max = Maximum;
        var clamped = min <= max ? Math.Clamp(v, min, max) : v;
        return Math.Round(clamped, Math.Max(0, MaxDecimalPlaces));
    }

    private static string FormatValue(double? value)
    {
        return value is double v ? v.ToString(CultureInfo.CurrentCulture) : string.Empty;
    }

    private static bool TryParseNumber(string? text, out double value)
    {
        text = text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            value = 0;
            return false;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            return true;

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
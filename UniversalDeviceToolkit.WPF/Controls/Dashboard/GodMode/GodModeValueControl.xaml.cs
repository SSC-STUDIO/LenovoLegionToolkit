using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using LenovoLegionToolkit.Lib.Extensions;
using UniversalDeviceToolkit.WPF.Extensions;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard.GodMode
{
public partial class GodModeValueControl
{
    private int? _defaultValue;
    private string _automationIdPrefix = string.Empty;
    private string _unit = string.Empty;

    public string Title
    {
        get => _titleTextBlock.Text;
        set
        {
            _titleTextBlock.Text = value ?? string.Empty;
            UpdateAutomationMetadata();
        }
    }

    public string AutomationIdPrefix
    {
        get => _automationIdPrefix;
        set
        {
            _automationIdPrefix = value ?? string.Empty;
            UpdateAutomationMetadata();
        }
    }

    public string Description
    {
        get => _descriptionTextBlock.Text;
        set
        {
            var text = value ?? string.Empty;
            _descriptionTextBlock.Text = text;
            _descriptionTextBlock.Visibility = string.IsNullOrWhiteSpace(text)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    public string Unit
    {
        get => _unit;
        set
        {
            _unit = value ?? string.Empty;
            RefreshSliderLabel();
        }
    }

    public int Value
    {
        get
        {
            if (_slider.Visibility == Visibility.Visible)
                return (int)_slider.Value;

            if (_comboBox.Visibility == Visibility.Visible && _comboBox.TryGetSelectedItem(out int value))
                return value;

            throw new InvalidOperationException("Unable to get Value");
        }
        set
        {
            if (_slider.Visibility == Visibility.Visible)
            {
                double newValue = value;

                if (_defaultValue.HasValue && (newValue < _slider.Minimum || newValue > _slider.Maximum))
                    newValue = _defaultValue.Value;

                newValue = Math.Clamp(MathExtensions.RoundNearest((int)newValue, (int)_slider.TickFrequency), _slider.Minimum, _slider.Maximum);
                _slider.Value = newValue;
                RefreshSliderLabel();
                return;
            }

            if (_comboBox.Visibility == Visibility.Visible)
            {
                var newValue = value;
                var items = _comboBox.GetItems<int>().ToArray();
                if (!items.Contains(newValue))
                {
                    var valueTemp = newValue;
                    newValue = items.MinBy(v => Math.Abs((long)v - valueTemp));
                }

                _comboBox.SelectItem(newValue);
                return;
            }

            throw new InvalidOperationException("Unable to set Value");
        }
    }

    public event RoutedPropertyChangedEventHandler<double> ValueChanged
    {
        add => _slider.ValueChanged += value;
        remove => _slider.ValueChanged -= value;
    }

    public GodModeValueControl()
    {
        InitializeComponent();
        _slider.ValueChanged += (_, _) => RefreshSliderLabel();
        UpdateAutomationMetadata();
    }

    public void Set(StepperValue? stepperValue)
    {
        if (!stepperValue.HasValue)
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        var value = stepperValue.Value;

        if (value.Steps.Length > 0)
        {
            _slider.Visibility = Visibility.Collapsed;
            _sliderLabel.Visibility = Visibility.Collapsed;
            _comboBox.Visibility = Visibility.Visible;

            _slider.Minimum = 0;
            _slider.Maximum = 0;
            _slider.TickFrequency = 0;
            _slider.Value = 0;

            _comboBox.SetItems(value.Steps, value.Value, v => string.IsNullOrEmpty(Unit) ? $"{v}" : $"{v} {Unit}");

            _defaultValue = value.DefaultValue;
            _resetToDefaultButton.Visibility = _defaultValue.HasValue ? Visibility.Visible : Visibility.Collapsed;

            Visibility = Visibility.Visible;
            return;
        }

        if (value.Step > 0)
        {
            _slider.Visibility = Visibility.Visible;
            _sliderLabel.Visibility = Visibility.Visible;
            _comboBox.Visibility = Visibility.Collapsed;

            _slider.Minimum = value.Min;
            _slider.Maximum = value.Max;
            _slider.TickFrequency = value.Step;
            _slider.Value = value.Value;
            RefreshSliderLabel();

            _comboBox.Items.Clear();
            _comboBox.SelectedItem = null;

            _defaultValue = value.DefaultValue;
            _resetToDefaultButton.Visibility = _defaultValue.HasValue ? Visibility.Visible : Visibility.Collapsed;

            Visibility = Visibility.Visible;
            return;
        }

        Visibility = Visibility.Collapsed;
    }

    private void RefreshSliderLabel()
    {
        if (_sliderLabel is null)
            return;

        var text = string.IsNullOrEmpty(Unit)
            ? $"{_slider.Value:0}"
            : $"{_slider.Value:0} {Unit}";
        _sliderLabel.Text = text;
    }

    private void ResetToDefaultButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_defaultValue.HasValue)
            return;

        if (_slider.Visibility == Visibility.Visible)
            _slider.Value = _defaultValue.Value;

        if (_comboBox.Visibility == Visibility.Visible)
            _comboBox.SelectItem(_defaultValue.Value);
    }

    private void UpdateAutomationMetadata()
    {
        var name = _titleTextBlock?.Text ?? string.Empty;
        if (_slider is not null)
            AutomationProperties.SetName(_slider, name);
        if (_comboBox is not null)
            AutomationProperties.SetName(_comboBox, name);
        if (_resetToDefaultButton is not null)
            AutomationProperties.SetName(_resetToDefaultButton, name);

        if (string.IsNullOrWhiteSpace(_automationIdPrefix))
            return;

        if (_slider is not null)
            AutomationProperties.SetAutomationId(_slider, $"{_automationIdPrefix}Slider");
        if (_comboBox is not null)
            AutomationProperties.SetAutomationId(_comboBox, $"{_automationIdPrefix}ComboBox");
        if (_resetToDefaultButton is not null)
            AutomationProperties.SetAutomationId(_resetToDefaultButton, $"{_automationIdPrefix}ResetButton");
    }
}
}

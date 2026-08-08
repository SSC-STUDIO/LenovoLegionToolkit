using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;

namespace UniversalDeviceToolkit.WPF.Controls.Custom;

public class CardExpander : Wpf.Ui.Controls.CardExpander
{
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("ExpanderToggleButton") is FrameworkElement toggle)
        {
            var name = AutomationProperties.GetName(this);
            if (string.IsNullOrWhiteSpace(name) && Header is DependencyObject header)
                name = AutomationProperties.GetName(header);

            AutomationProperties.SetAutomationId(toggle, "ExpanderToggleButton");
            AutomationProperties.SetName(toggle, string.IsNullOrWhiteSpace(name) ? "Expand or collapse" : name);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new CardExpanderAutomationPeer(this);

    private class CardExpanderAutomationPeer(CardExpander owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(CardExpander);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ItemContainer)
                return this;

            return base.GetPattern(patternInterface);
        }

        protected override AutomationPeer? GetLabeledByCore()
        {
            if (owner.Header is UIElement element)
                return CreatePeerForElement(element);

            return base.GetLabeledByCore();
        }

        protected override string GetNameCore()
        {
            var result = base.GetNameCore() ?? string.Empty;

            if (result == string.Empty)
                result = AutomationProperties.GetName(owner);

            if (result == string.Empty && owner.Header is DependencyObject d)
                result = AutomationProperties.GetName(d);

            if (result == string.Empty && owner.Header is string s)
                result = s;

            return result;
        }
    }
}

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Controls.Custom;

public class CardExpander : Wpf.Ui.Controls.CardExpander
{
    /// <summary>Glyph for the card header; maps to WPF-UI <see cref="Wpf.Ui.Controls.CardExpander.Icon"/> as <see cref="SymbolIcon"/>.</summary>
    public new SymbolRegular Icon
    {
        get => base.Icon is SymbolIcon si ? si.Symbol : SymbolRegular.Empty;
        set => base.Icon = value == SymbolRegular.Empty ? null : new SymbolIcon { Symbol = value };
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

using System;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace LenovoLegionToolkit.WPF.Controls.Custom;

public class NavigationItem : Wpf.Ui.Controls.NavigationItem
{
    private EventHandler? _invoked;

    public event EventHandler Invoked
    {
        add => _invoked += value;
        remove => _invoked -= value;
    }

    internal bool HasInvokeHandler => _invoked is not null;

    internal void InvokeFromAutomation()
    {
        if (!IsEnabled)
            throw new ElementNotEnabledException();

        _invoked?.Invoke(this, EventArgs.Empty);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NavigationItemAutomationPeer(this);

    private class NavigationItemAutomationPeer(NavigationItem owner) : FrameworkElementAutomationPeer(owner), IInvokeProvider
    {
        protected override string GetClassNameCore() => nameof(NavigationItem);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ItemContainer)
                return this;

            if (patternInterface == PatternInterface.Invoke && owner.HasInvokeHandler)
                return this;

            return base.GetPattern(patternInterface);
        }

        public void Invoke()
        {
            if (owner.Dispatcher.CheckAccess())
            {
                owner.InvokeFromAutomation();
                return;
            }

            owner.Dispatcher.Invoke(owner.InvokeFromAutomation);
        }

        protected override string GetNameCore()
        {
            var result = base.GetNameCore() ?? string.Empty;

            if (result == string.Empty)
                result = AutomationProperties.GetName(owner);

            return result;
        }
    }
}

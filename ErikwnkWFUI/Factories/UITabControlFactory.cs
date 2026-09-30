using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UITabControlFactory
    {
        // The editable variant: right-click a tab to add, rename or close tabs.
        public static TabControl CreateStandard()
        {
            return new TabControl();
        }

        // The display-only variant: tabs can be switched, not edited.
        public static ReadOnlyTabControl CreateReadOnlyStandard()
        {
            return new ReadOnlyTabControl();
        }

        // Same controls, with the selected tab's bar (and the menu's
        // selection) in the current accent instead of the neutral gray the
        // Standard ones keep - mirrors UIDataGridViewFactory.CreatePrimary.
        public static TabControl CreatePrimary()
        {
            return new TabControl
            {
                SelectedTabIndicatorColor = UIColors.Primary,
                ContextMenuSelectionColor = UIColors.Primary
            };
        }

        public static ReadOnlyTabControl CreateReadOnlyPrimary()
        {
            return new ReadOnlyTabControl
            {
                SelectedTabIndicatorColor = UIColors.Primary
            };
        }
    }
}

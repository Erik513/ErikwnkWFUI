using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UITabControlFactory
    {
        // The editable variant: right-click a tab to add, rename or close tabs.
        public static TabControl CreateStandard(UIColorSet colors = null)
        {
            TabControl tabs = new TabControl();
            ApplyColors(tabs, colors);

            return tabs;
        }

        // The display-only variant: tabs can be switched, not edited.
        public static ReadOnlyTabControl CreateReadOnlyStandard(UIColorSet colors = null)
        {
            ReadOnlyTabControl tabs = new ReadOnlyTabControl();
            ApplyColors(tabs, colors);

            return tabs;
        }

        // A color set replaces the neutral gray where the tab control shows its
        // "accent": the bar under the selected tab (the set's pressed shade - a thin
        // line needs more contrast than a filled selection does) and the selection of
        // the right-click menu (the set's own color). The tab fills and the text stay
        // those of the theme.
        private static void ApplyColors(ReadOnlyTabControl tabs, UIColorSet colors)
        {
            if (colors == null)
                return;

            tabs.SelectedTabIndicatorColor = colors.Pressed;

            TabControl editable = tabs as TabControl;

            if (editable != null)
                editable.ContextMenuSelectionColor = colors.Hover;
        }

        // The Standard ones in the current accent - the primary color set.
        public static TabControl CreatePrimary()
        {
            return CreateStandard(UIColors.PrimarySet);
        }

        public static ReadOnlyTabControl CreateReadOnlyPrimary()
        {
            return CreateReadOnlyStandard(UIColors.PrimarySet);
        }
    }
}

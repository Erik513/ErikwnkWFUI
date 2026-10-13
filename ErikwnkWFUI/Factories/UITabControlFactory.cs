using System;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UITabControlFactory
    {
        // The editable variant: right-click a tab to add, rename or close tabs.
        public static TabControl CreateStandard(UIColorSet colors = null)
        {
            return Standard(ColorSetSource.Of(colors));
        }

        // The display-only variant: tabs can be switched, not edited.
        public static ReadOnlyTabControl CreateReadOnlyStandard(UIColorSet colors = null)
        {
            return ReadOnlyStandard(ColorSetSource.Of(colors));
        }

        private static TabControl Standard(Func<UIColorSet> colors)
        {
            TabControl tabs = new TabControl();
            ApplyColors(tabs, colors);

            return tabs;
        }

        private static ReadOnlyTabControl ReadOnlyStandard(Func<UIColorSet> colors)
        {
            ReadOnlyTabControl tabs = new ReadOnlyTabControl();
            ApplyColors(tabs, colors);

            return tabs;
        }

        // A color set replaces the neutral gray where the tab control shows its
        // "accent": the bar under the selected tab (the set's pressed shade - a thin
        // line needs more contrast than a filled selection does) and the selection of
        // the right-click menu (the set's own color). The tab fills and the text stay
        // those of the theme. The set is asked for again whenever the theme or the
        // accent changes (the primary set follows the accent).
        private static void ApplyColors(ReadOnlyTabControl tabs, Func<UIColorSet> colors)
        {
            if (colors == null)
                return;

            tabs.FollowColors(colors);
        }

        // The Standard ones in the current accent - the primary color set.
        public static TabControl CreatePrimary()
        {
            return Standard(() => UIColors.PrimarySet);
        }

        public static ReadOnlyTabControl CreateReadOnlyPrimary()
        {
            return ReadOnlyStandard(() => UIColors.PrimarySet);
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIFlowLayoutPanelFactory
    {
        // The elevated surface - or, with a color set, the basic color of the set.
        public static FlowLayoutPanel CreateStandard(UIColorSet colors = null)
        {
            return CreateFlowLayoutPanel(
                colors == null ? (Func<Color>)(() => UIColors.BackgroundMediumElevated) : () => colors.Basic,
                true);
        }

        public static FlowLayoutPanel CreatePrimary()
        {
            return CreateFlowLayoutPanel(() => UIColors.PrimaryDarkDark, true);
        }

        // Nothing is seen of the size of a transparent one, and the programs that use it for a row of buttons are laid out with the
        // size it has always had (at least that of a panel, 200 x 100): it does not shrink.
        public static FlowLayoutPanel CreateTransparent()
        {
            return CreateFlowLayoutPanel(() => Color.Transparent, false);
        }

        private static FlowLayoutPanel CreateFlowLayoutPanel(Func<Color> backColor, bool hugContent)
        {
            FlowLayoutPanel panel = new ThemedFlowLayoutPanel
            {
                AutoSize = true,
                // With a background the panel is as big as its content (the standard mode only grows: it would stay 200 x 100).
                AutoSizeMode = hugContent ? AutoSizeMode.GrowAndShrink : AutoSizeMode.GrowOnly,
                WrapContents = false,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            ThemeWatcher.FollowBackColor(panel, backColor);

            return panel;
        }
    }
}

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
                colors == null ? (Func<Color>)(() => UIColors.BackgroundMediumElevated) : () => colors.Basic);
        }

        public static FlowLayoutPanel CreateTransparent()
        {
            return CreateFlowLayoutPanel(() => Color.Transparent);
        }

        private static FlowLayoutPanel CreateFlowLayoutPanel(Func<Color> backColor)
        {
            FlowLayoutPanel panel = new ThemedFlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            ThemeWatcher.FollowBackColor(panel, backColor);

            return panel;
        }
    }
}

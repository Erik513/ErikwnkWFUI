using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIFlowLayoutPanelFactory
    {
        // Transparent - or, with a color set, in the basic color of the set.
        public static FlowLayoutPanel CreateStandard(UIColorSet colors = null)
        {
            FlowLayoutPanel panel = new ThemedFlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            if (colors != null)
                ThemeWatcher.FollowBackColor(panel, () => colors.Basic);

            return panel;
        }
    }
}

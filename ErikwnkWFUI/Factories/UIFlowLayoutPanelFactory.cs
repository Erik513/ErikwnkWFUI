using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Factories
{
    internal static class UIFlowLayoutPanelFactory
    {
        public static FlowLayoutPanel CreateStandard()
        {
            FlowLayoutPanel panel = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            // (The scroll bars of a panel with AutoScroll are the system's own.)
            NativeControlTheme.Track(panel);

            return panel;
        }
    }
}

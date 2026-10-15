using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Factories
{
    internal static class UIFlowLayoutPanelFactory
    {
        public static FlowLayoutPanel CreateStandard()
        {
            FlowLayoutPanel panel = new ThemedFlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            return panel;
        }
    }
}

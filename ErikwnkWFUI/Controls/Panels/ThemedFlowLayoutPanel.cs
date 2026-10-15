using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The flow layout panel of <c>UIStyles.FlowLayoutPanels</c>: the standard <see cref="FlowLayoutPanel"/>, drawn without
    /// flicker and with the scroll bars in the style of the theme. Everything else is the standard control. Named
    /// <c>ThemedFlowLayoutPanel</c> so that it does not clash with the standard class in the files that use both namespaces.
    /// </summary>
    public class ThemedFlowLayoutPanel : FlowLayoutPanel
    {
        public ThemedFlowLayoutPanel()
        {
            DoubleBuffered = true;
            NativeControlTheme.Track(this);
        }
    }
}

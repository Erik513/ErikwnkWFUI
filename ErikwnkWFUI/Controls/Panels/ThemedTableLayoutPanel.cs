using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The table layout panel of <c>UIStyles.TableLayoutPanels</c>: the standard <see cref="TableLayoutPanel"/>, drawn without
    /// flicker and with the scroll bars in the style of the theme. Everything else is the standard control. Named
    /// <c>ThemedTableLayoutPanel</c> so that it does not clash with the standard class in the files that use both namespaces.
    /// </summary>
    public class ThemedTableLayoutPanel : TableLayoutPanel
    {
        public ThemedTableLayoutPanel()
        {
            DoubleBuffered = true;
            NativeControlTheme.Track(this);
        }
    }
}

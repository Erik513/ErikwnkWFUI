using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The flow layout panel of <c>UIStyles.FlowLayoutPanels</c>: the standard <see cref="FlowLayoutPanel"/>, drawn without
    /// flicker, with the scroll bars in the style of the theme and with an optional frame in the library's color
    /// (<see cref="Panel.BorderStyle"/>, <see cref="BorderColor"/>). Everything else is the standard control. Named
    /// <c>ThemedFlowLayoutPanel</c> so that it does not clash with the standard class in the files that use both namespaces.
    /// </summary>
    public class ThemedFlowLayoutPanel : FlowLayoutPanel
    {
        private readonly PanelFrame _frame;

        /// <summary>
        /// The color of the frame when <see cref="Panel.BorderStyle"/> is <c>FixedSingle</c> (the system draws it in its own
        /// color, which is painted over; of <c>Fixed3D</c> only the outer line). Follows the theme (and, on a color, is moved until it is seen against it) until it is set;
        /// <see cref="Color.Empty"/> gives that back. There is no frame by default.
        /// </summary>
        public Color BorderColor
        {
            get { return _frame.Color; }
            set { _frame.Color = value; }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            _frame.AfterWindowMessage(ref m);
        }

        public ThemedFlowLayoutPanel()
        {
            _frame = new PanelFrame(this);
            DoubleBuffered = true;
            NativeControlTheme.Track(this);
        }
    }
}

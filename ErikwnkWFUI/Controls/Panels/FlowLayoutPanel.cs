using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The flow layout panel of <c>UIStyles.FlowLayoutPanels</c>: the standard <see cref="FlowLayoutPanel"/>, drawn without
    /// flicker, with the scroll bars in the style of the theme and with an optional frame in the library's color
    /// (<see cref="System.Windows.Forms.Panel.BorderStyle"/>, <see cref="BorderColor"/>). Everything else is the standard control. It has the
    /// name of the standard class: in a file that uses both namespaces name it in full or with a <c>using</c> alias; the factories hand it back as a
    /// standard <see cref="System.Windows.Forms.FlowLayoutPanel"/>.
    /// </summary>
    public class FlowLayoutPanel : System.Windows.Forms.FlowLayoutPanel
    {
        private readonly PanelFrame _frame;

        /// <summary>
        /// The color of the frame when <see cref="System.Windows.Forms.Panel.BorderStyle"/> is <c>FixedSingle</c> (the system draws it in its own
        /// color, which is painted over; of <c>Fixed3D</c> only the outer line). Follows the theme (and, on a color, is moved until it is seen against it) until it is set;
        /// <see cref="Color.Empty"/> gives that back. There is no frame by default.
        /// </summary>
        public Color BorderColor
        {
            get { return _frame.Color; }
            set { _frame.Color = value; }
        }

        // The designer writes the color only where it was set (the default is followed, not stored).
        private bool ShouldSerializeBorderColor()
        {
            return _frame.IsSet;
        }

        private void ResetBorderColor()
        {
            BorderColor = Color.Empty;
        }

        /// <inheritdoc/>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            _frame.AfterWindowMessage(ref m);
        }

        /// <summary>Makes a panel without a frame, drawn without flicker; <c>UIStyles.FlowLayoutPanels</c> makes the usual ones.</summary>
        public FlowLayoutPanel()
        {
            _frame = new PanelFrame(this);
            DoubleBuffered = true;
            NativeControlTheme.Track(this);
        }
    }
}

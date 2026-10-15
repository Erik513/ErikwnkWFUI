using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The flow layout panel of <c>UIStyles.FlowLayoutPanels</c>: the standard <see cref="FlowLayoutPanel"/>, drawn without
    /// flicker, with the scroll bars in the style of the theme and with an optional frame in the library's color
    /// (<see cref="ShowBorder"/>, <see cref="BorderColor"/>). Everything else is the standard control. Named
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

        /// <summary>
        /// Whether the panel has a frame in <see cref="BorderColor"/> - a shortcut for <see cref="Panel.BorderStyle"/>
        /// <c>FixedSingle</c> (on) and <c>None</c> (off, the default). The frame takes one pixel from each side of the area inside.
        /// </summary>
        public bool ShowBorder
        {
            get { return BorderStyle != BorderStyle.None; }
            set
            {
                if (value != ShowBorder)
                    BorderStyle = value ? BorderStyle.FixedSingle : BorderStyle.None;
            }
        }

        // Where both scroll bars meet, the system leaves a light square even with dark bars.
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_NCPAINT)
                NativeControlTheme.PaintScrollBarCorner(this);
        }

        public ThemedFlowLayoutPanel()
        {
            _frame = new PanelFrame(this);
            DoubleBuffered = true;
            NativeControlTheme.Track(this);
        }
    }
}

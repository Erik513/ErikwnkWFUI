using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The panel of <c>UIStyles.Panels</c>: the standard <see cref="Panel"/>, drawn without flicker, with the scroll bars in the
    /// style of the theme and with an optional frame in the library's color (<see cref="System.Windows.Forms.Panel.BorderStyle"/>, <see cref="BorderColor"/>).
    /// Everything else - <c>AutoScroll</c>, <c>AutoSize</c>, <c>BackgroundImage</c>, <c>Padding</c> - is the standard control. It has the name of the standard class: in a file that uses both namespaces name it in full
    /// or with a <c>using</c> alias; the factories hand it back as a standard <see cref="System.Windows.Forms.Panel"/>.
    /// </summary>
    public class Panel : System.Windows.Forms.Panel
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

        /// <summary>Makes a panel without a frame, drawn without flicker; <c>UIStyles.Panels</c> makes the usual ones.</summary>
        public Panel()
        {
            _frame = new PanelFrame(this);
            // The panel is repainted as a whole instead of in strips while it is resized (the standard panel flickers).
            DoubleBuffered = true;

            // The scroll bars of a panel with AutoScroll are the system's own.
            NativeControlTheme.Track(this);
        }
    }
}

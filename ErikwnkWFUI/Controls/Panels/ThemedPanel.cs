using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The panel of <c>UIStyles.Panels</c>: the standard <see cref="Panel"/>, drawn without flicker, with the scroll bars in the
    /// style of the theme and with an optional frame in the library's color (<see cref="ShowBorder"/>, <see cref="BorderColor"/>).
    /// Everything else - <c>AutoScroll</c>, <c>AutoSize</c>, <c>BackgroundImage</c>, <c>Padding</c> - is the standard control. Named <c>ThemedPanel</c> (not <c>Panel</c>) so that it does not clash with the standard class in
    /// the files that use both namespaces; the factories hand it back as a <see cref="Panel"/>.
    /// </summary>
    public class ThemedPanel : Panel
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
            get { return _frame.ShowBorder; }
            set { _frame.ShowBorder = value; }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            _frame.AfterWindowMessage(ref m);
        }

        public ThemedPanel()
        {
            _frame = new PanelFrame(this);
            // The panel is repainted as a whole instead of in strips while it is resized (the standard panel flickers).
            DoubleBuffered = true;

            // The scroll bars of a panel with AutoScroll are the system's own.
            NativeControlTheme.Track(this);
        }
    }
}

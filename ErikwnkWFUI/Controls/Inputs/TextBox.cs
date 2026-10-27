using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The text box of the library: the standard <see cref="System.Windows.Forms.TextBox"/> with the frame of the library (a quiet outline, and with the
    /// focus the color of <see cref="Colors"/>) instead of the system's. Make one with <c>UIStyles.TextBoxes</c>; the factories give the
    /// standard type. The class is not public on purpose: a public <c>TextBox</c> in <c>ErikwnkWFUI.Controls</c> would make the name ambiguous
    /// in every file that uses both namespaces. The color is set through the factory (<c>colors:</c>, <c>CreatePrimary</c>).
    /// </summary>
    internal class TextBox : System.Windows.Forms.TextBox
    {
        private readonly ColorSetHolder _colorSet = new ColorSetHolder();
        private readonly Action _redrawFrame;
        private string _placeholder = "";

        public TextBox()
        {
            // The border of BorderStyle.FixedSingle is part of the window outside its client area, in the system's gray: the library paints
            // its own frame over it - a quiet outline, with the focus the color of the set - and moves the line to the middle of the box.
            _redrawFrame = InputField.AttachFrame(
                this,
                () => BorderStyle == BorderStyle.FixedSingle,
                () => InputField.GetFrameColor(this, Colors, Focused),
                () => InputField.GetTextTopInset(this));

            // The placeholder is painted, not written into the box: it is not the Text, not masked by a PasswordChar, not cut by MaxLength,
            // and no TextChanged comes when it appears or goes.
            ThemeWatcher.Attach(this, Invalidate);
        }

        /// <summary>
        /// A hint that is shown in the muted color while the box is empty and has not the focus (as the placeholder of a web page); it is
        /// painted over the box and never is its <see cref="TextBoxBase.Text"/>. Empty (the default) is none.
        /// </summary>
        public string Placeholder
        {
            get { return _placeholder; }
            set
            {
                _placeholder = value ?? "";
                Invalidate();
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_PAINT)
            {
                using (Graphics graphics = CreateGraphics())
                    PaintPlaceholder(graphics);
            }
            else if (m.Msg == WM_PRINT && ((long)m.LParam & PRF_CLIENT) != 0)
            {
                // (What DrawToBitmap and printing ask for: the client area is painted onto the device context that comes with the message.)
                using (Graphics graphics = Graphics.FromHdc(m.WParam))
                    PaintPlaceholder(graphics);
            }
        }

        private void PaintPlaceholder(Graphics graphics)
        {
            if (_placeholder.Length == 0 || TextLength > 0 || Focused || !IsHandleCreated)
                return;

            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding
                | (Multiline ? TextFormatFlags.WordBreak : TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

            TextRenderer.DrawText(graphics, _placeholder, Font, GetPlaceholderArea(), UIColors.TextMuted, flags);
        }

        // Where the system draws the text of the box: its formatting rectangle (the single line is also moved down by the inset of the
        // frame), so that the placeholder stands exactly where a text would.
        private Rectangle GetPlaceholderArea()
        {
            RECT format = new RECT();
            SendMessage(Handle, EM_GETRECT, IntPtr.Zero, ref format);

            int left = format.Left - (Multiline ? 0 : SingleLineLeftOffset);
            int top = format.Top + (Multiline ? 0 : InputField.GetTextTopInset(this));
            Rectangle client = ClientRectangle;

            return new Rectangle(left, top, Math.Max(1, client.Width - left), Math.Max(1, client.Height - top));
        }

        // (The line of a single-line box is drawn two pixels left of where the formatting rectangle says; a multiline box draws at it.)
        private const int SingleLineLeftOffset = 2;
        private const int EM_GETRECT = 0x00B2;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, ref RECT rect);

        /// <summary>
        /// The color of the frame while the box has the focus: the <c>Hover</c> color of the set is the main color (brought to where it is
        /// seen against the field). Null (the default) is the text color of the field; <c>UIStyles.TextBoxes.CreatePrimary</c> makes one that
        /// follows the accent; <c>CreateStandard(colors: ...)</c> takes a color set or simply a color. The fill and the text stay those
        /// of the theme. Not written by the designer (a set is code).
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIColorSet Colors
        {
            get { return _colorSet.Value; }
            set
            {
                _colorSet.Value = value;
                _redrawFrame();
            }
        }

        // A set that is asked for again each time it is needed (the primary set, which follows the accent); a later Colors replaces it.
        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorSet.Follow(source);
            _redrawFrame();
        }
    }
}

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The text box of the library: the standard <see cref="TextBox"/> with the frame of the library (a quiet outline, and with the focus
    /// the color of <see cref="Colors"/>) instead of the system's, a placeholder that is painted and never is the text, a slightly dimmed
    /// background while it is read-only, and a tooltip with the whole text when the text is wider than the box (<see cref="ToolTipWhenCut"/>).
    /// Make one with <c>UIStyles.TextBoxes</c>; the factories give the standard type, except
    /// <c>CreateLockable</c>, which gives a <see cref="LockableTextBox"/>. The box can have a button at its right end, inside the frame
    /// (the pencil of a <see cref="LockableTextBox"/>, the eye of a password box): it is painted by the box, so it is not a control of its
    /// own and not seen by a screen reader - what it does can also be done with the keyboard (see <see cref="LockableTextBox"/>). The class has its own name, as
    /// <c>ErikwnkWFUI.Controls.TextBox</c> would be ambiguous in every file that uses both namespaces.
    /// </summary>
    public partial class ThemedTextBox : TextBox
    {
        private readonly ColorSetHolder _colorSet = new ColorSetHolder();
        private readonly Action _redrawFrame;
        private string _placeholder = "";
        private bool _toolTipWhenCut = true;
        private ToolTip _toolTip;

        public ThemedTextBox()
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

            _toolTip = UIToolTipFactory.CreateHoverToolTip(this);
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

        /// <summary>
        /// Whether the whole text is shown as a tooltip while the mouse is over the box and the text is wider than the box (true by
        /// default). Not while the box hides what is typed (a password that is not shown), and not for a multi-line box, which shows its text.
        /// </summary>
        [DefaultValue(true)]
        public bool ToolTipWhenCut
        {
            get { return _toolTipWhenCut; }
            set
            {
                _toolTipWhenCut = value;
                UpdateToolTip(false);
            }
        }

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
                Invalidate();
            }
        }

        // A set that is asked for again each time it is needed (the primary set, which follows the accent); a later Colors replaces it.
        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorSet.Follow(source);
            _redrawFrame();
            Invalidate();
        }

        // A box that hides what is typed shows nothing of it, not even in a tooltip.
        private bool HidesText
        {
            get { return UseSystemPasswordChar || PasswordChar != '\0'; }
        }

        // The text is wider than the place that the system gives it.
        private bool IsTextCut()
        {
            // (The handle first: reading the PasswordChar of a box that has none makes one.)
            if (!IsHandleCreated || !_toolTipWhenCut || Multiline || TextLength == 0 || HidesText)
                return false;

            Rectangle area = GetTextArea();
            int width = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;

            return width > area.Width;
        }

        // What the tooltip of the box says at the moment: the text of the button in the box while the mouse is over it, else the whole text
        // when it is cut, else nothing.
        private void UpdateToolTip(bool overInnerButton)
        {
            if (_toolTip == null)
                return;

            string text = overInnerButton && _innerToolTip.Length > 0 ? _innerToolTip : IsTextCut() ? Text : string.Empty;

            if (_toolTip.GetToolTip(this) != text)
                _toolTip.SetToolTip(this, text);
        }

        // For a box whose way of hiding the text has changed (the password that is shown or hidden).
        internal void RefreshToolTip()
        {
            UpdateToolTip(_innerHot);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
            UpdateToolTip(_innerHot);
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

            if (!Enabled)
                ResetInnerState();

            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            ApplyInnerMargin();
            UpdateToolTip(_innerHot);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            ApplyInnerMargin();
            UpdateToolTip(_innerHot);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyInnerMargin();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_toolTip != null)
                {
                    _toolTip.Dispose();
                    _toolTip = null;
                }

                if (_innerFont != null)
                {
                    _innerFont.Dispose();
                    _innerFont = null;
                }
            }

            base.Dispose(disposing);
        }

        // ---- painting over what the system painted, the formatting rectangle of the text ----

        protected override void WndProc(ref Message m)
        {
            if (HandleInnerButtonMessage(ref m))
                return;

            base.WndProc(ref m);

            if (m.Msg == WM_MOUSEMOVE)
                UpdateInnerHot(GetPoint(m.LParam));

            if (m.Msg == WM_PAINT)
            {
                using (Graphics graphics = CreateGraphics())
                    PaintOver(graphics);
            }
            else if (m.Msg == WM_PRINT && ((long)m.LParam & PRF_CLIENT) != 0)
            {
                // (What DrawToBitmap and printing ask for: the client area is painted onto the device context that comes with the message.)
                using (Graphics graphics = Graphics.FromHdc(m.WParam))
                    PaintOver(graphics);
            }
        }

        // What is painted over what the system painted: the placeholder, and the button in the box.
        private void PaintOver(Graphics graphics)
        {
            PaintPlaceholder(graphics);
            PaintInnerButton(graphics);
        }

        private void PaintPlaceholder(Graphics graphics)
        {
            if (_placeholder.Length == 0 || TextLength > 0 || Focused || !IsHandleCreated)
                return;

            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding
                | (Multiline ? TextFormatFlags.WordBreak : TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

            // Quieter than a text, but readable: at least 4.5:1 on the field (the muted text color of the theme was 2.9:1 on the dark one).
            TextRenderer.DrawText(graphics, _placeholder, Font, GetPlaceholderArea(), UIColors.GetMutedText(BackColor, 0.45, 4.5), flags);
        }

        // Where the system draws the text of the box: its formatting rectangle.
        private Rectangle GetTextArea()
        {
            RECT format = new RECT();
            SendMessage(Handle, EM_GETRECT, IntPtr.Zero, ref format);

            return new Rectangle(format.Left, format.Top, Math.Max(1, format.Right - format.Left), Math.Max(1, format.Bottom - format.Top));
        }

        // Where a text would stand: the formatting rectangle (the single line is also moved down by the inset of the frame), so that the
        // placeholder stands exactly where a text would - and not under the button in the box.
        private Rectangle GetPlaceholderArea()
        {
            RECT format = new RECT();
            SendMessage(Handle, EM_GETRECT, IntPtr.Zero, ref format);

            int left = format.Left - (Multiline ? 0 : SingleLineLeftOffset);
            int top = format.Top + (Multiline ? 0 : InputField.GetTextTopInset(this));
            Rectangle client = ClientRectangle;

            return new Rectangle(left, top, Math.Max(1, client.Width - InnerWidth - left), Math.Max(1, client.Height - top));
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

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
    }
}

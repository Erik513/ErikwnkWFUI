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
    public partial class TextField : TextBox
    {
        private readonly ColorSetHolder _colorSet = new ColorSetHolder();

        private readonly Action _redrawFrame;

        public TextField()
        {
            // The border of BorderStyle.FixedSingle is part of the window outside its client area, in the system's gray: the library paints
            // its own frame over it - a quiet outline, with the focus the color of the set - and moves the line to the middle of the box.
            _redrawFrame = InputField.AttachFrame(
                this,
                () => BorderStyle == BorderStyle.FixedSingle,
                () => InputField.GetFrameColor(this, Colors, Focused),
                () => InputField.GetTextTopInset(this));

            _toolTip = UIToolTipFactory.CreateHoverToolTip(this);
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

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
            UpdateToolTip(_innerHot);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            StartWatchingClicks();
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            StopWatchingClicks();
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

            // (The text and the width are there before the window is: the first look at them had no window to measure in.)
            UpdateToolTip(_innerHot);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_clickWatcher != null)
                {
                    _clickWatcher.Dispose();
                    _clickWatcher = null;
                }

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

            bool wasPainting = _baseIsPainting;
            _baseIsPainting = true;

            try
            {
                base.WndProc(ref m);
            }
            finally
            {
                _baseIsPainting = wasPainting;
            }

            if (m.Msg == WM_MOUSEMOVE)
                UpdateInnerHot(GetPoint(m.LParam));

            // (Only a paint message matters - and the question below asks the window for its text length, so it must not be asked for every
            // message. Most boxes have nothing to paint over: no window context is asked for then.)
            if ((m.Msg != WM_PAINT && m.Msg != WM_PRINT) || !HasPaintOver)
                return;

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
    }
}

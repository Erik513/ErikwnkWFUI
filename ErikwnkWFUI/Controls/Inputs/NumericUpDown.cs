using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The standard <see cref="System.Windows.Forms.NumericUpDown"/> with the two arrow fields drawn by the library, in
    /// the colors of a <see cref="UIColorSet"/> (the standard control draws them in the system's look, a light block
    /// next to a dark field), a frame of the library around the field, and a prefix and a suffix for the number.
    /// Everything else is the standard control: the text field, the value, the limits, the keyboard, the mouse wheel and
    /// the repeat while an arrow is held.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Look.</b> The two arrow fields take their fill and arrow from <see cref="Colors"/> for rest, hovered, pressed
    /// and disabled; a field whose fill is close to the color of the field gets a quiet outline. With the focus the fields
    /// are framed in one pixel in the color of the set. The frame around the whole control is the border of
    /// <c>BorderStyle.FixedSingle</c> painted over in the library's colors (see <see cref="InputField"/>): a quiet outline,
    /// with the focus the color of the set. The number is moved to the middle of the box vertically. Everything is read
    /// while painting, so a change of the theme or the accent is followed live.
    /// </para>
    /// <para>
    /// <b>Colors of the field.</b> The field's own colors are the standard control's <c>BackColor</c> and
    /// <c>ForeColor</c>; the factories set them from the theme (the one background of all input fields, dimmed when
    /// disabled) and a color set by hand stays.
    /// </para>
    /// <para>
    /// <b>Left to the standard control:</b> the text of a disabled field (Windows draws it in its gray), the thick border
    /// of <c>BorderStyle.Fixed3D</c> (only its outer pixel gets the library's frame), and right-to-left layouts (not
    /// supported by the library). Everything else the standard control offers - <c>ReadOnly</c>, <c>UpDownAlign</c>,
    /// <c>Hexadecimal</c>, <c>DecimalPlaces</c>, <c>ThousandsSeparator</c>, <c>InterceptArrowKeys</c>,
    /// <c>Accelerations</c>, <c>TextAlign</c> - works as before.
    /// </para>
    /// </remarks>
    // In three files: this one (the colors, the layout of the number, the frame), NumericUpDown.Arrows.cs (the two arrow fields)
    // and NumericUpDown.Affixes.cs (the prefix and the suffix).
    public partial class NumericUpDown : System.Windows.Forms.NumericUpDown
    {
        // A field whose fill differs less than this from the field's color (contrast ratio) gets an outline.
        private const double OutlineBelowContrast = 2.0;

        // The frame of the focus must differ at least this much from the fill it surrounds.
        private const double FocusFrameAgainstFill = 1.5;

        // The least contrast (against the field) of the frame of the focus, of an arrow against its fill, and of the text
        // color used as the frame where the set's color is too close to its fill.
        private const double FocusFrameContrast = 3.0;
        private const double ArrowContrast = 3.0;
        private const double FocusTextContrast = 4.5;

        // A disabled field is its rest fill drawn this far toward the color of the field.
        private const double DisabledFillBlend = 0.65;

        // The fill at rest, hovered and pressed of the neutral set on a light theme: pale grays with a dark arrow.
        private static readonly Color LightNeutralBasic = Color.FromArgb(228, 228, 228);
        private static readonly Color LightNeutralHover = Color.FromArgb(212, 212, 212);
        private static readonly Color LightNeutralPressed = Color.FromArgb(190, 190, 190);

        // The two children of the standard control: the arrow buttons (UpDownButtons, not public) and the text field.
        private readonly Control _buttons;
        private readonly Control _edit;
        private readonly ArrowFields _arrowFields;

        private readonly ColorSetHolder _colorSet = new ColorSetHolder();

        // Moving the text field to the middle (see CenterNumber).
        private bool _centering;
        private int _shiftedTop = int.MinValue;
        private int _appliedShift;

        /// <summary>Makes a field with arrows in the neutral look of the theme; <c>UIStyles.NumericUpDowns</c> makes the usual ones.</summary>
        public NumericUpDown()
        {
            _buttons = FindButtons();
            _arrowFields = new ArrowFields(this);
            _edit = FindEdit();

            // The standard control places the text field again whenever the size or the font changes: the move that puts
            // the number in the middle is made again each time (see CenterNumber).
            if (_edit != null)
            {
                _edit.LocationChanged += (sender, e) => CenterNumber();
                _edit.SizeChanged += (sender, e) => CenterNumber();
            }

            if (_buttons != null)
            {
                // The window of the buttons is made again with every new window of the control.
                _buttons.HandleCreated += (sender, e) => _arrowFields.AssignHandle(_buttons.Handle);
                _buttons.HandleDestroyed += (sender, e) => _arrowFields.ReleaseHandle();

                if (_buttons.IsHandleCreated)
                    _arrowFields.AssignHandle(_buttons.Handle);
            }

            // The border of BorderStyle.FixedSingle (the factory's) is part of the window outside its client area, in the
            // system's gray: the library paints its own frame over it - a quiet outline, with the focus the color of the
            // set.
            InputField.AttachFrame(this, () => BorderStyle == BorderStyle.FixedSingle, () => InputField.GetFrameColor(this, Colors, ContainsFocus));

            // The colors are read while painting: a change of the theme only needs a repaint.
            ThemeWatcher.Attach(this, RepaintAll);
        }

        /// <summary>
        /// The colors of the arrow fields: their fill at rest, hovered, pressed and disabled, and the arrow on each.
        /// Left null, the neutral look of the theme: the gray set of the library on a dark theme, pale gray fields with a dark
        /// arrow on a light one; <c>UIStyles.NumericUpDowns.CreatePrimary</c> makes one in the accent color.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIColorSet Colors
        {
            get { return _colorSet.Value; }
            set
            {
                _colorSet.Value = value;
                RepaintArrows();
            }
        }

        // A set that is asked for again each time (the primary set, which follows the accent).
        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorSet.Follow(source);
            RepaintArrows();
        }

        // A seam for the tests: pretends another screen (0 is the real one).
        internal int DpiOverride;

        private int Px(int value96)
        {
            return Dpi.Scale(this, value96, DpiOverride);
        }

        /// <inheritdoc/>
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            RepaintAll();
        }

        /// <inheritdoc/>
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            RepaintAll();
        }

        // The standard control draws a ring around its text field (black when it is disabled); what lies outside the two
        // children is painted over in the field's color.
        /// <inheritdoc/>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (Region ring = new Region(ClientRectangle))
            using (SolidBrush back = new SolidBrush(BackColor))
            {
                foreach (Control child in Controls)
                    ring.Exclude(child.Bounds);

                e.Graphics.FillRegion(back, ring);
            }
        }

        /// <inheritdoc/>
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            _hotField = -1;
            _pressedField = -1;

            // (A disabled control gets no MouseLeave, and an enabled one no MouseMove before the mouse moves: it is looked at where the mouse is.)
            if (Enabled)
                RefreshHotField();

            RepaintAll();
        }

        // The arrow buttons are a child control of the standard control (UpDownButtons, not public).
        private Control FindButtons()
        {
            foreach (Control child in Controls)
            {
                if (child.GetType().Name == "UpDownButtons")
                    return child;
            }

            return null;
        }

        private Control FindEdit()
        {
            foreach (Control child in Controls)
            {
                if (child != _buttons)
                    return child;
            }

            return null;
        }

        // The text field of the standard control puts the line of text at its top edge, a fixed distance from the border,
        // so the number sits above the middle of the box with a small font and below it with a large one (measured
        // for 8 to 24 points: one pixel too high up to a line height of 16, centered from 18 to 25, then more and more
        // too low). The field is moved by that much. The standard control places it again whenever the size or the
        // font changes, which is why this follows the field's own location (and remembers what it moved it by).
        private void CenterNumber()
        {
            if (_centering || _edit == null)
                return;

            _centering = true;

            try
            {
                // Where the standard control put the field: what it has now, less what this moved it by before.
                int baseTop = _edit.Top == _shiftedTop ? _edit.Top - _appliedShift : _edit.Top;
                int lineHeight = Font.Height;
                int shift = lineHeight <= 16 ? 1 : lineHeight >= 30 ? -(int)Math.Round((lineHeight - 26) / 7.0) : 0;

                // The measures above are those of the factory's FixedSingle; the standard control places the field one
                // pixel higher without a border and one lower with the 3D one.
                shift += BorderStyle == BorderStyle.None ? 1 : BorderStyle == BorderStyle.Fixed3D ? -1 : 0;

                _appliedShift = shift;
                _shiftedTop = baseTop + shift;

                if (_edit.Top != _shiftedTop)
                    _edit.Top = _shiftedTop;
            }
            finally
            {
                _centering = false;
            }
        }
    }
}

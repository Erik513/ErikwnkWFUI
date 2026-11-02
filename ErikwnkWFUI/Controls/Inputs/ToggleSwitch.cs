using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// An iOS-style on/off switch, an alternative to a <see cref="CheckBox"/>. Make one with <c>UIStyles.ToggleSwitches</c>: gray when it is on
    /// (<c>CreateStandard</c>), in the accent (<c>CreatePrimary</c>) or in the colors of a color set (<c>colors:</c>). It works as a standard
    /// check box does: a click toggles it when the press ends over it, the space bar toggles it (pressed while down, toggled on release, with
    /// a focus frame once the keyboard has been used), and a screen reader is told a check button with its state - set
    /// <see cref="Control.AccessibleName"/>, as there is no text on the switch. When the user switches it, the knob glides (not when code
    /// sets <see cref="Checked"/>, nor while Windows has the animations off). It follows the theme and the dpi of the screen. There is no
    /// <c>Text</c> (put a label beside it), no three states and no right-to-left layout.
    /// </summary>
    public partial class ToggleSwitch : Control
    {
        private const int DefaultWidth = 45;

        private const int DefaultHeight = 25;

        private const int PaddingSize = 2;

        private const int BorderThickness = 1;

        private const int KnobMargin = 2;

        // Another dpi than the one of the screen, for the tests (every measure is one of 96 dpi, scaled to the screen).
        internal int DpiOverride;

        private int Px(int value96)
        {
            return Dpi.Scale(this, value96, DpiOverride);
        }

        private bool _checked;

        private bool _isHovered;

        private bool _isPressed;

        private ToolTip _toolTip;

        private string _toolTipTextChecked = "";

        private string _toolTipTextUnchecked = "";

        /// <summary>Raised when <see cref="Checked"/> changes, whether from user interaction or setting the property directly.</summary>
        public event EventHandler CheckedChanged;

        /// <summary>The switch's on/off state.</summary>
        public bool Checked
        {
            get
            {
                return _checked;
            }
            set
            {
                if (_checked == value)
                    return;

                _checked = value;

                OnCheckedChangedAnimation();
                UpdateToolTip();
                Invalidate();

                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Tooltip text shown while <see cref="Checked"/> is true.</summary>
        public string ToolTipTextChecked
        {
            get
            {
                return _toolTipTextChecked;
            }
            set
            {
                _toolTipTextChecked = value ?? "";
                UpdateToolTip();
            }
        }

        /// <summary>Tooltip text shown while <see cref="Checked"/> is false.</summary>
        public string ToolTipTextUnchecked
        {
            get
            {
                return _toolTipTextUnchecked;
            }
            set
            {
                _toolTipTextUnchecked = value ?? "";
                UpdateToolTip();
            }
        }

        /// <summary>Write-only shorthand for setting both <see cref="ToolTipTextChecked"/> and <see cref="ToolTipTextUnchecked"/> to the same text.</summary>
        public string ToolTipText
        {
            set
            {
                string text = value ?? "";

                _toolTipTextChecked = text;
                _toolTipTextUnchecked = text;

                UpdateToolTip();
            }
        }

        private readonly ColorSetHolder _colorSet = new ColorSetHolder();

        /// <summary>Makes a switch that is off, in the neutral gray set, 45 x 25 at 96 dpi; the factories (<c>UIStyles.ToggleSwitches</c>) make the usual ones.</summary>
        public ToggleSwitch()
        {
            // Everything is read while painting: a change of the theme only needs a repaint.
            ThemeWatcher.Attach(this, Invalidate);
            InitializeAnimation();
            ConfigureControl();
            CreateToolTip();
        }

        /// <inheritdoc/>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            AnimationSettings.Add(_settings);
        }

        /// <inheritdoc/>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            AnimationSettings.Remove(_settings);
            Settle();
            _isHovered = false;
            _isPressed = false;
            base.OnHandleDestroyed(e);
        }

        /// <inheritdoc/>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle toggleRectangle = GetToggleRectangle();
            Rectangle knobRectangle = GetKnobRectangle(toggleRectangle);

            DrawBackground(e.Graphics, toggleRectangle);
            DrawBorder(e.Graphics, toggleRectangle);
            DrawKnob(e.Graphics, knobRectangle);
            DrawKnobBorder(e.Graphics, knobRectangle);
            PaintFocus(e.Graphics, toggleRectangle);
        }

        // The dotted frame of the keyboard focus around the track - only once the keyboard has been used (as on a standard check box).
        private void PaintFocus(Graphics graphics, Rectangle toggleRectangle)
        {
            if (!Focused || !ShowFocusCues)
                return;

            Rectangle area = Rectangle.Inflate(toggleRectangle, Px(2), Px(2));
            area.Intersect(ClientRectangle);

            ControlPaint.DrawFocusRectangle(graphics, area, UIColors.TextPrimary, SurfaceColor.Of(this));
        }

        /// <inheritdoc/>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Parent == null || BackColor != Color.Transparent)
            {
                base.OnPaintBackground(e);
                return;
            }

            ParentBackground.Paint(this, e.Graphics, args =>
            {
                InvokePaintBackground(Parent, args);
                InvokePaint(Parent, args);
            });
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeAnimation();

                if (_toolTip != null)
                {
                    _toolTip.Dispose();
                    _toolTip = null;
                }
            }

            base.Dispose(disposing);
        }

        private void ConfigureControl()
        {
            Size = new Size(Dpi.Scale(this, DefaultWidth), Dpi.Scale(this, DefaultHeight));

            SetStyle(ControlStyleSets.OwnerDrawn | ControlStyles.SupportsTransparentBackColor, true);

            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
        }

        private void CreateToolTip()
        {
            _toolTip = UIToolTipFactory.CreateHoverToolTip(this);
        }

        private void UpdateToolTip()
        {
            if (_toolTip == null)
                return;

            string text = Checked
                ? _toolTipTextChecked
                : _toolTipTextUnchecked;

            _toolTip.SetToolTip(this, text ?? "");
        }

        private Rectangle GetToggleRectangle()
        {
            int padding = Px(PaddingSize);

            return new Rectangle(
                padding,
                padding,
                Width - padding * 2,
                Height - padding * 2);
        }

        private Rectangle GetKnobRectangle(Rectangle toggleRectangle)
        {
            // The same margin on all four sides: the knob sits centered vertically and inset equally at each end.
            int margin = Px(KnobMargin);

            int knobSize = toggleRectangle.Height - margin * 2;

            // From the left end to the right one; between them while the knob glides.
            int left = toggleRectangle.X + margin;
            int right = toggleRectangle.Right - knobSize - margin;
            int knobX = left + (int)Math.Round((right - left) * _position);

            return new Rectangle(
                knobX,
                toggleRectangle.Y + margin,
                knobSize,
                knobSize);
        }

        private void DrawBackground(Graphics graphics, Rectangle rectangle)
        {
            GraphicsPath path = RoundedRectangle.CreatePath(
                rectangle,
                rectangle.Height / 2);

            SolidBrush brush = new SolidBrush(GetBackgroundColor());

            try
            {
                graphics.FillPath(brush, path);
            }
            finally
            {
                brush.Dispose();
                path.Dispose();
            }
        }

        private void DrawBorder(Graphics graphics, Rectangle rectangle)
        {
            GraphicsPath path = RoundedRectangle.CreatePath(
                rectangle,
                rectangle.Height / 2);

            Pen pen = new Pen(GetBorderColor(), Math.Max(1, Px(BorderThickness)));

            try
            {
                graphics.DrawPath(pen, path);
            }
            finally
            {
                pen.Dispose();
                path.Dispose();
            }
        }

        private void DrawKnob(Graphics graphics, Rectangle rectangle)
        {
            SolidBrush brush = new SolidBrush(GetKnobColor());

            try
            {
                graphics.FillEllipse(brush, rectangle);
            }
            finally
            {
                brush.Dispose();
            }
        }

        private void DrawKnobBorder(Graphics graphics, Rectangle rectangle)
        {
            // A disabled knob has no outline of its own: it is a little lighter than the track, as the whole switch is quiet.
            Color borderColor = Enabled
                ? GetOutlineColor()
                : GetDisabledTrackColor();

            Pen pen = new Pen(borderColor, Math.Max(1, Px(BorderThickness)));

            try
            {
                graphics.DrawEllipse(pen, rectangle);
            }
            finally
            {
                pen.Dispose();
            }
        }
    }
}

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
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

        /// <summary>
        /// The colors of the switch while it is on: the <c>Hover</c> color of the set is the main color (the track, its outline and the
        /// outline while the mouse is over it); it is moved just far enough to stand out from the surface and from the white knob. Null
        /// (the default) is a neutral gray; <c>UIStyles.ToggleSwitches.CreatePrimary</c> makes one that follows the accent;
        /// <c>CreateStandard(colors: ...)</c> takes a color set or simply a color (e.g. a red one for a switch that is always a danger).
        /// Not written by the designer (a set is code).
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIColorSet Colors
        {
            get { return _colorSet.Value; }
            set
            {
                _colorSet.Value = value;
                Invalidate();
            }
        }

        // A set that is asked for again each time it is needed (the primary set, which follows the accent); a later Colors replaces it.
        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorSet.Follow(source);
            Invalidate();
        }

        public ToggleSwitch()
        {
            // Everything is read while painting: a change of the theme only needs a repaint.
            ThemeWatcher.Attach(this, Invalidate);
            InitializeAnimation();
            ConfigureControl();
            CreateToolTip();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;

            Invalidate();

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            _isPressed = false;

            Invalidate();

            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                bool wasPressed = _isPressed;
                _isPressed = false;

                // As a standard check box: the press has to end over the switch (a press that is dragged away and let go elsewhere
                // does nothing), and a press that has been cancelled in the meantime (the capture was lost) does not count.
                if (wasPressed && ClientRectangle.Contains(e.Location))
                    ToggleByUser();

                Invalidate();
            }

            base.OnMouseUp(e);
        }

        // The window that takes the mouse away (a dialog that opens on the press) means no release will come: nothing stays pressed.
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);

            if (_isPressed && !Capture)
            {
                _isPressed = false;
                Invalidate();
            }
        }

        // The space bar works as on a standard check box: the switch looks pressed while it is down and toggles when it is let go.
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Space && !e.Alt && !e.Control && !_isPressed && Enabled)
            {
                _isPressed = true;
                Invalidate();
                e.Handled = true;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.KeyCode != Keys.Space)
                return;

            bool wasPressed = _isPressed;
            _isPressed = false;
            Invalidate();

            if (wasPressed && Enabled)
            {
                ToggleByUser();
                e.Handled = true;
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        // A press with the space bar that is interrupted by the focus going elsewhere is cancelled (its release would never come).
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            _isPressed = false;
            Invalidate();
        }

        // A disabled control gets no MouseLeave: whether the mouse is over it is looked at again, and nothing stays pressed.
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            _isPressed = false;
            _isHovered = Enabled && IsHandleCreated && ClientRectangle.Contains(PointToClient(Cursor.Position));
            Invalidate();
        }

        // A control that is hidden under the mouse gets no MouseLeave either.
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!Visible)
            {
                _isHovered = false;
                _isPressed = false;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            AnimationSettings.Add(_settings);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            AnimationSettings.Remove(_settings);
            Settle();
            _isHovered = false;
            _isPressed = false;
            base.OnHandleDestroyed(e);
        }

        protected override AccessibleObject CreateAccessibilityInstance()
        {
            return new ToggleSwitchAccessibleObject(this);
        }

        // What a screen reader is told: a check button (a switch is read as one), whether it is on, and that it can be toggled. The name
        // is the AccessibleName of the control (there is no text on the switch; set it, or let a label beside it name it).
        private sealed class ToggleSwitchAccessibleObject : ControlAccessibleObject
        {
            private readonly ToggleSwitch _switch;

            public ToggleSwitchAccessibleObject(ToggleSwitch toggle)
                : base(toggle)
            {
                _switch = toggle;
            }

            public override AccessibleRole Role
            {
                get { return AccessibleRole.CheckButton; }
            }

            public override AccessibleStates State
            {
                get { return _switch.Checked ? base.State | AccessibleStates.Checked : base.State; }
            }

            public override string DefaultAction
            {
                get { return _switch.Enabled ? UIStrings.Get("ToggleSwitch.Toggle") : string.Empty; }
            }

            public override void DoDefaultAction()
            {
                if (_switch.Enabled)
                    _switch.ToggleByUser();
            }
        }

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

        private Color GetBackgroundColor()
        {
            if (!Enabled)
                return GetDisabledTrackColor();

            // (Between the two while the knob glides.)
            if (_position >= 1)
                return GetCheckedColor();

            // BackgroundLight, not BackgroundMedium: that one is pure white in the light theme, where the track would vanish into its
            // surroundings. BackgroundLight is a visible light gray there (and subtly lighter than the surface in the dark theme).
            return Mix(UIStyles.Colors.BackgroundLight, GetCheckedColor());
        }

        // A disabled switch is quiet in both states and in both themes, quieter than an enabled one that is off: the off track is the surface
        // moved just far enough to be seen (1.5:1, as the track of a slider), the on track the same a little further (2.2:1) - so on and off
        // can still be told apart by the track as well as by the knob - and the knob is white taken a good way toward the track.
        private const double DisabledOnContrast = 2.2;
        private const double DisabledKnobBlend = 0.45;

        private Color GetDisabledTrackColor()
        {
            Color surface = SurfaceColor.Of(this);
            Color off = UIColors.GetQuietTrackColor(surface);
            Color on = UIColors.EnsureContrast(surface, surface, DisabledOnContrast);

            return Mix(off, on);
        }

        // The outline of the track while the switch is off, and of the knob: the gray of the theme, moved to stand out from the surface (2.2:1;
        // the plain gray was 1.5:1 on the light theme, where the track itself is as light as the surface, and the off switch was hard to see).
        private const double OutlineContrast = 2.2;

        private Color GetOutlineColor()
        {
            return UIColors.EnsureContrast(UIStyles.Colors.BorderMedium, SurfaceColor.Of(this), OutlineContrast);
        }

        // The main color of the set, or the neutral gray.
        private Color GetMainColor()
        {
            return (Colors ?? UIStyles.Colors.Sets.Gray).Hover;
        }

        // The track while the switch is on: the main color moved just far enough to stand out from the surface (2:1) and to let the white
        // knob stand out from it (1.5:1) - a pale yellow turns a little darker, a dark blue stays as it is.
        private const double CheckedOnSurfaceContrast = 2.0;
        private const double CheckedUnderKnobContrast = 1.5;

        private Color GetCheckedColor()
        {
            return UIColors.EnsureContrast(GetMainColor(), SurfaceColor.Of(this), CheckedOnSurfaceContrast, UIColors.White, CheckedUnderKnobContrast);
        }

        private Color GetBorderColor()
        {
            // The same as the fill: a uniformly muted pill, without a separate outline.
            if (!Enabled)
                return GetDisabledTrackColor();

            // Matches the fill exactly, so a checked switch always reads as one consistent color instead of a colored center with a
            // leftover neutral-gray outline.
            if (_position >= 1)
                return GetCheckedColor();

            // The main color of the set, moved to stand out from the surface (gray for CreateStandard, the accent for CreatePrimary).
            Color resting = _isHovered
                ? UIColors.EnsureContrast(GetMainColor(), SurfaceColor.Of(this), CheckedOnSurfaceContrast)
                : GetOutlineColor();

            return Mix(resting, GetCheckedColor());
        }

        private Color GetKnobColor()
        {
            // Disabled: white taken part of the way toward the disabled track, so it is not as bold as an enabled knob.
            if (!Enabled)
                return UIColors.BlendToward(UIColors.White, GetDisabledTrackColor(), DisabledKnobBlend);

            if (_isPressed)
                return UIColors.BlendToward(UIColors.White, GetCheckedColor(), 0.25);

            // TextPrimary flips to near-black in Light theme (it's a theme
            // role, not a fixed shade), which made a hovered knob go black
            // there. The knob's own surface (like ToastForm/InfoPopupForm's
            // backgrounds) doesn't follow the theme at all, so its hover
            // tint shouldn't either - LightForeColor is the fixed light
            // shade meant for exactly this kind of surface.
            if (_isHovered)
                return UIColors.LightForeColor;

            return UIStyles.Colors.White;
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A card: a panel with smoothly rounded corners and a thin line around it, for a group of controls on a page
    /// (<c>UIStyles.Panels.CreateCardStandard</c>). It paints what is behind it in its corners, so it works on any page color.
    /// </summary>
    /// <remarks>
    /// The controls inside are not cut to the rounded shape: <see cref="Padding"/> keeps them from the corners (the card has
    /// one of its own by default). <c>AutoScroll</c> is not meant for a card (its scroll bars are rectangular and would stick
    /// out of the corners). A <c>BackgroundImage</c> is not drawn (the card paints its own shape), and a <c>BorderStyle</c> of the
    /// standard panel is not meant for it: the line is <see cref="ShowBorder"/> and <see cref="BorderColor"/>.
    /// <see cref="MultiPanel"/> is a card with pages (flat, with a <see cref="CornerRadius"/> of 0 and no line, or rounded).
    /// </remarks>
    public class ThemedCardPanel : Panel
    {
        private const int DefaultCornerRadius96 = 8;
        private const int DefaultPadding96 = 8;

        private readonly ThemeColor _borderColor;
        private int _cornerRadius = DefaultCornerRadius96;
        private bool _showBorder = true;

        public ThemedCardPanel()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            _borderColor = new ThemeColor(() => UIColors.GetLineColor(SurfaceColor.Of(this)));
            Padding = new Padding(Dpi.Scale(this, DefaultPadding96));
            ThemeWatcher.Attach(this, Invalidate);
        }

        // The line of the card takes this color until BorderColor is set (asked for again each time).
        internal void FollowBorderColor(Func<Color> color)
        {
            _borderColor.SetDefault(color);
            Invalidate();
        }

        /// <summary>The radius of the rounded corners at 96 dpi (scaled with the screen); 8 by default, 0 gives square corners.</summary>
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set
            {
                _cornerRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        /// <summary>Whether the thin line around the card is drawn (on by default).</summary>
        public bool ShowBorder
        {
            get { return _showBorder; }
            set
            {
                _showBorder = value;
                Invalidate();
            }
        }

        /// <summary>The color of the thin line around the card; follows the theme (and, on a color, is moved until it is seen against it) until it is set, <see cref="Color.Empty"/> gives that back.</summary>
        public Color BorderColor
        {
            get { return _borderColor.Value; }
            set
            {
                if (value.IsEmpty)
                    _borderColor.Reset();
                else
                    _borderColor.Set(value);

                Invalidate();
            }
        }

        // The corners show what is behind the card; the shape is painted over it.
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            if (Parent != null)
                ParentBackground.Paint(this, pevent.Graphics, args => InvokePaintBackground(Parent, args));

            SmoothingMode previous = pevent.Graphics.SmoothingMode;
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            pevent.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

            using (GraphicsPath shape = RoundedRectangle.CreatePath(new RectangleF(0, 0, Width, Height), GetRadius()))
            using (SolidBrush brush = new SolidBrush(BackColor))
                pevent.Graphics.FillPath(brush, shape);

            pevent.Graphics.SmoothingMode = previous;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!_showBorder)
                return;

            float thickness = Math.Max(1, Dpi.Scale(this, 1));
            RectangleF line = new RectangleF(thickness / 2, thickness / 2, Width - thickness, Height - thickness);
            SmoothingMode previous = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

            using (GraphicsPath outline = RoundedRectangle.CreatePath(line, Math.Max(0, GetRadius() - thickness / 2)))
            using (Pen pen = new Pen(BorderColor, thickness))
                e.Graphics.DrawPath(pen, outline);

            e.Graphics.SmoothingMode = previous;
        }

        private float GetRadius()
        {
            return Math.Min(Dpi.Scale(this, _cornerRadius), Math.Min(Width, Height) / 2f);
        }
    }
}

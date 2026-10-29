using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A themed <see cref="System.Windows.Forms.GroupBox"/>, drawn entirely by the library: a thin rounded frame with the title in a gap
    /// of its upper edge. The frame is the quiet line of the theme (and is moved until it is seen on a colored surface); with a
    /// <see cref="UIColorSet"/> it takes the color of the set. Get one through <see cref="UIStyles.GroupBoxes"/>.
    /// </summary>
    /// <remarks>
    /// It keeps everything that is not looks from the standard control: the children and their layout (<c>DisplayRectangle</c>), <c>Text</c>,
    /// the mnemonic, <c>Padding</c> and docking. Because the library draws it itself, <c>FlatStyle</c> and <c>UseCompatibleTextRendering</c>
    /// are ignored. Radio buttons placed in a group box form a group of their own.
    /// </remarks>
    public class GroupBox : System.Windows.Forms.GroupBox
    {
        // The measures at 96 dpi.
        private const int CornerRadius96 = 4;
        private const int TitleInset96 = 10;
        private const int TitleGap96 = 4;
        private const double SetFrameContrast = 3.0;

        private readonly ColorSetHolder _colorSet = new ColorSetHolder();
        private readonly ThemeColor _borderColor;

        public GroupBox()
        {
            // Painted as a whole in one go (the standard box flickers), by OnPaint alone.
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);

            _borderColor = new ThemeColor(GetDefaultBorderColor);

            ThemeWatcher.Attach(this, Invalidate);
        }

        /// <summary>
        /// The color of the frame: the quiet line of the theme (moved until it is seen against the surface) until it is set;
        /// <see cref="Color.Empty"/> gives that back. A <see cref="Colors"/> set colors the frame as long as this is not set by hand.
        /// </summary>
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

        private bool ShouldSerializeBorderColor()
        {
            return _borderColor.IsSet;
        }

        private void ResetBorderColor()
        {
            BorderColor = Color.Empty;
        }

        /// <summary>
        /// The colors of the frame: the <c>Hover</c> color of the set, moved just far enough to be seen against the surface. Null (the
        /// default) is the quiet line of the theme; <c>UIStyles.GroupBoxes.CreatePrimary</c> makes one that follows the accent.
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

        // A set that is asked for again each time (the primary set, which follows the accent).
        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorSet.Follow(source);
            Invalidate();
        }

        private Color GetDefaultBorderColor()
        {
            Color surface = SurfaceColor.Of(this);
            UIColorSet colors = _colorSet.Value;

            return colors == null
                ? UIColors.GetLineColor(surface)
                : UIColors.EnsureContrast(colors.Hover, surface, SetFrameContrast);
        }

        private Color GetFrameColor(Color surface)
        {
            return Enabled
                ? _borderColor.Value
                : UIColors.BlendToward(_borderColor.Value, surface, 0.5);
        }

        private Color GetTitleColor(Color surface)
        {
            return Enabled
                ? UIColors.TextPrimary
                : UIColors.BlendToward(UIColors.GetContrastingForeColor(surface), surface, 0.35);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            Invalidate();
        }

        private TextFormatFlags GetTextFlags()
        {
            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;

            // A mnemonic ("&Options") shows its underline only once the Alt key was used.
            if (!ShowKeyboardCues)
                flags |= TextFormatFlags.HidePrefix;

            return flags;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color surface = SurfaceColor.Of(this);

            using (SolidBrush erase = new SolidBrush(surface))
                g.FillRectangle(erase, ClientRectangle);

            int inset = Dpi.Scale(TitleInset96, DeviceDpi);
            int gap = Dpi.Scale(TitleGap96, DeviceDpi);
            int titleHeight = Font.Height;

            // The frame starts half the title's height below the top, so the title sits on the upper edge.
            Rectangle frame = new Rectangle(0, titleHeight / 2, Width - 1, Height - 1 - titleHeight / 2);
            int titleWidth = 0;

            if (!string.IsNullOrEmpty(Text))
            {
                int room = Math.Max(0, Width - inset * 2);
                TextFormatFlags measure = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
                titleWidth = Math.Min(room, TextRenderer.MeasureText(g, Text, Font, new Size(int.MaxValue, titleHeight), measure).Width);
            }

            if (frame.Width > 0 && frame.Height > 0)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (GraphicsPath path = RoundedRectangle.CreatePath(frame, Dpi.Scale(CornerRadius96, DeviceDpi)))
                using (Pen pen = new Pen(GetFrameColor(surface), Math.Max(1f, Dpi.Scale(10, DeviceDpi) / 10f)))
                {
                    // The gap for the title: the frame is drawn everywhere but behind the title.
                    Region previous = g.Clip;

                    if (titleWidth > 0)
                    {
                        using (Region clip = new Region(ClientRectangle))
                        {
                            clip.Exclude(new Rectangle(inset - gap, 0, titleWidth + gap * 2, titleHeight));
                            g.Clip = clip;
                            g.DrawPath(pen, path);
                        }
                    }
                    else
                    {
                        g.DrawPath(pen, path);
                    }

                    g.Clip = previous;
                }

                g.SmoothingMode = SmoothingMode.Default;
            }

            if (titleWidth > 0)
                TextRenderer.DrawText(g, Text, Font, new Rectangle(inset, 0, titleWidth, titleHeight), GetTitleColor(surface), GetTextFlags());
        }
    }
}

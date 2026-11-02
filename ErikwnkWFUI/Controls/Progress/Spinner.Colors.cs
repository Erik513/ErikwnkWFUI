using System;
using System.ComponentModel;
using System.Drawing;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // The colors of the ring, the arc and the text, and what they are worked out from.
    public partial class Spinner
    {
        private readonly ColorSetHolder _colorSet = new ColorSetHolder();
        private Color _arcColor = Color.Empty;
        private bool _useStatusGradient;

        /// <summary>
        /// The colors of the arc: its main color is the <c>Hover</c> color of the set (Green is the usual green, the primary set the
        /// accent). Null (the default) is the neutral gray; <c>UIStyles.Spinners.CreatePrimary</c> makes one that follows the accent.
        /// <see cref="ArcColor"/> and <see cref="UseStatusGradient"/> win over it. Not written by the designer (a set is code).
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

        /// <summary>An explicit color of the arc, overriding <see cref="Colors"/> and <see cref="UseStatusGradient"/> (not the disabled look). <see cref="Color.Empty"/> (the default) uses those rules.</summary>
        [DefaultValue(typeof(Color), "")]
        public Color ArcColor
        {
            get { return _arcColor; }
            set
            {
                _arcColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// True = the arc color tells the state of the load: a solid color blended red (0) over yellow to green (100) by
        /// <see cref="Value"/>. Ignored when <see cref="ArcColor"/> is set. As <see cref="SlimProgressBar.UseStatusGradient"/>.
        /// </summary>
        [DefaultValue(false)]
        public bool UseStatusGradient
        {
            get { return _useStatusGradient; }
            set
            {
                _useStatusGradient = value;
                Invalidate();
            }
        }

        // What the spinner lies on: its own color if it has one, else the first color that is not see-through behind it.
        private Color GetSurface()
        {
            return SurfaceColor.Of(this);
        }

        // The faint full ring: the quiet line color of the surface (1.5:1 against it, in both themes).
        internal Color GetTrackColor()
        {
            return UIColors.GetLineColor(GetSurface());
        }

        // The arc as painted: a color set by hand as it is, the others moved just far enough to stand out from the track (3:1, as the
        // fill of the bars). A disabled spinner has the disabled look.
        private Color ResolveArcColor()
        {
            Color track = GetTrackColor();

            if (!Enabled)
                return UIColors.EnsureContrast(UIColors.GetDisabledText(GetSurface()), track, ProgressBarPainting.FillContrast);
            if (!_arcColor.IsEmpty)
                return _arcColor;
            if (_useStatusGradient)
                return ProgressBarPainting.GetStatusFill(_value, 0, 100, track);

            UIColorSet colors = Colors;

            return UIColors.EnsureContrast(colors != null ? colors.Hover : UIColors.DisabledGray, track, ProgressBarPainting.FillContrast);
        }

        // The color of the text (the digits, the text next to the ring): readable on what the spinner lies on.
        private Color GetTextColor()
        {
            Color surface = GetSurface();

            return Enabled ? ProgressBarPainting.GetTextColorOn(surface) : UIColors.GetDisabledText(surface);
        }
    }
}

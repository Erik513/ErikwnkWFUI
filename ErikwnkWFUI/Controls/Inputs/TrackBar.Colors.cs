using System;
using System.ComponentModel;
using System.Drawing;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // The colors of the track, the filled part and the thumb, and what they are worked out from.
    public partial class TrackBar
    {
        private UIColorSet _colors;
        private Func<UIColorSet> _colorsSource;
        private Color? _fillColor;

        /// <summary>
        /// The colors of the filled part and the thumb: the <c>Hover</c> color of the set is the main color (Green is the usual
        /// green, the primary set the accent). Null (the default) is a neutral gray; <c>UIStyles.TrackBars.CreatePrimary</c> makes
        /// one that follows the accent. <see cref="FillColor"/> wins over it. Not written by the designer (a set is code).
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIColorSet Colors
        {
            get { return _colorsSource?.Invoke() ?? _colors; }
            set
            {
                _colors = value;
                _colorsSource = null;
                Invalidate();
            }
        }

        // A set that is asked for again each time it is needed (the primary set, which follows the accent); a later Colors replaces it.
        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorsSource = source;
            _colors = null;
            Invalidate();
        }

        /// <summary>An explicit fill/thumb color, overriding <see cref="Colors"/> and kept as it is (not the disabled look). Null (the default) uses the color of the set, or the neutral gray.</summary>
        [DefaultValue(null)]
        public Color? FillColor
        {
            get { return _fillColor; }
            set { _fillColor = value; Invalidate(); }
        }

        // What the slider lies on: its own color if it has one, else the first color that is not see-through behind it.
        private Color GetSurface()
        {
            return SurfaceColor.Of(this);
        }

        // The track: the surface moved just far enough to stand out from it, 1.5:1, in both themes.
        internal Color GetTrackColor()
        {
            return UIColors.GetQuietTrackColor(GetSurface());
        }

        // The color of the filled part and the thumb: a color set by hand as it is; the others (the main color of the set, the gray, the
        // disabled color) moved just far enough to stand out from the surface and from the track (3:1, as the fill of the bars). The
        // thumb reaches beyond the track, so it has to be seen on the surface too.
        internal Color GetFillColor()
        {
            Color surface = GetSurface();

            if (!Enabled)
                return Stand(UIColors.GetDisabledText(surface), surface);

            if (_fillColor.HasValue)
                return _fillColor.Value;

            UIColorSet colors = Colors;

            return Stand(colors != null ? colors.Hover : UIColors.DisabledGray, surface);
        }

        private Color Stand(Color color, Color surface)
        {
            return UIColors.EnsureContrast(color, surface, GetTrackColor(), ProgressBarPainting.FillContrast);
        }
    }
}

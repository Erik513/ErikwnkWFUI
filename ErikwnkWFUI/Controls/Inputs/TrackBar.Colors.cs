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

        /// <summary>
        /// The colors of the filled part and the thumb: the <c>Hover</c> color of the set is the main color (Green is the usual
        /// green, the primary set the accent). Null (the default) is a neutral gray; <c>UIStyles.TrackBars.CreatePrimary</c> makes
        /// one that follows the accent; <c>UIStyles.TrackBars.CreateStandard(colors: ...)</c> takes a color set or simply a color. The popup has the
        /// same colors unless <see cref="ValuePopupColors"/> says otherwise. Not written by the designer (a set is code).
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
        // disabled color) moved just far enough to stand out from the surface (2.5:1) and from the track (1.5:1).
        internal Color GetFillColor()
        {
            Color surface = GetSurface();

            if (!Enabled)
                return Stand(UIColors.GetDisabledText(surface), surface);

            UIColorSet colors = Colors;

            return Stand(colors != null ? colors.Hover : UIColors.DisabledGray, surface);
        }

        // The thumb reaches beyond the track and has to be seen on the surface (2.5:1: a strong accent such as a red keeps its strength; the
        // 3:1 of the bars would turn it pale on a dark surface); the filled part only has to be told from the empty rest of the track
        // (1.5:1, as quiet as the track itself: a stronger demand made a red accent lighter and paler on a dark surface, only to stand
        // out from the lighter track).
        private const double FillOnSurfaceContrast = 2.5;
        private const double FillOnTrackContrast = 1.5;

        private Color Stand(Color color, Color surface)
        {
            return UIColors.EnsureContrast(color, surface, FillOnSurfaceContrast, GetTrackColor(), FillOnTrackContrast);
        }
    }
}

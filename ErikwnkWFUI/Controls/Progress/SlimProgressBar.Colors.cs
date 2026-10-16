using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // The colors of the slim bar: the track, the fill, the text; what the designer writes; what is painted.
    public partial class SlimProgressBar
    {
        /// <summary>
        /// The color of the track. By default the quiet line of the library for the panel the bar lies on (as the frame of a panel):
        /// the gray of the theme, moved only as far as the panel needs to keep the track seen (1.5:1) - the track has no border of its own
        /// here. It follows the panel and the theme. Set a color for another one, <see cref="Color.Empty"/> gives the default back; a
        /// transparent color is the one of the panel.
        /// </summary>
        public override Color BackColor
        {
            get
            {
                if (_trackColor == null)
                    return base.BackColor;

                Color track = _trackColor.Value;

                return track.A != 255 ? GetSurface() : track;
            }
            set
            {
                if (_trackColor == null)
                {
                    base.BackColor = value;

                    return;
                }

                if (value.IsEmpty)
                    _trackColor.Reset();
                else
                    _trackColor.Set(value);

                Invalidate();
            }
        }

        /// <summary>
        /// The color of the fill. The bars of <c>UIStyles.SlimProgressBars</c> have the main color of their color set (or the gray) until
        /// one is set; that color is moved only as far as needed to stand out from the track (3:1), a color set here is used as it is.
        /// <see cref="Color.Empty"/> gives the default back.
        /// </summary>
        public override Color ForeColor
        {
            get { return _fillColor == null ? base.ForeColor : _fillColor.Value; }
            set
            {
                if (_fillColor == null)
                {
                    base.ForeColor = value;

                    return;
                }

                if (value.IsEmpty)
                    _fillColor.Reset();
                else
                    _fillColor.Set(value);

                Invalidate();
            }
        }

        /// <summary>
        /// The color of the <see cref="Text"/> and the percentage, which stand on the panel next to the strip. <see cref="Color.Empty"/>
        /// (the default) is the one that reads best on that panel (4.5:1); a disabled bar has the quiet text color of the library in any case.
        /// </summary>
        [DefaultValue(typeof(Color), "")]
        public Color TextColor
        {
            get { return _textColor; }
            set
            {
                _textColor = value;
                Invalidate();
            }
        }

        // The designer writes the colors only where they were set (the defaults are followed, not stored); Reset is the one of Control.
        private bool ShouldSerializeBackColor()
        {
            return _trackColor != null && _trackColor.IsSet;
        }

        private bool ShouldSerializeForeColor()
        {
            return _fillColor != null && _fillColor.IsSet;
        }

        // The color the factory gives the fill; followed live (the theme, the accent) until a color is set by hand.
        internal void SetDefaultFill(Func<Color> color)
        {
            _fillColor.SetDefault(color);
            Invalidate();
        }

        // What the bar lies on: the first color that is not see-through behind it.
        private Color GetSurface()
        {
            return Parent != null ? SurfaceColor.Of(Parent) : UIColors.BackgroundMedium;
        }

        // The fill as painted: a color set by hand as it is, the others moved just far enough to stand out from the track.
        internal Color GetFillColor(Color track)
        {
            if (!Enabled)
                return UIColors.EnsureContrast(UIColors.DisabledGray, track, ProgressBarPainting.FillContrast);

            if (_useStatusGradient)
                return ProgressBarPainting.GetStatusFill(_value, _minimum, _maximum, track);

            return _fillColor.IsSet ? _fillColor.Value : UIColors.EnsureContrast(_fillColor.Value, track, ProgressBarPainting.FillContrast);
        }
    }
}

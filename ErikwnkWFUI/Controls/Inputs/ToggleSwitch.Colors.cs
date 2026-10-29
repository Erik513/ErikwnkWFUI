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
    // The colors of the track, the outline and the knob, and what they are worked out from.
    public partial class ToggleSwitch
    {
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

        private Color GetBackgroundColor()
        {
            if (!Enabled)
                return GetDisabledTrackColor();

            // (Between the two while the knob glides.)
            if (_position >= 1)
                return GetCheckedColor();

            return Mix(GetOffTrackColor(), GetCheckedColor());
        }

        // The track while the switch is off: a little lighter than the surface in a dark theme; on a light surface (where BackgroundLight is
        // practically white, and the track looked like a hole in the page) a little darker than it.
        private const double LightSurfaceOffTrackShade = 0.04;

        private Color GetOffTrackColor()
        {
            Color surface = SurfaceColor.Of(this);

            return UIColors.IsLight(surface)
                ? UIColors.BlendToward(surface, UIColors.Black, LightSurfaceOffTrackShade)
                : UIStyles.Colors.BackgroundLight;
        }

        // A disabled switch is quiet in both states and in both themes, quieter than an enabled one that is off: the off track is the surface
        // moved just far enough to be seen (1.5:1, as the track of a slider), the on track the same a little further (2.2:1) - so on and off
        // can still be told apart by the track as well as by the knob - and the knob is white taken a good way toward the track.
        private const double DisabledOnContrast = 2.2;

        private const double DisabledKnobBlend = 0.35;

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

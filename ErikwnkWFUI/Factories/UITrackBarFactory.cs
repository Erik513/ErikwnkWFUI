using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UITrackBarFactory
    {
        public static TrackBar CreateStandard(double value = 0, double maximum = 1.0, UIColorSet colors = null)
        {
            return new TrackBar
            {
                Maximum = maximum,
                Value = value,
                Colors = colors
            };
        }

        // The set is asked for again each time: the primary set follows the accent.
        public static TrackBar CreatePrimary(double value = 0, double maximum = 1.0)
        {
            TrackBar slider = CreateStandard(value, maximum);
            slider.FollowColors(() => UIColors.PrimarySet);

            return slider;
        }
    }
}

using System;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UITrackBarFactory
    {
        public static TrackBar CreateStandard(double value = 0, double maximum = 1.0, UIColorSet colors = null,
            bool showValuePopup = false, Func<double, string> formatValue = null)
        {
            TrackBar slider = new TrackBar
            {
                Maximum = maximum,
                Value = value,
                Colors = colors,
                ShowValuePopup = showValuePopup
            };

            if (formatValue != null)
                slider.FormatValue = formatValue;

            return slider;
        }

        // The set is asked for again each time: the primary set follows the accent.
        public static TrackBar CreatePrimary(double value = 0, double maximum = 1.0,
            bool showValuePopup = false, Func<double, string> formatValue = null)
        {
            TrackBar slider = CreateStandard(value, maximum, null, showValuePopup, formatValue);
            slider.FollowColors(() => UIColors.PrimarySet);

            return slider;
        }
    }
}

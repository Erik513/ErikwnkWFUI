using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIVolumeSliderFactory
    {
        // No maximum parameter, unlike UITrackBarFactory.CreateStandard -
        // VolumeSlider fixes Maximum to 1.0 itself (a volume is always 0..1).
        public static VolumeSlider CreateStandard(double value = 0, UIColorSet colors = null)
        {
            return new VolumeSlider
            {
                Value = value,
                Colors = colors
            };
        }

        public static VolumeSlider CreatePrimary(double value = 0)
        {
            VolumeSlider slider = CreateStandard(value);
            slider.FollowColors(() => UIColors.PrimarySet);

            return slider;
        }
    }
}

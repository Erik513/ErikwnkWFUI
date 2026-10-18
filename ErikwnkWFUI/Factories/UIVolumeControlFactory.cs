using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIVolumeControlFactory
    {
        // The slider (and its popup) is gray, in the colors of a set, or - for the primary one - in the accent, asked for again each
        // time, so it follows a change of the accent. The mute button has the neutral look in every case.
        public static VolumeControl CreateStandard(double volume = 1.0, UIColorSet colors = null, Size? size = null)
        {
            VolumeControl control = new VolumeControl { Colors = colors };

            return Configure(control, volume, size);
        }

        public static VolumeControl CreatePrimary(double volume = 1.0, Size? size = null)
        {
            VolumeControl control = new VolumeControl();
            control.FollowColors(() => UIColors.PrimarySet);

            return Configure(control, volume, size);
        }

        private static VolumeControl Configure(VolumeControl control, double volume, Size? size)
        {
            if (size.HasValue)
                control.Size = size.Value;

            control.Volume = volume;

            return control;
        }
    }
}

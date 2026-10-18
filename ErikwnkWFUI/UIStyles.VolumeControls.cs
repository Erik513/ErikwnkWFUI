using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// A <see cref="Controls.VolumeControl"/> for audio: a mute button (a loudspeaker, crossed out while it is muted) with a slider beside it,
        /// as on video sites. The volume is remembered while it is muted, the key M on the slider mutes too, and the slider shows the percentage in
        /// a popup. Read <see cref="Controls.VolumeControl.EffectiveVolume"/> for what the audio output is to get, and react to
        /// <see cref="Controls.VolumeControl.VolumeChanged"/>.
        /// </summary>
        public static class VolumeControls
        {
            /// <summary>A volume control with a gray slider, or one in the color of a color set.</summary>
            /// <param name="volume">The start volume, from 0 to 1 (0 starts it muted).</param>
            /// <param name="colors">A color set for the slider and its popup: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>; null (the default) is gray.</param>
            /// <param name="size">The size of the control (the button is as high as it, the slider takes the rest); 150 x 30 when left out.</param>
            public static VolumeControl CreateStandard(double volume = 1.0, UIColorSet colors = null, Size? size = null)
            {
                return UIVolumeControlFactory.CreateStandard(volume, colors, size);
            }

            /// <summary>Same, but the slider and its popup follow the theme accent.</summary>
            public static VolumeControl CreatePrimary(double volume = 1.0, Size? size = null)
            {
                return UIVolumeControlFactory.CreatePrimary(volume, size);
            }
        }
    }
}

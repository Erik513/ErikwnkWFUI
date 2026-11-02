using System.Drawing;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>A small, borderless popup with extra info next to a control or the mouse: blocks of an optional header and an optional text. Set its content, then show it. Reuse one popup for many show calls.</summary>
        public static class InfoPopups
        {
            /// <summary>A popup in the neutral gray.</summary>
            /// <param name="colors">
            /// A color set for the background and the text instead of the neutral gray: one of
            /// <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a
            /// <see cref="Color"/>.
            /// </param>
            /// <param name="fixedSize">A client size the popup keeps whatever its content, with the content centered (a short value that changes while shown, like a percentage over a slider); none sizes it to the content.</param>
            /// <param name="transparency">How see-through the popup is; medium by default.</param>
            /// <param name="duration">How long the popup stays before it hides itself (3, 5 or 8 seconds); medium by default. Every show call starts the time again.</param>
            /// <param name="delay">How long a delayed show (hover) waits before the popup appears: not at all, 0.2, 0.4 (the default) or 0.8 seconds.</param>
            public static ErikwnkWFUI.Forms.InfoPopupForm CreateStandard(UIColorSet colors = null, Size? fixedSize = null, ErikwnkWFUI.Forms.PopupTransparency transparency = ErikwnkWFUI.Forms.PopupTransparency.Medium, ErikwnkWFUI.Forms.PopupDuration duration = ErikwnkWFUI.Forms.PopupDuration.Medium, ErikwnkWFUI.Forms.PopupDelay delay = ErikwnkWFUI.Forms.PopupDelay.Medium)
            {
                return UIInfoPopupFactory.CreateStandard(colors, fixedSize, transparency, duration, delay);
            }

            /// <summary>Like <see cref="CreateStandard"/> but in the current accent color instead of the neutral gray - mirrors <see cref="Buttons.CreatePrimary"/>.</summary>
            public static ErikwnkWFUI.Forms.InfoPopupForm CreatePrimary(Size? fixedSize = null, ErikwnkWFUI.Forms.PopupTransparency transparency = ErikwnkWFUI.Forms.PopupTransparency.Medium, ErikwnkWFUI.Forms.PopupDuration duration = ErikwnkWFUI.Forms.PopupDuration.Medium, ErikwnkWFUI.Forms.PopupDelay delay = ErikwnkWFUI.Forms.PopupDelay.Medium)
            {
                return UIInfoPopupFactory.CreatePrimary(fixedSize, transparency, duration, delay);
            }
        }
    }
}

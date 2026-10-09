using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// Themed <see cref="Label"/>s: <see cref="CreateStandard"/> (body text, gray) and
        /// <see cref="CreatePrimary"/> (in the accent color), plus <see cref="CreateTitle"/>
        /// (bold, larger) and <see cref="CreateMuted"/> (de-emphasized).
        /// </summary>
        public static class Labels
        {
            /// <summary>Body text in the neutral gray.</summary>
            /// <param name="text">The text of the label.</param>
            /// <param name="colors">
            /// A color set for the text instead of the neutral gray: one of
            /// <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a
            /// <see cref="System.Drawing.Color"/>. The text takes the main color of the set,
            /// lightened or darkened where the page behind it needs that to read.
            /// </param>
            public static ThemedLabel CreateStandard(string text = "", UIColorSet colors = null)
            {
                return UILabelFactory.CreateStandard(text, colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but in the current accent color instead of the neutral gray - mirrors <see cref="Buttons.CreatePrimary"/>.</summary>
            public static ThemedLabel CreatePrimary(string text = "")
            {
                return UILabelFactory.CreatePrimary(text);
            }

            /// <summary>A heading: bold and larger, centered.</summary>
            public static ThemedLabel CreateTitle(string text = "")
            {
                return UILabelFactory.CreateTitle(text);
            }

            /// <summary>De-emphasized text: smaller and dimmer than the standard one.</summary>
            public static ThemedLabel CreateMuted(string text = "")
            {
                return UILabelFactory.CreateMuted(text);
            }

            /// <summary>The old name of <see cref="CreateStandard"/>.</summary>
            [Obsolete("Use CreateStandard.")]
            public static ThemedLabel CreateNormal(string text = "")
            {
                return UILabelFactory.CreateStandard(text);
            }
        }
    }
}

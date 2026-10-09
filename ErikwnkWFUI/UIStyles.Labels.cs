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
        /// (bold, larger) and <see cref="CreateMuted"/> (de-emphasized). Each can move its text when it is
        /// too long for the label (<c>scroll</c>, see <see cref="LabelScroll"/>).
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
            /// <param name="scroll">How the text moves when it is too long for the label: <c>LabelScroll.BackAndForth()</c> or <c>LabelScroll.Endless()</c>, each with its own settings. Null (the default) means it does not move, the text ends in an ellipsis.</param>
            public static ThemedLabel CreateStandard(string text = "", UIColorSet colors = null, LabelScroll scroll = null)
            {
                return UILabelFactory.CreateStandard(text, colors, scroll);
            }

            /// <summary>Like <see cref="CreateStandard"/> but in the current accent color instead of the neutral gray - mirrors <see cref="Buttons.CreatePrimary"/>.</summary>
            /// <param name="text">The text of the label.</param>
            /// <param name="scroll">How the text moves when it is too long for the label: <c>LabelScroll.BackAndForth()</c> or <c>LabelScroll.Endless()</c>, each with its own settings. Null (the default) means it does not move, the text ends in an ellipsis.</param>
            public static ThemedLabel CreatePrimary(string text = "", LabelScroll scroll = null)
            {
                return UILabelFactory.CreatePrimary(text, scroll);
            }

            /// <summary>A heading: bold and larger, centered.</summary>
            /// <param name="text">The text of the label.</param>
            /// <param name="scroll">How the text moves when it is too long for the label: <c>LabelScroll.BackAndForth()</c> or <c>LabelScroll.Endless()</c>, each with its own settings. Null (the default) means it does not move, the text ends in an ellipsis.</param>
            public static ThemedLabel CreateTitle(string text = "", LabelScroll scroll = null)
            {
                return UILabelFactory.CreateTitle(text, scroll);
            }

            /// <summary>De-emphasized text: smaller and dimmer than the standard one.</summary>
            /// <param name="text">The text of the label.</param>
            /// <param name="scroll">How the text moves when it is too long for the label: <c>LabelScroll.BackAndForth()</c> or <c>LabelScroll.Endless()</c>, each with its own settings. Null (the default) means it does not move, the text ends in an ellipsis.</param>
            public static ThemedLabel CreateMuted(string text = "", LabelScroll scroll = null)
            {
                return UILabelFactory.CreateMuted(text, scroll);
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

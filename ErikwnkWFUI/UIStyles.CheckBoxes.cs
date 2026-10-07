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
        /// Themed <see cref="CheckBox"/>es, drawn by the library: <see cref="CreateStandard"/> (neutral
        /// gray when checked) and <see cref="CreatePrimary"/> (the accent color), each also as a
        /// label-less <see cref="CreateStandardCompact"/> / <see cref="CreatePrimaryCompact"/> for table
        /// cells. A color set can be given to any of the standard ones. Hover, pressed, keyboard
        /// focus, disabled and the undetermined third state (<c>ThreeState</c>) are built in.
        /// </summary>
        public static class CheckBoxes
        {
            /// <param name="text">The label next to the box.</param>
            /// <param name="isChecked">Whether the box starts checked.</param>
            /// <param name="colors">
            /// The colors of the filled box (rest, hovered, pressed), for example one of
            /// <c>UIStyles.Colors.Sets</c>; left out, the neutral gray. The mark and the
            /// hover and pressed looks follow from the set.
            /// </param>
            public static System.Windows.Forms.CheckBox CreateStandard(
                string text = "",
                bool isChecked = true,
                UIColorSet colors = null)
            {
                return UICheckBoxFactory.CreateStandard(
                    text,
                    isChecked,
                    colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but with the box filled in the current accent instead of the neutral gray - mirrors <see cref="Buttons.CreatePrimary"/>.</summary>
            public static System.Windows.Forms.CheckBox CreatePrimary(
                string text = "",
                bool isChecked = true)
            {
                return UICheckBoxFactory.CreatePrimary(
                    text,
                    isChecked);
            }

            /// <summary>A box without a label, 25 x 25 (for table cells and the like).</summary>
            /// <inheritdoc cref="CreateStandard"/>
            public static System.Windows.Forms.CheckBox CreateStandardCompact(
                bool isChecked = true,
                UIColorSet colors = null)
            {
                return UICheckBoxFactory.CreateStandardCompact(
                    isChecked,
                    colors);
            }

            /// <summary>Like <see cref="CreateStandardCompact"/> but with the box filled in the current accent instead of the neutral gray - the compact counterpart to <see cref="CreatePrimary"/>.</summary>
            public static System.Windows.Forms.CheckBox CreatePrimaryCompact(
                bool isChecked = true)
            {
                return UICheckBoxFactory.CreatePrimaryCompact(
                    isChecked);
            }
        }
    }
}

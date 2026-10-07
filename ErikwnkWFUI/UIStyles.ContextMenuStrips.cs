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
        /// <summary>A <see cref="Controls.ContextMenuStrip"/> themed to match the rest of ErikwnkWFUI instead of the plain OS-default popup menu - functionally identical to the native control.</summary>
        public static class ContextMenuStrips
        {
            /// <summary>A themed context menu; the hovered item is the neutral gray.</summary>
            /// <param name="colors">
            /// A color set for the highlighted item (hover) and the item with an open submenu
            /// (pressed) instead of the neutral gray: one of <c>UIStyles.Colors.Sets</c>,
            /// <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="Color"/>.
            /// </param>
            public static Controls.ContextMenuStrip CreateStandard(UIColorSet colors = null)
            {
                return UIContextMenuStripFactory.CreateStandard(colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but with the highlight in the current accent instead of the neutral gray - mirrors <see cref="Buttons.CreatePrimary"/>.</summary>
            public static Controls.ContextMenuStrip CreatePrimary()
            {
                return UIContextMenuStripFactory.CreatePrimary();
            }
        }
    }
}

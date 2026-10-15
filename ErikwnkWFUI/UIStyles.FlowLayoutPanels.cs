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
        /// A plain, transparent <see cref="FlowLayoutPanel"/> with no padding/margin - a layout helper, not itself themed. It differs
        /// from the standard one in two settings: it is as big as its content (<c>AutoSize</c>) and does not wrap (<c>WrapContents</c> is
        /// off); both can be set again. <c>FlowDirection</c> and <c>AutoScroll</c> (scroll bars in the style of the theme) work as in the
        /// standard control. Right-to-left layouts are not supported.
        /// </summary>
        public static class FlowLayoutPanels
        {
            /// <summary>A transparent flow layout panel that is as big as its content and does not wrap, without margin and padding.</summary>
            /// <param name="colors">A color set for the background (its basic color) instead of none: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>.</param>
            public static FlowLayoutPanel CreateStandard(UIColorSet colors = null)
            {
                return UIFlowLayoutPanelFactory.CreateStandard(colors);
            }
        }
    }
}

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
        /// A <see cref="FlowLayoutPanel"/> with no padding/margin, in the elevated background of the theme or transparent. It differs
        /// from the standard one in two settings: it is as big as its content (<c>AutoSize</c>) and does not wrap (<c>WrapContents</c> is
        /// off); both can be set again. <c>FlowDirection</c> and <c>AutoScroll</c> (scroll bars in the style of the theme) work as in the
        /// standard control. Right-to-left layouts are not supported.
        /// </summary>
        public static class FlowLayoutPanels
        {
            /// <summary>A flow layout panel in the elevated background of the theme (<c>UIColors.BackgroundMediumElevated</c>, the same as <see cref="Panels.CreateElevated"/>) that is as big as its content and does not wrap, without margin and padding. Follows a change of the theme. For a row of controls on a surface, use <see cref="CreateTransparent"/>.</summary>
            /// <param name="colors">A color set for the background (its basic color) instead of the elevated one: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>.</param>
            public static FlowLayoutPanel CreateStandard(UIColorSet colors = null)
            {
                return UIFlowLayoutPanelFactory.CreateStandard(colors);
            }

            /// <summary>A flow layout panel without a background of its own (what lies behind it shows through), otherwise like <see cref="CreateStandard"/>.</summary>
            public static FlowLayoutPanel CreateTransparent()
            {
                return UIFlowLayoutPanelFactory.CreateTransparent();
            }
        }
    }
}

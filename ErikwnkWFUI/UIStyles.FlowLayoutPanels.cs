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
        /// <summary>A plain, transparent <see cref="FlowLayoutPanel"/> with no padding/margin - a layout helper, not itself themed.</summary>
        public static class FlowLayoutPanels
        {
            public static FlowLayoutPanel CreateStandard()
            {
                return UIFlowLayoutPanelFactory.CreateStandard();
            }
        }
    }
}

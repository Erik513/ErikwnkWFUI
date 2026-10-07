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
        /// <summary>Plain <see cref="Panel"/>s pre-filled with one of the theme's background shades - handy as containers/cards.</summary>
        public static class Panels
        {
            public static Panel CreateDark()
            {
                return UIPanelFactory.CreateDark();
            }

            public static Panel CreateMedium()
            {
                return UIPanelFactory.CreateMedium();
            }

            public static Panel CreateElevated()
            {
                return UIPanelFactory.CreateElevated();
            }

            public static Panel CreateTransparent()
            {
                return UIPanelFactory.CreateTransparent();
            }
            public static Panel CreatePrimary()
            {
                return UIPanelFactory.CreatePrimary();
            }
        }
    }
}

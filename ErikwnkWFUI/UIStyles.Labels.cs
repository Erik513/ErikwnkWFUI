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
        /// <summary>Themed <see cref="Label"/>s: <see cref="CreateTitle"/> (bold/large), <see cref="CreateNormal"/> (body text), <see cref="CreateMuted"/> (de-emphasized).</summary>
        public static class Labels
        {
            public static Label CreateTitle(string text = "")
            {
                return UILabelFactory.CreateTitle(text);
            }

            public static Label CreateNormal(string text = "")
            {
                return UILabelFactory.CreateNormal(text);
            }

            public static Label CreateMuted(string text = "")
            {
                return UILabelFactory.CreateMuted(text);
            }
        }
    }
}

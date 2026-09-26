using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A <see cref="System.Windows.Forms.ContextMenuStrip"/> themed to match
    /// the rest of ErikwnkWFUI instead of the plain OS-default popup menu -
    /// same click/keyboard/Items behavior as the native control, only the
    /// rendering changes.
    /// </summary>
    public class ContextMenuStrip : System.Windows.Forms.ContextMenuStrip
    {
        public ContextMenuStrip()
        {
            Renderer = CreateRenderer();
            ShowImageMargin = false;
        }

        /// <summary>
        /// A <see cref="ToolStripMenuItem"/>'s own submenu popup (its
        /// DropDown) is a separate <see cref="ToolStripDropDownMenu"/> the
        /// framework creates lazily and does not inherit this menu's
        /// Renderer - a caller building a submenu needs a fresh instance of
        /// this same renderer for it.
        /// </summary>
        internal static ToolStripRenderer CreateRenderer()
        {
            return new ThemedRenderer();
        }

        private sealed class ThemedRenderer : ToolStripProfessionalRenderer
        {
            public ThemedRenderer() : base(new ThemedColorTable())
            {
                RoundedEdges = false;
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = GetForeColor(e.Item);
                base.OnRenderItemText(e);
            }

            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                e.ArrowColor = GetForeColor(e.Item);
                base.OnRenderArrow(e);
            }

            private static Color GetForeColor(ToolStripItem item)
            {
                if (!item.Enabled)
                    return UIColors.TextDisabled;

                bool highlighted = item.Selected || item.Pressed;
                return highlighted ? UIColors.AccentForeColor : UIColors.TextPrimary;
            }
        }

        // Every color here is read live off UIColors on each access (like
        // the rest of this library's Paint handlers) instead of snapshotted
        // once at construction, so a long-lived menu instance still renders
        // correctly after a theme/accent change.
        private sealed class ThemedColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => UIColors.BackgroundMediumElevated;
            public override Color ImageMarginGradientBegin => UIColors.BackgroundMediumElevated;
            public override Color ImageMarginGradientMiddle => UIColors.BackgroundMediumElevated;
            public override Color ImageMarginGradientEnd => UIColors.BackgroundMediumElevated;

            public override Color MenuBorder => UIColors.BorderMedium;
            public override Color MenuItemBorder => UIColors.Primary;

            public override Color SeparatorDark => UIColors.BorderMedium;
            public override Color SeparatorLight => UIColors.BorderMedium;

            public override Color MenuItemSelectedGradientBegin => UIColors.Primary;
            public override Color MenuItemSelectedGradientEnd => UIColors.Primary;

            public override Color MenuItemPressedGradientBegin => UIColors.PrimaryDark;
            public override Color MenuItemPressedGradientMiddle => UIColors.PrimaryDark;
            public override Color MenuItemPressedGradientEnd => UIColors.PrimaryDark;
        }
    }
}

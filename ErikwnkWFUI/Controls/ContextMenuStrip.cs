using System;
using System.ComponentModel;
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
        private readonly ThemeColor _selectionBackColor = new ThemeColor(() => UIColors.BorderLight);

        public ContextMenuStrip()
        {
            Renderer = CreateRenderer(() => SelectionBackColor);
        }

        /// <summary>
        /// Background of a hovered/selected menu item. Defaults to a fixed,
        /// neutral gray (<see cref="UIColors.BorderLight"/>), not the
        /// current accent - set this explicitly (e.g. to
        /// <see cref="UIColors.Primary"/>) for an accent-colored highlight
        /// instead, matching <see cref="ListBox.SelectedBackColor"/>'s own
        /// CreateStandard/CreatePrimary split.
        /// </summary>
        public Color SelectionBackColor
        {
            get => _selectionBackColor.Value;
            set => _selectionBackColor.Set(value);
        }

        // A ToolStripMenuItem's own submenu popup (its DropDown) is a
        // separate ToolStripDropDownMenu the framework creates lazily and
        // does not inherit this menu's Renderer/ShowImageMargin - re-applied
        // to the whole tree on every Opening rather than once at Items.Add
        // time, since a caller can keep adding submenu items to an
        // already-built menu right up until it's shown.
        protected override void OnOpening(CancelEventArgs e)
        {
            ApplyThemeToSubmenus(Items);
            base.OnOpening(e);
        }

        private void ApplyThemeToSubmenus(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                if (item is ToolStripMenuItem menuItem
                    && menuItem.HasDropDownItems
                    && menuItem.DropDown is ToolStripDropDownMenu dropDown)
                {
                    dropDown.Renderer = CreateRenderer(() => SelectionBackColor);
                    dropDown.ShowImageMargin = ShowImageMargin;

                    ApplyThemeToSubmenus(menuItem.DropDownItems);
                }
            }
        }

        internal static ToolStripRenderer CreateRenderer(Func<Color> selectionBackColor)
        {
            return new ThemedRenderer(selectionBackColor);
        }

        private sealed class ThemedRenderer : ToolStripProfessionalRenderer
        {
            private readonly Func<Color> _selectionBackColor;

            public ThemedRenderer(Func<Color> selectionBackColor) : base(new ThemedColorTable(selectionBackColor))
            {
                _selectionBackColor = selectionBackColor;
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

            private Color GetForeColor(ToolStripItem item)
            {
                if (!item.Enabled)
                    return UIColors.TextDisabled;

                bool highlighted = item.Selected || item.Pressed;
                return highlighted ? UIColors.GetContrastingForeColor(_selectionBackColor()) : UIColors.TextPrimary;
            }
        }

        // Every color here is read live off UIColors (and the owning menu's
        // own SelectionBackColor) on each access - like the rest of this
        // library's Paint handlers - instead of snapshotted once at
        // construction, so a long-lived menu instance still renders
        // correctly after a theme/accent change.
        private sealed class ThemedColorTable : ProfessionalColorTable
        {
            private readonly Func<Color> _selectionBackColor;

            public ThemedColorTable(Func<Color> selectionBackColor)
            {
                _selectionBackColor = selectionBackColor;
            }

            public override Color ToolStripDropDownBackground => UIColors.BackgroundMediumElevated;
            public override Color ImageMarginGradientBegin => UIColors.BackgroundMediumElevated;
            public override Color ImageMarginGradientMiddle => UIColors.BackgroundMediumElevated;
            public override Color ImageMarginGradientEnd => UIColors.BackgroundMediumElevated;

            public override Color MenuBorder => UIColors.BorderMedium;
            public override Color MenuItemBorder => _selectionBackColor();

            public override Color SeparatorDark => UIColors.BorderMedium;
            public override Color SeparatorLight => UIColors.BorderMedium;

            public override Color MenuItemSelectedGradientBegin => _selectionBackColor();
            public override Color MenuItemSelectedGradientEnd => _selectionBackColor();

            public override Color MenuItemPressedGradientBegin => UIColors.Darken(_selectionBackColor(), 30);
            public override Color MenuItemPressedGradientMiddle => UIColors.Darken(_selectionBackColor(), 30);
            public override Color MenuItemPressedGradientEnd => UIColors.Darken(_selectionBackColor(), 30);

            public override Color CheckBackground => UIColors.BackgroundMediumElevated;
            public override Color CheckSelectedBackground => _selectionBackColor();
            public override Color CheckPressedBackground => UIColors.Darken(_selectionBackColor(), 30);
        }
    }
}

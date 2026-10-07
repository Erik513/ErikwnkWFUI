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
        private readonly ThemeColor _selectionBackColor;
        private UIColorSet _colors;

        public ContextMenuStrip()
        {
            // The highlighted item: the set's hover color (brought to where its text reads), or
            // the neutral gray without a set.
            _selectionBackColor = new ThemeColor(() => _colors != null
                ? UIColors.GetReadableFill(_colors.Hover, _colors.HoverText)
                : UIColors.BorderLight);

            Renderer = CreateRenderer();
        }

        /// <summary>
        /// The colors of the menu: the hovered item takes the set's hover color and text, an item
        /// whose submenu is open the pressed ones. Left null, the neutral gray;
        /// <c>UIStyles.ContextMenuStrips.CreatePrimary</c> makes one in the accent color. A
        /// <see cref="SelectionBackColor"/> set explicitly wins over the set's hover color.
        /// </summary>
        public UIColorSet Colors
        {
            get { return _colors; }
            set { _colors = value; }
        }

        /// <summary>
        /// Background of a hovered/selected menu item. Defaults to the hover color of
        /// <see cref="Colors"/>, or without a set to a fixed, neutral gray
        /// (<see cref="UIColors.BorderLight"/>); set this explicitly to use any color. The
        /// menus of the grids, the list view and the tab control set it to follow their own
        /// highlight color.
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
                    dropDown.Renderer = CreateRenderer();
                    dropDown.ShowImageMargin = ShowImageMargin;

                    ApplyThemeToSubmenus(menuItem.DropDownItems);
                }
            }
        }

        // A plain, parameterless factory - nothing outside this class ever
        // needs a renderer for anything other than THIS menu's own
        // SelectionBackColor, so there's no reason for a caller to be able
        // to pass in a different color source.
        private ToolStripRenderer CreateRenderer()
        {
            return new ThemedRenderer(this);
        }

        private sealed class ThemedRenderer : ToolStripProfessionalRenderer
        {
            private readonly ContextMenuStrip _menu;

            public ThemedRenderer(ContextMenuStrip menu) : base(new ThemedColorTable(menu))
            {
                _menu = menu;
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

                if (!highlighted)
                    return UIColors.TextPrimary;

                // The set's own text, on its own fill; an explicit selection color gets the best
                // text for that color.
                if (_menu._colors != null && !_menu._selectionBackColor.IsSet)
                    return item.Pressed ? _menu._colors.PressedText : _menu._colors.HoverText;

                return UIColors.GetContrastingForeColor(_menu.SelectionBackColor);
            }
        }

        // Every color here is read live off UIColors (and the owning menu's
        // own SelectionBackColor) on each access - like the rest of this
        // library's Paint handlers - instead of snapshotted once at
        // construction, so a long-lived menu instance still renders
        // correctly after a theme/accent change.
        private sealed class ThemedColorTable : ProfessionalColorTable
        {
            private readonly ContextMenuStrip _menu;

            public ThemedColorTable(ContextMenuStrip menu)
            {
                _menu = menu;
            }

            private Color Selection => _menu.SelectionBackColor;

            public override Color ToolStripDropDownBackground => UIColors.BackgroundMediumElevated;
            public override Color ImageMarginGradientBegin => UIColors.BackgroundMediumElevated;
            public override Color ImageMarginGradientMiddle => UIColors.BackgroundMediumElevated;
            public override Color ImageMarginGradientEnd => UIColors.BackgroundMediumElevated;

            // BorderLight, not BorderMedium - measured contrast against
            // ToolStripDropDownBackground (BackgroundMediumElevated): 1.95:1
            // vs 1.57:1 in Dark, 1.37:1 vs 1.24:1 in Light (Light theme's
            // BorderLight is, counter to its name, the darkest-valued of
            // the three border roles - see UIThemes.Light).
            public override Color MenuBorder => UIColors.BorderLight;

            public override Color SeparatorDark => UIColors.BorderLight;
            public override Color SeparatorLight => UIColors.BorderLight;

            // Derived from the CURRENT fill rather than a fixed role - a
            // fixed UIColors.BorderLight border coincidentally equals
            // CreateStandard's own default fill (SelectionBackColor also
            // defaults to BorderLight), which made the border invisible
            // again for exactly the most common case. Shifting away from
            // the fill's own brightness (darker if the fill is light enough
            // for dark text, lighter otherwise) keeps the two visually
            // distinct no matter what SelectionBackColor is set to - gray,
            // an accent color, or anything a consumer picks.
            public override Color MenuItemBorder
            {
                get
                {
                    Color fill = Selection;
                    bool fillIsLight = UIColors.IsLight(fill);
                    return fillIsLight ? UIColors.Darken(fill, 40) : UIColors.Lighten(fill, 40);
                }
            }

            public override Color MenuItemSelectedGradientBegin => Selection;
            public override Color MenuItemSelectedGradientEnd => Selection;

            public override Color MenuItemPressedGradientBegin => PressedFill;
            public override Color MenuItemPressedGradientMiddle => PressedFill;
            public override Color MenuItemPressedGradientEnd => PressedFill;

            public override Color CheckBackground => UIColors.BackgroundMediumElevated;
            public override Color CheckSelectedBackground => Selection;
            public override Color CheckPressedBackground => PressedFill;

            // One value backing all four "pressed" roles above: the set's pressed color (readable
            // for its text), else the selection fill darkened.
            private Color PressedFill
            {
                get
                {
                    if (_menu._colors != null && !_menu._selectionBackColor.IsSet)
                        return UIColors.GetReadableFill(_menu._colors.Pressed, _menu._colors.PressedText);

                    return UIColors.Darken(Selection, 30);
                }
            }
        }
    }
}

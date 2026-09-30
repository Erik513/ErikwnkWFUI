using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A themed, display-only <see cref="System.Windows.Forms.TabControl"/>:
    /// the user can switch tabs but not add or close them. Behaves exactly
    /// like the standard control (same <c>TabPages</c>, keyboard handling,
    /// alignment, multiline rows, images, events) - only the drawing is
    /// replaced with the library's own look. <see cref="TabControl"/> builds
    /// on this and adds a right-click menu to add, rename and close tabs.
    /// </summary>
    /// <remarks>
    /// Go through <see cref="UIStyles.TabControls.CreateReadOnlyStandard"/> to
    /// get one. Differences from the standard control: <see cref="Appearance"/>
    /// is always <see cref="TabAppearance.Normal"/> (tabs are never drawn as
    /// buttons). Both customization routes of the standard control still
    /// work: a page's own <c>BackColor</c> is kept (see
    /// <see cref="PageBackColor"/>), and <c>DrawMode = OwnerDrawFixed</c>
    /// hands the drawing of each tab to <c>DrawItem</c>. A tab that sizes
    /// itself comes out a little larger than the standard control's (about
    /// 6 px wider and 1 px taller for a short name), because the native
    /// control lays it out differently once it is painted by the library;
    /// set <c>ItemSize</c> and <c>Padding</c> for exactly the standard sizes.
    /// <c>HotTrack</c> (the hover highlight) is on by default, where the
    /// standard control has it off, since a hover color is part of the theme.
    /// </remarks>
    public partial class ReadOnlyTabControl : System.Windows.Forms.TabControl
    {
        private const int AccentThickness = 2;
        private const int ImageTextGap = 4;

        // Width of the native border band around the page contents, which
        // DisplayRectangle already excludes.
        internal const int NativePageBorder = 4;

        // The native tab strip sits this far in from the control's edges.
        // The page box keeps the same margin so its edge lines up with the
        // outer edge of the first tab instead of sticking out past it.
        internal const int NativeStripMargin = 2;

        private readonly ThemeColor _headerBackColor = new ThemeColor(() => Color.Transparent);
        private readonly ThemeColor _tabBackColor = new ThemeColor(() => UIColors.BackgroundDark);
        private readonly ThemeColor _hoverTabBackColor = new ThemeColor(GetDefaultHoverTabBackColor);
        private readonly ThemeColor _selectedTabBackColor = new ThemeColor(() => UIColors.BackgroundMedium);
        private readonly ThemeColor _tabForeColor = new ThemeColor(() => UIColors.TextSecondary);
        private readonly ThemeColor _selectedTabForeColor = new ThemeColor(() => UIColors.TextPrimary);
        private readonly ThemeColor _disabledTabForeColor = new ThemeColor(() => UIColors.TextDisabled);
        private readonly ThemeColor _accentColor = new ThemeColor(() => UIColors.TextTertiary);
        private readonly ThemeColor _borderColor = new ThemeColor(() => UIColors.BorderMedium);
        private readonly ThemeColor _pageBackColor = new ThemeColor(() => UIColors.BackgroundMedium);

        private const int WM_PARENTNOTIFY = 0x0210;
        private const int WM_HSCROLL = 0x0114;
        private const int WM_VSCROLL = 0x0115;

        private readonly TabScrollButtons _scrollButtons;

        private bool _allowSelectingDisabledTabs = true;

        private int _hoveredTabIndex = -1;

        /// <summary>
        /// Background behind the tabs, where no tab is drawn. Transparent by
        /// default, so the parent shows through and only the tabs and the
        /// page box are visible.
        /// </summary>
        public Color HeaderBackColor
        {
            get => _headerBackColor.Value;
            set
            {
                _headerBackColor.Set(value);
                Repaint();
            }
        }

        /// <summary>Background of a tab that is neither selected nor hovered.</summary>
        public Color TabBackColor
        {
            get => _tabBackColor.Value;
            set
            {
                _tabBackColor.Set(value);
                Repaint();
            }
        }

        /// <summary>
        /// Background of the tab under the mouse. Only used while
        /// <see cref="System.Windows.Forms.TabControl.HotTrack"/> is on - which
        /// it is by default here, unlike the standard control. By default a dark theme lightens a hovered
        /// tab (<see cref="UIColors.BackgroundLight"/>) and a light theme
        /// steps it toward the selected tab from the dark side
        /// (<see cref="UIColors.BackgroundDarkElevated"/>), so a hovered tab
        /// never looks more selected than the selected one.
        /// </summary>
        public Color HoverTabBackColor
        {
            get => _hoverTabBackColor.Value;
            set
            {
                _hoverTabBackColor.Set(value);
                Repaint();
            }
        }

        /// <summary>Background of the selected tab. Defaults to the same color as <see cref="PageBackColor"/> so the tab flows into its page.</summary>
        public Color SelectedTabBackColor
        {
            get => _selectedTabBackColor.Value;
            set
            {
                _selectedTabBackColor.Set(value);
                Repaint();
            }
        }

        /// <summary>Text color of a tab that is not selected.</summary>
        public Color TabForeColor
        {
            get => _tabForeColor.Value;
            set
            {
                _tabForeColor.Set(value);
                Repaint();
            }
        }

        /// <summary>Text color of the selected tab.</summary>
        public Color SelectedTabForeColor
        {
            get => _selectedTabForeColor.Value;
            set
            {
                _selectedTabForeColor.Set(value);
                Repaint();
            }
        }

        /// <summary>Text color of a tab whose page is disabled.</summary>
        public Color DisabledTabForeColor
        {
            get => _disabledTabForeColor.Value;
            set
            {
                _disabledTabForeColor.Set(value);
                Repaint();
            }
        }

        /// <summary>
        /// Color of the bar on the outer edge of the selected tab. Defaults
        /// to a neutral gray (<see cref="UIColors.TextTertiary"/>, which
        /// stands out from the tab in both themes), not the current accent -
        /// set it to <see cref="UIColors.PrimaryLight"/> for an
        /// accent-colored bar, which is what <see cref="UIStyles.TabControls.CreatePrimary"/> does.
        /// </summary>
        public Color SelectedTabIndicatorColor
        {
            get => _accentColor.Value;
            set
            {
                _accentColor.Set(value);
                Repaint();
            }
        }

        /// <summary>Color of the tab outlines and the border around the page area.</summary>
        public Color BorderColor
        {
            get => _borderColor.Value;
            set
            {
                _borderColor.Set(value);
                Repaint();
            }
        }

        /// <summary>
        /// Background color of the pages, applied to pages that don't have a
        /// color of their own - existing ones and ones added later. A page
        /// whose <c>BackColor</c> was set individually keeps it, as with
        /// the standard control.
        /// </summary>
        public Color PageBackColor
        {
            get => _pageBackColor.Value;
            set
            {
                Color previous = _pageBackColor.Value;
                _pageBackColor.Set(value);
                ApplyPageBackColor(previous);
                Repaint();
            }
        }

        /// <summary>
        /// Whether a tab whose page is disabled can be selected. On by
        /// default, like the standard control, where a disabled page can
        /// still be opened (its content is disabled, the tab is not). Off:
        /// a click, the keyboard or code cannot select such a tab, and the
        /// arrow keys and Ctrl+Tab skip over it. A tab that is selected when
        /// its page gets disabled stays selected.
        /// </summary>
        public bool AllowSelectingDisabledTabs
        {
            get => _allowSelectingDisabledTabs;
            set => _allowSelectingDisabledTabs = value;
        }

        /// <summary>
        /// Raised when the right mouse button goes down on a tab - with the
        /// page, its index and where in the control the click was. The
        /// standard control has no such event; it is what a custom menu or
        /// the <see cref="TabControl"/>'s add/close menu builds on.
        /// </summary>
        public event EventHandler<TabRightClickEventArgs> TabRightClick;

        /// <summary>
        /// Always <see cref="TabAppearance.Normal"/> - this control only
        /// draws tabs, never button-style tabs. Setting anything else throws.
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new TabAppearance Appearance
        {
            get => TabAppearance.Normal;
            set
            {
                if (value != TabAppearance.Normal)
                    throw new NotSupportedException("Only TabAppearance.Normal is supported.");
            }
        }

        // The native styles that switch a tab control to button tabs. The
        // base class's own Appearance can still be reached through a
        // reference typed as the standard TabControl, so they are stripped
        // from the window itself as well.
        private const int TCS_BUTTONS = 0x0100;
        private const int TCS_FLATBUTTONS = 0x0008;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.Style &= ~(TCS_BUTTONS | TCS_FLATBUTTONS);
                return parameters;
            }
        }

        // Which way "hovered" goes depends on the surface: on a light theme
        // white would outshine the selected tab, so there it is the tone
        // between a normal and the selected tab.
        private static Color GetDefaultHoverTabBackColor()
        {
            bool lightSurface = UIColors.GetContrastingForeColor(UIColors.BackgroundDark).R < 128;

            return lightSurface ? UIColors.BackgroundDarkElevated : UIColors.BackgroundLight;
        }

        public ReadOnlyTabControl()
        {
            // First, before anything below can resize the control: on the
            // .NET Framework, setting the font makes the tab control resize
            // itself while it is still being constructed.
            _scrollButtons = new TabScrollButtons(this);

            Font = UIFonts.Normal;
            ForeColor = UIColors.TextPrimary;
            HotTrack = true;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            UpdateStyles();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // The scroll arrows are a child window the native control creates
            // on its own, only while the tabs overflow - this is where that
            // creation shows up.
            if (m.Msg == WM_PARENTNOTIFY && IsHandleCreated)
            {
                _scrollButtons.AttachTo(Handle);
            }

            // Scrolling moves every tab, but the native control only repaints
            // the part it uncovers - old tab pixels would be left in the
            // wrong place. So everything is redrawn.
            if ((m.Msg == WM_HSCROLL || m.Msg == WM_VSCROLL) && IsHandleCreated)
            {
                Invalidate();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            _scrollButtons.AttachTo(Handle);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // Not before there is a window - reading Handle would create it.
            if (IsHandleCreated)
            {
                _scrollButtons.AttachTo(Handle);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _scrollButtons.ReleaseHandle();

                if (_arrowToolTipTimer != null)
                {
                    _arrowToolTipTimer.Dispose();
                    _arrowToolTipTimer = null;
                }

                if (_tabToolTip != null)
                {
                    _tabToolTip.Dispose();
                    _tabToolTip = null;
                }
            }

            base.Dispose(disposing);
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);

            if (e.Control is TabPage page)
            {
                ApplyPageBackColor(page);
            }
        }

        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);

            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = GetTabIndexAt(e.Location);

                if (index >= 0)
                {
                    OnTabRightClick(new TabRightClickEventArgs(TabPages[index], index, e.Location));
                }
            }

            base.OnMouseDown(e);
        }

        /// <summary>Raises <see cref="TabRightClick"/>.</summary>
        protected virtual void OnTabRightClick(TabRightClickEventArgs e)
        {
            TabRightClick?.Invoke(this, e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int index = GetTabIndexAt(e.Location);

            SetHoveredTabIndex(HotTrack ? index : -1);
            UpdateTabToolTip(index);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            SetHoveredTabIndex(-1);
            UpdateTabToolTip(-1);
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);

            Repaint();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);

            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);

            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);

            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            Invalidate();
        }

        /// <summary>Redraws the control and its scroll arrows.</summary>
        protected void Repaint()
        {
            Invalidate();
            _scrollButtons.Refresh();
        }

        // A page that still shows the previous page color was never given one
        // of its own, so it follows the change; any other color was chosen
        // for that page and stays.
        private void ApplyPageBackColor(Color previous)
        {
            foreach (TabPage page in TabPages)
            {
                if (page.BackColor == previous)
                {
                    SetPageBackColor(page);
                }
            }
        }

        // A page that was added without a color of its own - it uses the
        // visual-style texture or just inherits this control's BackColor -
        // gets the themed one.
        private void ApplyPageBackColor(TabPage page)
        {
            if (page.UseVisualStyleBackColor || page.BackColor == BackColor)
            {
                SetPageBackColor(page);
            }
        }

        private void SetPageBackColor(TabPage page)
        {
            // The visual-style page background is a themed texture that
            // ignores BackColor entirely.
            page.UseVisualStyleBackColor = false;
            page.BackColor = PageBackColor;
        }

        private void SetHoveredTabIndex(int index)
        {
            if (_hoveredTabIndex == index)
                return;

            _hoveredTabIndex = index;
            Invalidate();
        }

        /// <summary>The index of the tab at a point in the control, or -1 when there is none there.</summary>
        protected int GetTabIndexAt(Point location)
        {
            for (int i = 0; i < TabCount; i++)
            {
                if (GetTabRect(i).Contains(location))
                    return i;
            }

            return -1;
        }

        private bool IsVertical()
        {
            return Alignment == TabAlignment.Left || Alignment == TabAlignment.Right;
        }
    }
}

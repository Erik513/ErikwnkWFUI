using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A themed <see cref="System.Windows.Forms.ComboBox"/>. The rows of the list are drawn by
    /// the library (the selected or hovered one in the neutral gray or in a color set), and so
    /// is the closed box of a drop-down list: border, text and arrow, with a look for hovered,
    /// dropped down, disabled and keyboard focus (a dotted frame once the keyboard is used; a
    /// box that has the focus after a pick with the mouse looks like any other). Get one through
    /// <see cref="UIStyles.ComboBoxes"/>.
    /// </summary>
    /// <remarks>
    /// Everything but the looks is the standard control: items, sorting, binding, selection, the
    /// events (<c>SelectionChangeCommitted</c> too), keyboard and type-ahead, the size of the list
    /// (<c>DropDownWidth</c>, <c>DropDownHeight</c>, <c>MaxDropDownItems</c>) and auto-completion
    /// of the editable style. The list that opens is the system's own window, drawn row by row by
    /// the library; an application can still paint over a row in a <c>DrawItem</c> handler, which
    /// runs after the library's drawing.
    ///
    /// Taken over by the library, and ignored: <c>FlatStyle</c>, <c>RightToLeft</c> and, for the
    /// closed box, the system's own look. <c>DrawMode</c> is set by the library (owner-drawn); a
    /// variable-height mode still works. A <c>DropDownList</c> has its whole closed box drawn by
    /// the library. A <c>DropDown</c> holds a text field (a window of its own): the library draws
    /// the border and the arrow button around it, the field keeps the system's selection color.
    /// A <c>Simple</c> box keeps the system's drawing for the box itself. The list is drawn like
    /// the others in every style, and its outer frame has the border color of the open box.
    /// </remarks>
    // In three files: this one (the colors, the state of the mouse and the keyboard), ComboBox.Drawing.cs (the rows
    // of the list, the closed box) and ComboBox.Native.cs (the windows of the system behind the box).
    public partial class ComboBox : System.Windows.Forms.ComboBox
    {

        // The width of the arrow part at 96 dpi, and the padding of the text.
        private const int ArrowWidth96 = 22;
        private const int TextPadding96 = 6;

        // The off-screen buffer is this box's own. The shared one (BufferedGraphicsManager.Current)
        // is one bitmap for every control of the thread: when two boxes paint close together,
        // one can show up with pixels of the other (the text of the neighbor).
        private readonly BufferedGraphicsContext _bufferContext = new BufferedGraphicsContext();

        private readonly ListFrame _listFrame;
        private bool _isHot;
        private bool _lastInputWasMouse;
        private readonly ColorSetHolder _colorSet = new ColorSetHolder();

        /// <summary>Makes a drop-down list in the library's look; <c>UIStyles.ComboBoxes</c> makes the usual ones.</summary>
        public ComboBox()
        {
            // The colors are read while painting: a change of the theme needs a repaint (the box colors the factory
            // gave follow, see UIComboBoxFactory).
            ThemeWatcher.Attach(this, Invalidate);

            // The list of a dark theme gets the system's dark style, which also gives it dark scroll bars (the plain,
            // unthemed list has light ones).
            NativeControlTheme.Track(this, GetListHandle, handle =>
            {
                if (NativeControlTheme.IsDark)
                    NativeControlTheme.ApplyToScrollBars(handle);
                else
                    SetWindowTheme(handle, "", "");
            });

            _listFrame = new ListFrame(this);

            // The rows are owner-drawn: the list of the system always highlights the hovered
            // row in the system's accent color, no matter what is set. Drawing every row is
            // the only way to make it follow the library's colors.
            FlatStyle = FlatStyle.Flat;
            DrawMode = DrawMode.OwnerDrawFixed;
        }

        /// <summary>
        /// The colors of the box: the selected (hovered) row of the list, and for the closed box
        /// the border when hovered and dropped down (the open list's frame has the latter). Left null, the neutral gray
        /// (<c>UIStyles.Colors.Sets.Gray</c>) for the closed box and the gray of the selection in
        /// other controls for the row; <c>UIStyles.ComboBoxes.CreatePrimary</c> makes one in the
        /// accent color.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIColorSet Colors
        {
            get { return ResolvedColors; }
            set
            {
                _colorSet.Value = value;
                Invalidate();
            }
        }

        private UIColorSet ResolvedColors
        {
            get { return _colorSet.Value; }
        }

        // A set that is asked for again each time (the primary set, which follows the accent).
        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorSet.Follow(source);
            Invalidate();
        }

        // The whole closed box is painted by the library only for a drop-down list; the styles
        // with a text field keep that field the system's.
        internal bool OwnsClosedPainting
        {
            get { return DropDownStyle == ComboBoxStyle.DropDownList && !DesignMode; }
        }

        // The editable style holds a text field (a child window that draws itself); the library
        // paints what is around it: the border and the arrow button.
        internal bool OwnsEditableFramePainting
        {
            get { return DropDownStyle == ComboBoxStyle.DropDown && !DesignMode; }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _bufferContext.Dispose();
                DisposeClickWatcher();
            }

            base.Dispose(disposing);
        }

        private UIColorSet GetColors()
        {
            return ResolvedColors ?? UIStyles.Colors.Sets.Gray;
        }

        private int Scale(int value96)
        {
            return Dpi.Scale(this, value96);
        }

        // ---- the states that change the look ----

        /// <inheritdoc/>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHot = true;
            Invalidate();
        }

        /// <inheritdoc/>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHot = false;
            Invalidate();
        }

        /// <inheritdoc/>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _lastInputWasMouse = true;
            Invalidate();
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            _lastInputWasMouse = false;
            Invalidate();
        }

        /// <inheritdoc/>
        protected override void OnDropDown(EventArgs e)
        {
            base.OnDropDown(e);
            Invalidate();
        }

        // While the list was open the mouse was over the list, not the box, and the box got
        // no MouseLeave: after a pick it would still look hovered until the mouse came back
        // and left. So the hover is worked out again from where the mouse is now.
        /// <inheritdoc/>
        protected override void OnDropDownClosed(EventArgs e)
        {
            base.OnDropDownClosed(e);
            _isHot = IsMouseOver();
            Invalidate();
        }

        private bool IsMouseOver()
        {
            return IsHandleCreated && ClientRectangle.Contains(PointToClient(MousePosition));
        }

        /// <inheritdoc/>
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);

            // Arriving with a mouse button down is a click; otherwise the keyboard brought it.
            _lastInputWasMouse = MouseButtons != MouseButtons.None;
            Invalidate();
        }

        /// <inheritdoc/>
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        /// <inheritdoc/>
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            // A disabled control gets no MouseLeave: whether the mouse is over the box is looked at again
            // when it is enabled, so it does not look hovered with the mouse somewhere else.
            _isHot = Enabled && IsMouseOver();
            Invalidate();
        }

        /// <inheritdoc/>
        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }

        /// <inheritdoc/>
        protected override void OnDropDownStyleChanged(EventArgs e)
        {
            base.OnDropDownStyleChanged(e);
            Invalidate();
        }

        // Hidden, a box gets no MouseLeave: shown again it must not still look hovered.
        /// <inheritdoc/>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            _isHot = Visible && Enabled && IsMouseOver();
        }
    }
}

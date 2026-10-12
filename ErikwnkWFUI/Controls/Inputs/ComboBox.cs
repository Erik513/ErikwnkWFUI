using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Native;
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
    public class ComboBox : System.Windows.Forms.ComboBox
    {
        private const int WM_ERASEBKGND = 0x0014;
        private const int WM_NCPAINT = 0x0085;
        private const int WM_CTLCOLOREDIT = 0x0133;
        private const int WM_CTLCOLORSTATIC = 0x0138;
        private const int WM_PRINT = 0x0317;
        private const int WM_PRINTCLIENT = 0x0318;
        private const int PRF_CLIENT = 0x00000004;

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
        private UIColorSet _colors;

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
        public UIColorSet Colors
        {
            get { return ResolvedColors; }
            set
            {
                _colors = value;
                _colorsSource = null;
                Invalidate();
            }
        }

        // A set that is asked for again each time (the primary set, which follows the accent).
        private Func<UIColorSet> _colorsSource;

        private UIColorSet ResolvedColors
        {
            get { return _colorsSource?.Invoke() ?? _colors; }
        }

        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorsSource = source;
            _colors = null;
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

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Even flat, Windows paints a themed focus and hover border in the system's accent
            // around the box; it comes from the visual-styles engine and ignores every color
            // property. Detaching the control from its theme class stops it. The list that
            // opens is a window of its own (its handle comes from GetComboBoxInfo) with the
            // same themed border, which is painted over below; comctl32 creates it with the
            // box, so it is there already.
            // Done on every new handle, not once: the handle can be recreated.
            SetWindowTheme(Handle, "", "");

            NativeComboBoxInfo info = new NativeComboBoxInfo { cbSize = Marshal.SizeOf(typeof(NativeComboBoxInfo)) };

            if (GetComboBoxInfo(Handle, ref info) && info.hwndList != IntPtr.Zero)
            {
                // The list's own frame is the system's (blue in the dark style); the box's
                // border color is drawn over it. A Simple box shows its list all the time as
                // part of the box, which keeps the system's drawing.
                if (DropDownStyle != ComboBoxStyle.Simple)
                    _listFrame.AssignHandle(info.hwndList);
            }
        }

        private IntPtr GetListHandle()
        {
            NativeComboBoxInfo info = new NativeComboBoxInfo { cbSize = Marshal.SizeOf(typeof(NativeComboBoxInfo)) };

            return GetComboBoxInfo(Handle, ref info) ? info.hwndList : IntPtr.Zero;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _listFrame.ReleaseHandle();
            base.OnHandleDestroyed(e);
        }

        // Watches the window of the open list and paints its outer edge after the system did.
        private sealed class ListFrame : NativeWindow
        {
            private readonly ComboBox _owner;

            public ListFrame(ComboBox owner)
            {
                _owner = owner;
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                if (m.Msg != WM_NCPAINT)
                    return;

                NativeRect rect;
                IntPtr hdc = GetWindowDC(Handle);

                if (hdc == IntPtr.Zero || !GetWindowRect(Handle, out rect))
                    return;

                try
                {
                    using (Graphics g = Graphics.FromHdc(hdc))
                    using (Pen pen = new Pen(_owner.ListBorderColor))
                        g.DrawRectangle(pen, 0, 0, rect.Right - rect.Left - 1, rect.Bottom - rect.Top - 1);
                }
                finally
                {
                    ReleaseDC(Handle, hdc);
                }
            }
        }

        // The list's frame has the color the box shows while the list is open.
        internal Color ListBorderColor
        {
            get
            {
                if (!Enabled)
                    return UIColors.DisabledGray;

                return UIColors.EnsureContrast(GetColors().Pressed, BackColor, 6.0);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _bufferContext.Dispose();

            base.Dispose(disposing);
        }

        // ---- the rows of the list ----

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            // On a focus change the system repaints the selection field of the closed box
            // itself, asking for it as an item (state ComboBoxEdit) and not through WM_PAINT.
            // Drawn as a row it came out a few pixels off, and the text of a box that just lost
            // the focus jumped for a moment. The closed box is drawn the one way instead.
            if (OwnsClosedPainting && (e.State & DrawItemState.ComboBoxEdit) != 0)
            {
                PaintBuffered(e.Graphics);

                return;
            }

            if (e.Index >= 0)
                DrawRow(e);

            base.OnDrawItem(e);
        }

        private void DrawRow(DrawItemEventArgs e)
        {
            // ComboBoxEdit marks the box's own display area (redrawn whenever the control
            // has the focus) as opposed to a row inside the open list - without excluding it
            // the closed box would stay highlighted the whole time it has the focus.
            bool isDisplayArea = (e.State & DrawItemState.ComboBoxEdit) != 0;
            bool isSelected = !isDisplayArea && (e.State & DrawItemState.Selected) != 0;

            // The selected (or hovered) row: the set's color with the set's text, or the
            // neutral gray of the other controls' selections without a set.
            Color backColor = !isSelected
                ? (isDisplayArea ? BackColor : InputField.Background)
                : ResolvedColors != null ? UIColors.GetReadableFill(ResolvedColors.Hover, ResolvedColors.HoverText) : UIColors.BorderLight;

            // A disabled box draws its text muted.
            Color textColor = !Enabled
                ? UIColors.GetDisabledText(backColor)
                : isSelected
                    ? (ResolvedColors != null ? ResolvedColors.HoverText : UIColors.GetContrastingForeColor(backColor))
                    : UIColors.TextPrimary;

            using (SolidBrush backBrush = new SolidBrush(backColor))
                e.Graphics.FillRectangle(backBrush, e.Bounds);

            // GetItemText, not ToString: it respects DisplayMember like the native box does.
            object item = e.Index < Items.Count ? Items[e.Index] : null;

            TextRenderer.DrawText(
                e.Graphics,
                GetItemText(item),
                Font,
                e.Bounds,
                textColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }

        // ---- the closed box ----

        protected override void WndProc(ref Message m)
        {
            if (OwnsClosedPainting || OwnsEditableFramePainting)
            {
                if (m.Msg == WM_ERASEBKGND)
                {
                    m.Result = (IntPtr)1;

                    return;
                }

                if (m.Msg == WM_PAINT)
                {
                    NativePaintStruct paint;
                    IntPtr hdc = BeginPaint(Handle, out paint);

                    try
                    {
                        PaintBuffered(hdc);
                    }
                    finally
                    {
                        EndPaint(Handle, ref paint);
                    }

                    return;
                }

                // DrawToBitmap and printing ask the control to draw itself into a given DC.
                if (m.Msg == WM_PRINTCLIENT || (m.Msg == WM_PRINT && ((long)m.LParam & PRF_CLIENT) != 0))
                {
                    PaintBuffered(m.WParam);

                    return;
                }
            }

            // The text field of a disabled box asks for its colors as a "static" control, which
            // the system answers with white; it gets the box's colors with muted text instead.
            if (m.Msg == WM_CTLCOLORSTATIC && OwnsEditableFramePainting)
            {
                m.Msg = WM_CTLCOLOREDIT;
                base.WndProc(ref m);
                SetTextColor(m.WParam, ColorTranslator.ToWin32(Enabled ? ForeColor : UIColors.GetDisabledText(BackColor)));

                return;
            }

            base.WndProc(ref m);
        }

        // Drawn into an off-screen buffer and put on the screen in one go: drawing the fill and
        // then the text straight onto the window showed as flicker whenever the box repainted
        // (while items were loaded, say). The buffer is a compatible bitmap, so the text keeps
        // its ClearType, and every pixel of it is painted before it is shown.
        private void PaintBuffered(IntPtr hdc)
        {
            using (Graphics target = Graphics.FromHdc(hdc))
                PaintBuffered(target);
        }

        private void PaintBuffered(Graphics target)
        {
            bool isEditable = OwnsEditableFramePainting;

            using (BufferedGraphics buffer = _bufferContext.Allocate(target, ClientRectangle))
            {
                PaintClosed(buffer.Graphics, !isEditable);

                // The text field is a window of its own on top: never painted over.
                if (isEditable)
                    target.SetClip(GetTextFieldBounds(), CombineMode.Exclude);

                buffer.Render(target);
            }
        }

        private Rectangle GetTextFieldBounds()
        {
            NativeComboBoxInfo info = new NativeComboBoxInfo { cbSize = Marshal.SizeOf(typeof(NativeComboBoxInfo)) };

            if (!GetComboBoxInfo(Handle, ref info) || info.hwndEdit == IntPtr.Zero)
                return Rectangle.Empty;

            return Rectangle.FromLTRB(info.rcItem.Left, info.rcItem.Top, info.rcItem.Right, info.rcItem.Bottom);
        }

        private UIColorSet GetColors()
        {
            return ResolvedColors ?? UIStyles.Colors.Sets.Gray;
        }

        private int Scale(int value96)
        {
            return (int)Math.Round(value96 * DeviceDpi / 96.0);
        }

        // The border in the current state: the 3:1 outline at rest; hovered the set's color,
        // dropped down the set's stronger one - each brought to its contrast against the box,
        // so the state is seen as a stronger border in both themes. The focus does not change
        // it: after a pick with the mouse the box keeps the focus and must look like any other.
        internal Color BorderColor
        {
            get
            {
                if (!Enabled)
                    return UIColors.DisabledGray;

                UIColorSet colors = GetColors();

                if (DroppedDown)
                    return ListBorderColor;

                if (_isHot)
                    return UIColors.EnsureContrast(colors.Hover, BackColor, 4.5);

                return UIColors.GetOutlineColor(BackColor);
            }
        }

        private void PaintClosed(Graphics g, bool withText = true)
        {
            Rectangle bounds = ClientRectangle;

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            using (SolidBrush back = new SolidBrush(BackColor))
                g.FillRectangle(back, bounds);

            bool isActive = Enabled && (DroppedDown || _isHot);
            Color border = BorderColor;
            int borderWidth = isActive ? Scale(2) : 1;

            using (Pen pen = new Pen(border, borderWidth))
            {
                float half = borderWidth / 2f;
                g.DrawRectangle(pen, half, half, bounds.Width - borderWidth, bounds.Height - borderWidth);
            }

            // The layout uses the widest border, whatever the state: the text and the arrow must
            // not move when the border gets thicker on hover.
            int inset = Scale(2);
            int arrowWidth = Scale(ArrowWidth96);
            Rectangle arrowArea = new Rectangle(bounds.Right - arrowWidth - inset, bounds.Top, arrowWidth, bounds.Height);
            Color textColor = Enabled ? ForeColor : UIColors.GetDisabledText(BackColor);
            Color arrowColor = !Enabled ? UIColors.DisabledGray : isActive ? border : textColor;

            DrawArrow(g, arrowArea, arrowColor);

            int padding = Scale(TextPadding96);
            Rectangle textArea = new Rectangle(
                bounds.Left + inset + padding,
                bounds.Top + inset,
                Math.Max(0, arrowArea.Left - bounds.Left - inset - padding),
                bounds.Height - inset * 2);

            if (!withText)
                return;

            string text = SelectedIndex >= 0 ? GetItemText(SelectedItem) : "";

            if (!string.IsNullOrEmpty(text))
            {
                TextRenderer.DrawText(
                    g,
                    text,
                    Font,
                    textArea,
                    textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            }

            // The dotted frame of the keyboard focus, around the text - only while the keyboard is
            // what is being used: a box that keeps the focus after a pick with the mouse does not
            // show it.
            if (Focused && ShowFocusCues && !_lastInputWasMouse)
            {
                int textWidth = string.IsNullOrEmpty(text) ? 0 : Math.Min(textArea.Width, TextRenderer.MeasureText(text, Font).Width);
                Rectangle frame = new Rectangle(textArea.Left - 2, textArea.Top + 2, textWidth + 4, textArea.Height - 4);
                ControlPaint.DrawFocusRectangle(g, frame, textColor, BackColor);
            }
        }

        // A small chevron pointing down, in the middle of the arrow area.
        private void DrawArrow(Graphics g, Rectangle area, Color color)
        {
            float width = Scale(8);
            float height = Scale(4);
            float cx = area.Left + area.Width / 2f;
            float cy = area.Top + area.Height / 2f;

            SmoothingMode previous = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (Pen pen = new Pen(color, Scale(2)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                g.DrawLines(pen, new[]
                {
                    new PointF(cx - width / 2f, cy - height / 2f),
                    new PointF(cx, cy + height / 2f),
                    new PointF(cx + width / 2f, cy - height / 2f)
                });
            }

            g.SmoothingMode = previous;
        }

        // ---- the states that change the look ----

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHot = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHot = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _lastInputWasMouse = true;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            _lastInputWasMouse = false;
            Invalidate();
        }

        protected override void OnDropDown(EventArgs e)
        {
            base.OnDropDown(e);
            Invalidate();
        }

        // While the list was open the mouse was over the list, not the box, and the box got
        // no MouseLeave: after a pick it would still look hovered until the mouse came back
        // and left. So the hover is worked out again from where the mouse is now.
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

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);

            // Arriving with a mouse button down is a click; otherwise the keyboard brought it.
            _lastInputWasMouse = MouseButtons != MouseButtons.None;
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }

        protected override void OnDropDownStyleChanged(EventArgs e)
        {
            base.OnDropDownStyleChanged(e);
            Invalidate();
        }

        // Hidden, a box gets no MouseLeave: shown again it must not still look hovered.
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!Visible)
                _isHot = false;
        }
    }
}

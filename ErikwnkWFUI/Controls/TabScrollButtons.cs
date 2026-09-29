using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// Takes over the drawing of the little scroll arrows a native tab
    /// control shows when its tabs don't fit in one row. The arrows stay
    /// the native up-down control (so clicking, holding and scrolling behave
    /// exactly as before) - only its painting is replaced, so it matches the
    /// <see cref="TabControl"/> it belongs to.
    /// </summary>
    internal sealed class TabScrollButtons : NativeWindow
    {
        internal enum Part
        {
            None,
            First,
            Second
        }

        private const string ClassName = "msctls_updown32";

        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const int WM_PAINT = 0x000F;
        private const int WM_ERASEBKGND = 0x0014;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_CAPTURECHANGED = 0x0215;
        private const int WM_MOUSELEAVE = 0x02A3;
        private const int WM_PRINTCLIENT = 0x0318;

        private const int GWL_STYLE = -16;
        private const int UDS_HORZ = 0x0040;
        private const int UDM_GETRANGE32 = 0x0470;
        private const int UDM_GETPOS32 = 0x0472;
        private const int TCM_ADJUSTRECT = 0x1328;

        private const int TME_LEAVE = 0x0002;

        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_NOACTIVATE = 0x0010;

        private readonly TabControl _owner;

        private Part _hotPart;
        private Part _pressedPart;
        private bool _isTrackingMouse;

        public TabScrollButtons(TabControl owner)
        {
            _owner = owner;
        }

        // Finds the tab control's up-down child, if it has one right now
        // (it only exists while the tabs overflow) and starts drawing it.
        public void AttachTo(IntPtr tabControlHandle)
        {
            if (Handle != IntPtr.Zero)
                return;

            IntPtr child = FindWindowEx(tabControlHandle, IntPtr.Zero, ClassName, null);

            if (child != IntPtr.Zero)
            {
                AssignHandle(child);
                MoveToOwnerPosition();
            }
        }

        // Where the arrows currently are, in the tab control's client
        // coordinates - empty while there are none or they're hidden.
        public Rectangle GetBounds()
        {
            if (Handle == IntPtr.Zero || !IsWindowVisible(Handle))
                return Rectangle.Empty;

            RECT rect;
            GetWindowRect(Handle, out rect);

            return _owner.RectangleToClient(Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom));
        }

        public void Refresh()
        {
            if (Handle != IntPtr.Zero)
            {
                InvalidateRect(Handle, IntPtr.Zero, false);
            }
        }

        // Which half of the button pair a point is over: the first (left or
        // top) or the second (right or bottom).
        internal static Part GetPartAt(Point location, Size size, bool horizontal)
        {
            if (location.X < 0 || location.Y < 0 || location.X >= size.Width || location.Y >= size.Height)
                return Part.None;

            bool inFirstHalf = horizontal
                ? location.X < size.Width / 2
                : location.Y < size.Height / 2;

            return inFirstHalf ? Part.First : Part.Second;
        }

        // The native control places the arrows a pixel or two off from the
        // strip and past the page box's edge - every move it makes is
        // corrected to the position the owner wants instead.
        private void MoveToOwnerPosition()
        {
            RECT rect;
            GetWindowRect(Handle, out rect);
            Point? location = GetDesiredLocation(new Size(rect.Right - rect.Left, rect.Bottom - rect.Top));

            if (location.HasValue)
            {
                SetWindowPos(Handle, IntPtr.Zero, location.Value.X, location.Value.Y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
            }
        }

        // In the strip's last stretch, flush with the page box's outer edge
        // and the row of tabs. Null for the vertical alignments, where the
        // native position is kept. Measured natively rather than through
        // the managed ClientRectangle/DisplayRectangle: the native control
        // moves the arrows while it is still handling a resize, when those
        // managed values haven't caught up yet.
        private Point? GetDesiredLocation(Size size)
        {
            if (_owner.Alignment != TabAlignment.Top && _owner.Alignment != TabAlignment.Bottom)
                return null;

            IntPtr tabs = GetParent(Handle);
            RECT client;
            GetClientRect(tabs, out client);

            RECT display = client;
            SendMessage(tabs, TCM_ADJUSTRECT, IntPtr.Zero, ref display);

            int right = client.Right - TabControl.NativeStripMargin;

            if (_owner.Alignment == TabAlignment.Top)
            {
                int pageTop = Math.Max(TabControl.NativeStripMargin, display.Top - TabControl.NativePageBorder);
                return new Point(right - size.Width, pageTop + 1 - size.Height);
            }

            int pageBottom = Math.Min(client.Bottom - TabControl.NativeStripMargin, display.Bottom + TabControl.NativePageBorder);
            return new Point(right - size.Width, pageBottom - 1);
        }

        private void CorrectPosition(ref Message m)
        {
            WINDOWPOS position = (WINDOWPOS)Marshal.PtrToStructure(m.LParam, typeof(WINDOWPOS));

            if ((position.flags & SWP_NOMOVE) != 0)
                return;

            RECT rect;
            GetWindowRect(Handle, out rect);
            Size size = (position.flags & SWP_NOSIZE) != 0
                ? new Size(rect.Right - rect.Left, rect.Bottom - rect.Top)
                : new Size(position.cx, position.cy);

            Point? location = GetDesiredLocation(size);

            if (!location.HasValue)
                return;

            position.x = location.Value.X;
            position.y = location.Value.Y;
            Marshal.StructureToPtr(position, m.LParam, false);
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_WINDOWPOSCHANGING:
                    CorrectPosition(ref m);
                    break;

                case WM_ERASEBKGND:
                    m.Result = (IntPtr)1;
                    return;

                case WM_PAINT:
                    PaintWindow();
                    return;

                case WM_PRINTCLIENT:
                    PaintToDeviceContext(m.WParam);
                    return;

                case WM_MOUSEMOVE:
                    UpdateHotPart(m.LParam);
                    break;

                case WM_LBUTTONDOWN:
                    _pressedPart = GetPartAtMessagePoint(m.LParam);
                    Refresh();
                    break;

                case WM_LBUTTONUP:
                case WM_CAPTURECHANGED:
                    _pressedPart = Part.None;
                    Refresh();
                    break;

                case WM_MOUSELEAVE:
                    _isTrackingMouse = false;
                    _hotPart = Part.None;
                    Refresh();
                    break;
            }

            base.WndProc(ref m);

            // The click that just ran is what scrolls the tabs, so the
            // arrow that just reached its end has to be redrawn disabled.
            if (m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONUP)
            {
                Refresh();
            }
        }

        private bool IsHorizontal()
        {
            return (GetWindowLong(Handle, GWL_STYLE) & UDS_HORZ) != 0;
        }

        private Size GetClientSize()
        {
            RECT rect;
            GetClientRect(Handle, out rect);
            return new Size(rect.Right, rect.Bottom);
        }

        private Part GetPartAtMessagePoint(IntPtr lParam)
        {
            Point location = new Point((short)(lParam.ToInt64() & 0xFFFF), (short)((lParam.ToInt64() >> 16) & 0xFFFF));
            return GetPartAt(location, GetClientSize(), IsHorizontal());
        }

        private void UpdateHotPart(IntPtr lParam)
        {
            Part part = GetPartAtMessagePoint(lParam);

            if (!_isTrackingMouse)
            {
                TRACKMOUSEEVENT track = new TRACKMOUSEEVENT
                {
                    cbSize = Marshal.SizeOf(typeof(TRACKMOUSEEVENT)),
                    dwFlags = TME_LEAVE,
                    hwndTrack = Handle
                };

                _isTrackingMouse = TrackMouseEvent(ref track);
            }

            if (part == _hotPart)
                return;

            _hotPart = part;
            Refresh();
        }

        private void PaintWindow()
        {
            PAINTSTRUCT paint;
            IntPtr hdc = BeginPaint(Handle, out paint);

            try
            {
                PaintToDeviceContext(hdc);
            }
            finally
            {
                EndPaint(Handle, ref paint);
            }
        }

        private void PaintToDeviceContext(IntPtr hdc)
        {
            if (hdc == IntPtr.Zero)
                return;

            Size size = GetClientSize();

            if (size.Width <= 0 || size.Height <= 0)
                return;

            using (Graphics graphics = Graphics.FromHdc(hdc))
            {
                Draw(graphics, size);
            }
        }

        private void Draw(Graphics graphics, Size size)
        {
            bool horizontal = IsHorizontal();
            bool canGoBack;
            bool canGoForward;
            GetScrollState(out canGoBack, out canGoForward);

            Rectangle first;
            Rectangle second;

            if (horizontal)
            {
                int half = size.Width / 2;
                first = new Rectangle(0, 0, half, size.Height);
                second = new Rectangle(half, 0, size.Width - half, size.Height);
            }
            else
            {
                int half = size.Height / 2;
                first = new Rectangle(0, 0, size.Width, half);
                second = new Rectangle(0, half, size.Width, size.Height - half);
            }

            DrawButton(graphics, first, Part.First, canGoBack, horizontal ? ArrowDirection.Left : ArrowDirection.Up);
            DrawButton(graphics, second, Part.Second, canGoForward, horizontal ? ArrowDirection.Right : ArrowDirection.Down);

            using (Pen pen = new Pen(_owner.BorderColor))
            {
                graphics.DrawRectangle(pen, 0, 0, size.Width - 1, size.Height - 1);

                if (horizontal)
                    graphics.DrawLine(pen, second.Left, 0, second.Left, size.Height - 1);
                else
                    graphics.DrawLine(pen, 0, second.Top, size.Width - 1, second.Top);
            }
        }

        private void DrawButton(Graphics graphics, Rectangle bounds, Part part, bool enabled, ArrowDirection direction)
        {
            bool pressed = enabled && _pressedPart == part;
            bool hot = enabled && _hotPart == part;

            Color back = pressed
                ? _owner.SelectedTabBackColor
                : hot ? _owner.HoverTabBackColor : _owner.TabBackColor;

            Color arrow = !enabled
                ? _owner.DisabledTabForeColor
                : (hot || pressed) ? _owner.SelectedTabForeColor : _owner.TabForeColor;

            using (SolidBrush brush = new SolidBrush(back))
            {
                graphics.FillRectangle(brush, bounds);
            }

            DrawArrow(graphics, bounds, arrow, direction);
        }

        private static void DrawArrow(Graphics graphics, Rectangle bounds, Color color, ArrowDirection direction)
        {
            const int Size = 3;

            float centerX = bounds.Left + bounds.Width / 2f;
            float centerY = bounds.Top + bounds.Height / 2f;
            PointF[] points;

            switch (direction)
            {
                case ArrowDirection.Left:
                    points = new[] { new PointF(centerX + Size / 2f, centerY - Size), new PointF(centerX + Size / 2f, centerY + Size), new PointF(centerX - Size / 2f, centerY) };
                    break;

                case ArrowDirection.Right:
                    points = new[] { new PointF(centerX - Size / 2f, centerY - Size), new PointF(centerX - Size / 2f, centerY + Size), new PointF(centerX + Size / 2f, centerY) };
                    break;

                case ArrowDirection.Up:
                    points = new[] { new PointF(centerX - Size, centerY + Size / 2f), new PointF(centerX + Size, centerY + Size / 2f), new PointF(centerX, centerY - Size / 2f) };
                    break;

                default:
                    points = new[] { new PointF(centerX - Size, centerY - Size / 2f), new PointF(centerX + Size, centerY - Size / 2f), new PointF(centerX, centerY + Size / 2f) };
                    break;
            }

            SmoothingMode previous = graphics.SmoothingMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (SolidBrush brush = new SolidBrush(color))
            {
                graphics.FillPolygon(brush, points);
            }

            graphics.SmoothingMode = previous;
        }

        // The native control keeps its position as "how many tabs are
        // scrolled out of view" - at the start the back arrow is spent, at
        // the end the forward one is.
        private void GetScrollState(out bool canGoBack, out bool canGoForward)
        {
            IntPtr range = Marshal.AllocHGlobal(2 * sizeof(int));

            try
            {
                SendMessage(Handle, UDM_GETRANGE32, range, range + sizeof(int));
                int minimum = Marshal.ReadInt32(range);
                int maximum = Marshal.ReadInt32(range, sizeof(int));
                int position = SendMessage(Handle, UDM_GETPOS32, IntPtr.Zero, IntPtr.Zero).ToInt32();

                canGoBack = position > minimum;
                canGoForward = position < maximum;
            }
            finally
            {
                Marshal.FreeHGlobal(range);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TRACKMOUSEEVENT
        {
            public int cbSize;
            public int dwFlags;
            public IntPtr hwndTrack;
            public int dwHoverTime;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, int flags);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int index);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, ref RECT lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr rect, bool erase);

        [DllImport("user32.dll")]
        private static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT paint);

        [DllImport("user32.dll")]
        private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT paint);

        [DllImport("user32.dll")]
        private static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT track);
    }
}

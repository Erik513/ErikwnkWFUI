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

        private const int TME_LEAVE = 0x0002;

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
            }
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

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
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
        private static extern int GetWindowLong(IntPtr hWnd, int index);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

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

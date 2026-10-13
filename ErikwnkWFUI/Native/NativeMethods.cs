using System;
using System.Runtime.InteropServices;

namespace ErikwnkWFUI.Native
{
    /// <summary>
    /// The Win32 functions, structs and message numbers the library uses,
    /// declared once. Pull them in with <c>using static</c>.
    /// </summary>
    internal static class NativeMethods
    {
        // ---- window messages ----
        public const int WM_PAINT = 0x000F;
        public const int WM_CANCELMODE = 0x001F;
        public const int WM_NCLBUTTONDOWN = 0x00A1;
        public const int WM_NCRBUTTONDOWN = 0x00A4;
        public const int WM_HSCROLL = 0x0114;
        public const int WM_VSCROLL = 0x0115;
        public const int WM_SETCURSOR = 0x0020;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_MBUTTONDOWN = 0x0207;
        public const int WM_MOUSEWHEEL = 0x020A;
        public const int WM_XBUTTONDOWN = 0x020B;
        public const int WM_CAPTURECHANGED = 0x0215;
        public const int WM_ERASEBKGND = 0x0014;
        public const int WM_WINDOWPOSCHANGING = 0x0046;
        public const int WM_WINDOWPOSCHANGED = 0x0047;
        public const int WM_NCCALCSIZE = 0x0083;
        public const int WM_NCPAINT = 0x0085;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_CTLCOLOREDIT = 0x0133;
        public const int WM_CTLCOLORSTATIC = 0x0138;
        public const int WM_PARENTNOTIFY = 0x0210;
        public const int WM_MOUSELEAVE = 0x02A3;
        public const int WM_PRINT = 0x0317;
        public const int WM_PRINTCLIENT = 0x0318;

        // SystemParametersInfo: whether the animations in windows are on (Windows settings, Accessibility).
        public const uint SPI_GETCLIENTAREAANIMATION = 0x1042;

        // Messages of the application itself start here.
        public const int WM_APP = 0x8000;

        // ---- list view and its header ----
        public const int LVM_FIRST = 0x1000;
        public const int LVM_APPROXIMATEVIEWRECT = LVM_FIRST + 64;
        public const int LVM_SETEXTENDEDLISTVIEWSTYLE = LVM_FIRST + 54;
        public const int LVM_GETHEADER = LVM_FIRST + 31;
        public const int LVM_DELETEITEM = LVM_FIRST + 8;
        public const int LVM_DELETEALLITEMS = LVM_FIRST + 9;
        public const int LVM_DELETECOLUMN = LVM_FIRST + 28;
        public const int LVM_INSERTITEMW = LVM_FIRST + 77;
        public const int LVM_SETCOLUMNW = LVM_FIRST + 96;
        public const int LVM_INSERTCOLUMNW = LVM_FIRST + 97;
        public const int LVS_EX_DOUBLEBUFFER = 0x00010000;
        public const int HDM_FIRST = 0x1200;
        public const int HDM_HITTEST = HDM_FIRST + 6;
        public const int HDM_GETITEMRECT = HDM_FIRST + 7;
        public const int HDS_NOSIZING = 0x0800;

        // The parts of a header that HDM_HITTEST answers with: on the divider between two columns, or on the divider of
        // a column of width 0.
        public const uint HHT_ONDIVIDER = 0x0004;
        public const uint HHT_ONDIVOPEN = 0x0008;

        // ---- window styles ----
        public const int GWL_STYLE = -16;
        public const int WS_HSCROLL = 0x00100000;
        public const int WS_VSCROLL = 0x00200000;

        // ---- SetWindowPos, RedrawWindow and WM_PRINT flags ----
        public const int SWP_NOSIZE = 0x0001;
        public const int SWP_NOMOVE = 0x0002;
        public const int SWP_NOZORDER = 0x0004;
        public const int SWP_NOACTIVATE = 0x0010;
        public const int SWP_FRAMECHANGED = 0x0020;
        public const uint RDW_INVALIDATE = 0x0001;
        public const uint RDW_ERASE = 0x0004;
        public const uint RDW_UPDATENOW = 0x0100;
        public const uint RDW_FRAME = 0x0400;
        public const int PRF_NONCLIENT = 0x0002;
        public const int PRF_CLIENT = 0x0004;

        // ---- window corners (Windows 11) ----
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWCP_ROUND = 2;
        public const int DWMWCP_ROUNDSMALL = 3;

        // ---- user32: messages ----
        // Reads a setting of Windows; the value is a BOOL here (0 or 1).
        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(uint action, uint parameter, ref int value, uint winIni);

        // Puts a message in the queue of the window and returns at once (from any thread).
        [DllImport("user32.dll", EntryPoint = "PostMessageW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        public static extern int SendMessage(IntPtr hWnd, int message, int wParam, int lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, ref NativeRect lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, ref NativeHeaderHitTest lParam);

        // ---- user32: windows ----
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out NativeRect rect);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, int flags);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        public static extern int SetWindowLong(IntPtr hWnd, int index, int value);

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

        [DllImport("user32.dll")]
        public static extern int MapWindowPoints(IntPtr from, IntPtr to, ref NativeRect rectangle, uint count);

        [DllImport("user32.dll")]
        public static extern bool GetComboBoxInfo(IntPtr hwndCombo, ref NativeComboBoxInfo info);

        // ---- user32: painting ----
        [DllImport("user32.dll")]
        public static extern bool LockWindowUpdate(IntPtr hWndLock);

        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(IntPtr hWnd, IntPtr rect, bool erase);

        [DllImport("user32.dll")]
        public static extern bool RedrawWindow(IntPtr hWnd, IntPtr updateRect, IntPtr updateRegion, uint flags);

        [DllImport("gdi32.dll")]
        public static extern int SetTextColor(IntPtr hdc, int color);

        [DllImport("user32.dll")]
        public static extern IntPtr BeginPaint(IntPtr hWnd, out NativePaintStruct paint);

        [DllImport("user32.dll")]
        public static extern bool EndPaint(IntPtr hWnd, ref NativePaintStruct paint);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        // ---- user32: mouse ----
        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr window);

        [DllImport("user32.dll")]
        public static extern IntPtr GetCapture();

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern bool TrackMouseEvent(ref NativeTrackMouseEvent track);

        // ---- other ----
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string subAppName, string subIdList);

        // Returns when the desktop has put the next picture on the screen: the beat of the display.
        [DllImport("dwmapi.dll")]
        public static extern int DwmFlush();

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        // Numeric runs compare numerically, everything else as text - the
        // same comparison Explorer uses for file names.
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string text1, string text2);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeWindowPos
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
    internal struct NativePaintStruct
    {
        public IntPtr hdc;
        public bool fErase;
        public NativeRect rcPaint;
        public bool fRestore;
        public bool fIncUpdate;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeTrackMouseEvent
    {
        public int cbSize;
        public int dwFlags;
        public IntPtr hwndTrack;
        public int dwHoverTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeHeaderHitTest
    {
        public int X, Y;
        public uint Flags;
        public int Item;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeComboBoxInfo
    {
        public int cbSize;
        public NativeRect rcItem;
        public NativeRect rcButton;
        public int buttonState;
        public IntPtr hwndCombo;
        public IntPtr hwndEdit;
        public IntPtr hwndList;
    }
}

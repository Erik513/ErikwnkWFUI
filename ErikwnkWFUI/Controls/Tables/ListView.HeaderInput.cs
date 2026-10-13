using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Native;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    // The input of the header, which is a window of its own: a press at a divider resizes, another drag starts the reordering.
    public partial class ListView : System.Windows.Forms.ListView
    {

        // The header is a window of its own and gets its input here: a press at a divider resizes the column (by the screen
        // distance moved), any other drag reorders the columns.
        private sealed class HeaderInputSubclass : NativeWindow
        {
            private readonly ListView _owner;
            private int _pendingColumnIndex = -1;
            private int _pendingStartX;
            private ColumnHeader _resizingColumn;
            private int _resizeStartScreenX;
            private int _resizeStartWidth;

            public HeaderInputSubclass(ListView owner)
            {
                _owner = owner;
            }

            public void CancelPendingReorder()
            {
                _pendingColumnIndex = -1;
            }

            public static System.IntPtr GetHeaderHandle(System.IntPtr listViewHandle)
            {
                return SendMessage(listViewHandle, LVM_GETHEADER, System.IntPtr.Zero, System.IntPtr.Zero);
            }

            // The header is a window of its own: Invalidate() on the list does not reach it.
            public void InvalidateHeaderNow()
            {
                RedrawWindow(Handle, System.IntPtr.Zero, System.IntPtr.Zero, RDW_INVALIDATE | RDW_ERASE | RDW_UPDATENOW);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_LBUTTONDOWN && TryBeginResize(m.LParam))
                {
                    m.Result = System.IntPtr.Zero;
                    return;
                }

                if (_resizingColumn != null)
                {
                    if (m.Msg == WM_MOUSEMOVE || m.Msg == WM_LBUTTONUP)
                    {
                        if (m.Msg == WM_LBUTTONUP || (Control.MouseButtons & MouseButtons.Left) != 0)
                        {
                            ApplyResize();
                        }
                        if (m.Msg == WM_LBUTTONUP || (Control.MouseButtons & MouseButtons.Left) == 0)
                        {
                            EndResize();
                        }
                        m.Result = System.IntPtr.Zero;
                        return;
                    }
                    if (m.Msg == WM_CANCELMODE || m.Msg == WM_CAPTURECHANGED)
                    {
                        EndResize();
                    }
                }

                // The system asks for the cursor again and again while the mouse moves, before every mouse move; the
                // header answers with its resize cursor at a divider. Where that column cannot be resized the
                // answer is the arrow, given here instead of the header's - correcting it only after the mouse move
                // (see below) let the resize cursor flicker in between.
                // The mouse move at such a divider is not handed to the header at all (it would set its resize
                // cursor on the way): no button is down then, so there is nothing for it to track.
                if (m.Msg == WM_MOUSEMOVE && _resizingColumn == null && (Control.MouseButtons & MouseButtons.Left) == 0
                    && _owner.IsNearNonResizableColumnBorder(GetX(m.LParam)))
                {
                    Cursor.Current = Cursors.Default;
                    m.Result = System.IntPtr.Zero;
                    return;
                }

                if (m.Msg == WM_SETCURSOR && IsOnNonResizableBorder())
                {
                    Cursor.Current = Cursors.Default;
                    m.Result = (System.IntPtr)1;
                    return;
                }

                base.WndProc(ref m);

                switch (m.Msg)
                {
                    case WM_LBUTTONDOWN:
                        OnMouseDown(GetX(m.LParam));
                        break;

                    case WM_MOUSEMOVE:
                        // Runs after base.WndProc: the header sets its own
                        // resize cursor from its own mouse-move handling
                        // as well, so the arrow is set after it (and for
                        // WM_SETCURSOR above, instead of it).
                        TrySuppressResizeCursor(GetX(m.LParam));
                        OnMouseMove(GetX(m.LParam));
                        break;

                    case WM_LBUTTONUP:
                    case WM_CANCELMODE:
                    case WM_CAPTURECHANGED:
                        // A click that never went past the threshold: nothing to reorder.
                        _pendingColumnIndex = -1;
                        break;

                    case WM_PAINT:
                        FillHeaderTrailingBackground();
                        break;
                }
            }

            private static int GetX(System.IntPtr lParam)
            {
                return unchecked((short)((long)lParam & 0xFFFF));
            }

            // comctl32 clips the drawing of an item to its own rectangle, so OnDrawColumnHeader cannot paint right of the last
            // column; here the header's own device context is painted on.
            private void FillHeaderTrailingBackground()
            {
                if (_owner.Columns.Count == 0 || !GetClientRect(Handle, out NativeRect clientRect))
                {
                    return;
                }

                var rightEdge = _owner.GetColumnsTotalWidth();
                if (rightEdge >= clientRect.Right)
                {
                    return;
                }

                using (var graphics = Graphics.FromHwnd(Handle))
                using (var background = new SolidBrush(_owner.HeaderBackColor))
                {
                    graphics.FillRectangle(background, rightEdge, clientRect.Top, clientRect.Right - rightEdge, clientRect.Bottom - clientRect.Top);
                }
            }

            private bool TryBeginResize(System.IntPtr coordinates)
            {
                var hit = new NativeHeaderHitTest
                {
                    X = GetX(coordinates),
                    Y = unchecked((short)((long)coordinates >> 16))
                };
                const uint DividerFlags = HHT_ONDIVIDER | HHT_ONDIVOPEN;
                SendMessage(Handle, HDM_HITTEST, System.IntPtr.Zero, ref hit);
                if ((hit.Flags & DividerFlags) == 0 || hit.Item < 0 || hit.Item >= _owner.Columns.Count)
                {
                    return false;
                }

                CancelPendingReorder();
                _owner._suppressNextColumnClickSort = true;
                if (!_owner.IsColumnResizable(hit.Item))
                {
                    return true;
                }

                _resizingColumn = _owner.Columns[hit.Item];
                _resizeStartScreenX = Cursor.Position.X;
                _resizeStartWidth = _resizingColumn.Width;
                SetCapture(Handle);
                Cursor.Current = Cursors.SizeWE;
                return true;
            }

            private void ApplyResize()
            {
                var column = _resizingColumn;
                if (column == null || column.Index < 0 || column.ListView != _owner)
                {
                    EndResize();
                    return;
                }

                // A scrolling header changes client coordinates; screen deltas stay 1:1.
                int width = Math.Max(_owner.GetEffectiveMinimumWidth(column.Index),
                    _resizeStartWidth + Cursor.Position.X - _resizeStartScreenX);
                if (column.Width != width)
                {
                    column.Width = width;
                }
                Cursor.Current = Cursors.SizeWE;
            }

            private void EndResize()
            {
                _resizingColumn = null;
                CancelPendingReorder();
                if (GetCapture() == Handle)
                {
                    ReleaseCapture();
                }
                Cursor.Current = Cursors.Default;

                // Both windows are painted once more now that the drag is over: Windows may have found only a strip at the
                // dragged border dirty and left the trailing background wrong.
                _owner.Invalidate();
                _owner.Update();
                InvalidateHeaderNow();
            }

            // Whether the mouse is on a divider of a column that cannot be resized.
            private bool IsOnNonResizableBorder()
            {
                if (!GetWindowRect(Handle, out NativeRect window))
                    return false;

                return _owner.IsNearNonResizableColumnBorder(Cursor.Position.X - window.Left);
            }

            // The mouse move itself sets the header's resize cursor too, so the arrow is set again after it.
            private void TrySuppressResizeCursor(int x)
            {
                if (_owner.IsNearNonResizableColumnBorder(x))
                {
                    Cursor.Current = Cursors.Default;
                }
            }

            private void OnMouseDown(int x)
            {
                _pendingColumnIndex = -1;

                // (Only a drag that reaches BeginColumnDragDrop suppresses the sort of the click that follows.)
                _owner._suppressNextColumnClickSort = false;

                // (Without AllowDrop - off an STA thread, see the constructor - DoDragDrop has no target.)
                if (!_owner.AllowDrop || _owner.Columns.Count == 0 || _owner.IsNearColumnBorder(x))
                {
                    return;
                }

                var displayIndex = _owner.GetColumnIndexAtX(x);
                var orderedColumns = _owner.GetColumnsInDisplayOrder();
                if (displayIndex < 0 || displayIndex >= orderedColumns.Count)
                {
                    return;
                }

                var clickedColumn = orderedColumns[displayIndex];
                if (!_owner.IsColumnReorderable(clickedColumn.Index))
                {
                    return;
                }

                _pendingColumnIndex = clickedColumn.Index;
                _pendingStartX = Cursor.Position.X;
            }

            private void OnMouseMove(int x)
            {
                if ((Control.MouseButtons & MouseButtons.Left) == 0)
                {
                    CancelPendingReorder();
                    return;
                }

                if (_pendingColumnIndex < 0 || _owner._isDraggingColumn)
                {
                    return;
                }

                // (The screen position is used: the header's own coordinates move when the list scrolls during the drag.)
                if (Math.Abs(Cursor.Position.X - _pendingStartX) < SystemInformation.DragSize.Width)
                {
                    return;
                }

                var columnIndex = _pendingColumnIndex;
                _pendingColumnIndex = -1;

                // (DoDragDrop returns when the drag is over; it runs a message loop of its own.)
                _owner.BeginColumnDragDrop(columnIndex);
            }
        }
    }
}

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // The popup with the current value above the thumb (ShowValuePopup): while the user drags it, then briefly after they let go,
    // click once, or change the value with a key or the wheel - and for as long as the mouse simply hovers over it,
    // disappearing the instant the mouse leaves.
    public partial class TrackBar
    {
        // How long the popup stays up once nothing is actively changing the
        // value anymore (drag released, single click, or the last arrow-key
        // press) - there's no single "interaction ended" event that covers
        // all three, so this timeout stands in for it everywhere, like a
        // tooltip. A plain click (mouse down then up with no movement) still
        // goes through DragStarted/DragEnded just as fast as any other
        // click, so without this the popup would flash on and instantly
        // back off again.
        private const int PopupHideDelayMs = 700;

        private InfoPopupForm _popup;
        private Timer _hideTimer;

        private bool _showValuePopup;

        /// <summary>
        /// Whether the value is shown in a small popup above the thumb: while the user drags it, then briefly after they let go, click
        /// once or use a key or the wheel, and for as long as the mouse hovers over the slider. Off by default.
        /// </summary>
        [DefaultValue(false)]
        public bool ShowValuePopup
        {
            get { return _showValuePopup; }
            set
            {
                if (value == _showValuePopup)
                    return;

                _showValuePopup = value;

                if (!value)
                {
                    if (_hideTimer != null)
                        _hideTimer.Stop();

                    HidePopup();
                }
            }
        }
        private bool _popupWidthStale = true;

        private void InitializePopup()
        {
            // ValueChanged alone would already cover dragging too (it fires
            // continuously on every mouse-move, restarting the hide timer
            // each time so it never gets 700ms of quiet to actually fire) -
            // DragStarted is only needed for the click-without-moving edge
            // case, where SetValueFromUser's no-op-if-unchanged check means
            // ValueChanged never fires at all.
            DragStarted += delegate { if (_showValuePopup) ShowPopup(); };
            ValueChanged += delegate { if (_showValuePopup) { ShowPopup(); StartHideTimer(); } };

            // Forced (bypasses the throttle below) - releasing a fast drag
            // right on 0% or 100% could otherwise land its very last
            // ValueChanged inside the throttle window, leaving the popup
            // stuck showing a slightly-stale value forever (the drag is
            // over, so nothing else would ever trigger another update).
            DragEnded += delegate { if (_showValuePopup) { ShowPopup(force: true); StartHideTimer(); } };

            // Same problem, same fix, for holding an arrow key: OS key-repeat
            // can outrun the throttle just like a fast drag can, so the last
            // repeat before release might get skipped, stranding the popup on
            // a slightly-stale value (e.g. 95% instead of 100%). KeyUp fires
            // even though IsInputKey claims these keys - it only changes
            // which keys generate KeyDown/KeyUp, not whether they do.
            KeyUp += delegate(object sender, KeyEventArgs e) { if (_showValuePopup && IsValueKey(e.KeyCode)) { ShowPopup(force: true); StartHideTimer(); } };
        }

        // The keys that move the value: only these show the popup (not every key that is let go while the slider has the focus: Shift,
        // Ctrl, the Tab that brought the focus here ...).
        private static bool IsValueKey(Keys key)
        {
            return key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down
                || key == Keys.PageUp || key == Keys.PageDown || key == Keys.Home || key == Keys.End;
        }

        private static Size PopupSizeFor(Size text)
        {
            return new Size(text.Width + 8, text.Height + 6);
        }

        // The widest text of the values in the range: not only the two ends (a number with decimals is wider in the middle than
        // at the ends, "0.37" against "0" and "1"), so the range is measured in twenty steps, and the current value on top.
        private Size MeasureWidestValue()
        {
            Func<double, string> format = FormatValue;
            Size widest = MeasurePopupText(format(Value));

            for (int step = 0; step <= 20; step++)
            {
                double fraction = step / 20.0;
                Size size = MeasurePopupText(format(Minimum * (1 - fraction) + Maximum * fraction));

                widest = new Size(Math.Max(widest.Width, size.Width), Math.Max(widest.Height, size.Height));
            }

            return widest;
        }

        private static Size MeasurePopupText(string text)
        {
            return TextRenderer.MeasureText(
                text, UIFonts.Normal,
                new Size(short.MaxValue, short.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        private void StartHideTimer()
        {
            if (_hideTimer == null)
            {
                _hideTimer = new Timer { Interval = PopupHideDelayMs };
                _hideTimer.Tick += delegate
                {
                    _hideTimer.Stop();
                    HidePopup();
                };
            }

            // Restart rather than let an already-running one finish early -
            // repeated arrow presses (or a fresh drag right after a click)
            // should keep the popup up continuously, only hiding after the
            // last interaction.
            _hideTimer.Stop();
            _hideTimer.Start();
        }

        private InfoPopupForm Popup
        {
            get
            {
                if (_popup == null || _popup.IsDisposed)
                {
                    // The colors of the slider (gray, a set, the accent), asked for again each time: it follows them.
                    _popup = UIStyles.InfoPopups.CreateStandard();
                    _popup.FollowColors(() => Colors);

                    _popupWidthStale = true;
                }

                // Fixed size for the widest value so it doesn't jitter as digits are added/removed while dragging - just a
                // few px of breathing room around the text, like a tooltip. Worked out again when the range or the format has
                // changed.
                if (_popupWidthStale)
                {
                    _popupWidthStale = false;
                    _popup.FixedSize = PopupSizeFor(MeasureWidestValue());
                }

                return _popup;
            }
        }

        // Narrowly scoped to just around a ShowPopup() call (see below) -
        // deliberately NOT "is the popup currently visible", which was tried
        // first and was wrong: the popup is meant to stay visible for up to
        // PopupHideDelayMs after ANY interaction, so that check treated
        // every focus loss in that whole window - including a deliberate
        // click on some other control entirely - as "my own popup stole
        // focus" and kept stealing it back, blocking that other control.
        private bool _isSelfFocusChurn;

        // Guards against scheduling a fresh BeginInvoke below on every
        // single ShowPopup() call - which, during a fast drag, fires on
        // every mouse-move (dozens of times a second). Each BeginInvoke
        // posts a real message through the window's message queue; doing
        // that at mouse-move frequency was enough overhead to visibly stall
        // unrelated things driven by the same message loop, like the
        // Showcase's own ProgressBar animation timer. Only one clear is
        // ever pending at a time - later calls just keep _isSelfFocusChurn
        // true for longer, which is exactly what's wanted anyway.
        private bool _focusChurnClearPending;

        // The popup call (once, its BringToFront() forced a Z-order/DWM update
        // every single time) turned out to be the real cost, not the
        // BeginInvoke above - it runs once
        // per ValueChanged, i.e. once per mouse-move during a drag, dozens
        // of times a second, and that alone was enough to visibly stall the
        // Showcase's ProgressBar animation. A value readout doesn't
        // need to redraw faster than this to still look instant, so actual
        // updates are capped to this interval; ShowPopup() calls in between
        // just keep the already-visible popup up (via the hide timer) with
        // whatever value it last showed.
        private const int MinPopupUpdateIntervalMs = 40;
        private int _lastPopupUpdateTickCount = int.MinValue;

        // force bypasses the throttle - used by DragEnded to guarantee the
        // final value is always shown, even if it lands inside the window.
        private void ShowPopup(bool force = false)
        {
            // A disabled slider reacts to nothing, so it shows nothing either.
            if (FindForm() == null || !Enabled)
                return;

            int now = Environment.TickCount;
            bool alreadyVisible = _popup != null && !_popup.IsDisposed && _popup.Visible;
            if (!force && alreadyVisible && unchecked(now - _lastPopupUpdateTickCount) < MinPopupUpdateIntervalMs)
                return;

            _lastPopupUpdateTickCount = now;

            Point savedScroll = CaptureScrollPosition();

            _isSelfFocusChurn = true;
            string text = FormatValue(Value);
            InfoPopupForm popup = Popup;

            // A value wider than all that were measured (a format with more digits at some value): the popup grows to it and
            // stays so, rather than cutting the text.
            Size needed = PopupSizeFor(MeasurePopupText(text));

            if (needed.Width > popup.FixedSize.Width || needed.Height > popup.FixedSize.Height)
            {
                popup.FixedSize = new Size(Math.Max(needed.Width, popup.FixedSize.Width), Math.Max(needed.Height, popup.FixedSize.Height));
            }

            popup.SetContent(text);
            popup.ShowCenteredAbove(this, ThumbCenterX);

            RestoreScrollPosition(savedScroll);

            if (_focusChurnClearPending)
                return;

            _focusChurnClearPending = true;

            // Cleared on the NEXT message-loop iteration, not immediately -
            // Show()'s transient focus loss can arrive slightly after this
            // call returns, not only synchronously within it.
            BeginInvoke(new MethodInvoker(delegate
            {
                _focusChurnClearPending = false;
                _isSelfFocusChurn = false;
            }));
        }

        private void HidePopup()
        {
            if (_popup == null || _popup.IsDisposed)
                return;

            Point savedScroll = CaptureScrollPosition();
            _popup.Hide();
            RestoreScrollPosition(savedScroll);
        }

        // Show()/Hide() on the popup - a separate top-level window owned by
        // whatever Form this control lives in - can disturb that owner's
        // ActiveControl (e.g. hiding it seems to hand activation back to the
        // owner, which then falls back to selecting the first control in tab
        // order if nothing else claims it). If this control sits inside a
        // ScrollableControl, .NET auto-scrolls whatever just became active
        // into view - which, for the "first control" fallback, usually means
        // jumping straight back to the very top. Saving/restoring the
        // nearest scrollable ancestor's position around every Show/Hide
        // neutralizes that regardless of the exact mechanism.
        private Point CaptureScrollPosition()
        {
            ScrollableControl scrollable = FindScrollableAncestor();
            return scrollable != null
                ? new Point(-scrollable.AutoScrollPosition.X, -scrollable.AutoScrollPosition.Y)
                : Point.Empty;
        }

        private void RestoreScrollPosition(Point saved)
        {
            ScrollableControl scrollable = FindScrollableAncestor();
            if (scrollable != null)
                scrollable.AutoScrollPosition = saved;
        }

        private ScrollableControl FindScrollableAncestor()
        {
            for (Control control = Parent; control != null; control = control.Parent)
            {
                ScrollableControl scrollable = control as ScrollableControl;
                if (scrollable != null && scrollable.AutoScroll)
                    return scrollable;
            }
            return null;
        }

        // Independent of the interaction popup above (drag/click/arrow-key,
        // which lingers for PopupHideDelayMs) - hovering shows it too, but
        // only for exactly as long as the mouse stays over the control, no
        // lingering. Skipped while dragging: DragStarted/ValueChanged/
        // DragEnded already own the popup for that, and the mouse can leave
        // the control mid-drag (no Capture is set) without the drag itself
        // ending.
        private void PopupMouseEnter()
        {
            if (!_showValuePopup || IsDragging)
                return;

            if (_hideTimer != null)
                _hideTimer.Stop();

            ShowPopup(force: true);
        }

        private void PopupMouseLeave()
        {
            if (!_showValuePopup || IsDragging)
                return;

            HidePopup();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();

            if (!_showValuePopup)
                return;

            if (_isSelfFocusChurn)
            {
                // Our own popup's Show()/BringToFront() just took Win32
                // focus, not the user moving away - reclaim it once the
                // current message (which might still be Show()'s own
                // internal pump) has fully unwound. Reclaiming inline here
                // was tried and made things measurably worse during a fast
                // drag: Show() pumps messages internally, so a queued
                // MouseMove could dispatch and recurse back into
                // ShowPopup() while Show() was still on the call stack,
                // corrupting the popup window (UI freezes, a popup that
                // never finished painting).
                BeginInvoke(new MethodInvoker(delegate
                {
                    if (!IsDisposed && !Focused)
                        Focus();
                }));
                return;
            }

            if (_hideTimer != null)
                _hideTimer.Stop();
            HidePopup();
        }

        private void DisposePopup()
        {
            if (_hideTimer != null)
            {
                _hideTimer.Stop();
                _hideTimer.Dispose();
                _hideTimer = null;
            }

            if (_popup != null)
            {
                _popup.Dispose();
                _popup = null;
            }
        }
    }
}

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
        // back off again. (Not a constant so that a test does not have to wait for the whole time.)
        internal static int PopupHideDelayMs = 1500;

        private InfoPopupForm _popup;
        private Timer _hideTimer;

        private bool _showValuePopup;
        private UIColorSet _popupColors;

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

        /// <summary>
        /// The colors of the popup (its background and its text): a color set, or simply a color. Null (the default) gives it the colors
        /// of the slider - gray for <c>CreateStandard</c>, the accent for <c>CreatePrimary</c>, the set of <see cref="Colors"/> - and it
        /// follows them. Not written by the designer (a set is code).
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIColorSet ValuePopupColors
        {
            get { return _popupColors; }
            set
            {
                _popupColors = value;

                if (_popup != null && !_popup.IsDisposed)
                    _popup.Invalidate();
            }
        }

        private void InitializePopup()
        {
            // ValueChanged alone would already cover dragging too (it fires
            // continuously on every mouse-move, restarting the hide timer
            // each time so it never gets a quiet moment of that length to actually fire) -
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

            _hideTimer.Interval = Math.Max(1, PopupHideDelayMs);

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
                    // Its own colors if it has some, else the colors of the slider (gray, a set, the accent), asked for again each time: it
                    // follows them.
                    _popup = UIStyles.InfoPopups.CreateStandard();
                    _popup.FollowColors(() => _popupColors ?? Colors);

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

        // The updates of the popup are capped to this interval: it runs once per ValueChanged, that is once per mouse-move during a drag,
        // dozens of times a second, and moving a window that often is enough to visibly stall other things driven by the same message loop
        // (the progress bar animation of the Showcase did). A value readout does not need to redraw faster to still look instant. The
        // update that is held back is made when the pause is over (see ScheduleTrailingUpdate).
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
            int sinceLast = unchecked(now - _lastPopupUpdateTickCount);

            if (!force && alreadyVisible && sinceLast < MinPopupUpdateIntervalMs)
            {
                // Not shown now, but not forgotten: when the user stops (a fast slide to an end, the mouse held there), nothing else
                // would come, and the popup would stay at the value of the last update ("95" with the thumb at 100). The last value is
                // shown when the pause is over.
                ScheduleTrailingUpdate(MinPopupUpdateIntervalMs - sinceLast);

                return;
            }

            if (_trailingTimer != null)
                _trailingTimer.Stop();

            _lastPopupUpdateTickCount = now;

            Point savedScroll = CaptureScrollPosition();

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
        }

        // The update of the popup that was held back by the throttle, made when the pause is over (with the value of that moment).
        private Timer _trailingTimer;

        private void ScheduleTrailingUpdate(int milliseconds)
        {
            if (_trailingTimer == null)
            {
                _trailingTimer = new Timer();
                _trailingTimer.Tick += delegate
                {
                    _trailingTimer.Stop();

                    if (_showValuePopup && !IsDisposed)
                        ShowPopup(force: true);
                };
            }

            // The first one that is waiting stays: later values only change what it will show.
            if (_trailingTimer.Enabled)
                return;

            _trailingTimer.Interval = Math.Max(1, milliseconds);
            _trailingTimer.Start();
        }

        private void HidePopup()
        {
            if (_trailingTimer != null)
                _trailingTimer.Stop();

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

            // A drag that ends with the mouse outside of the slider (a slide past the end) lets the mouse leave right after the
            // release: the popup is still in the short time it stays after an interaction, and the hide timer takes it away then.
            if (_hideTimer != null && _hideTimer.Enabled)
                return;

            HidePopup();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();

            if (!_showValuePopup)
                return;

            // The popup is shown without taking the focus (an owned window that is not activated), so a loss of the focus is always the
            // user's: the focus goes where the user sent it, and the popup goes with it.
            if (_hideTimer != null)
                _hideTimer.Stop();
            HidePopup();
        }

        private void DisposePopup()
        {
            if (_trailingTimer != null)
            {
                _trailingTimer.Stop();
                _trailingTimer.Dispose();
                _trailingTimer = null;
            }

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

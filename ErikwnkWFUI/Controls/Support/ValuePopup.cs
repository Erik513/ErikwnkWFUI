using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>What a <see cref="ValuePopup"/> needs to know of the control that it belongs to.</summary>
    internal interface IValuePopupHost
    {
        /// <summary>The control that the popup is shown above.</summary>
        Control Owner { get; }

        double Value { get; }
        double Minimum { get; }
        double Maximum { get; }

        /// <summary>The text for a value.</summary>
        string Format(double value);

        /// <summary>Pixel x (in the owner) that the popup is centered over - the thumb.</summary>
        int AnchorX { get; }

        /// <summary>The colors of the popup, asked for again each time it is painted (it follows them).</summary>
        UIColorSet PopupColors { get; }
    }

    /// <summary>
    /// The small popup with the current value above a control (the thumb of a <see cref="TrackBar"/>): the mechanics of showing it
    /// without taking the focus, keeping its size steady, not moving it too often, and taking it away again - while the user changes
    /// the value, briefly after, and for as long as the mouse hovers over the control. The control tells it what happens
    /// (<see cref="ShowUpdate"/>, <see cref="ShowAfterInteraction"/>, <see cref="MouseEnter"/> ...) and answers its questions
    /// (<see cref="IValuePopupHost"/>).
    /// </summary>
    internal sealed class ValuePopup
    {
        // How long the popup stays up once nothing is actively changing the
        // value anymore (drag released, single click, or the last arrow-key
        // press) - there's no single "interaction ended" event that covers
        // all three, so this timeout stands in for it everywhere, like a
        // tooltip. A plain click (mouse down then up with no movement) still
        // goes through DragStarted/DragEnded just as fast as any other
        // click, so without this the popup would flash on and instantly
        // back off again. (Not a constant so that a test does not have to wait for the whole time.)
        internal static int HideDelayMs = 1500;

        // The updates of the popup are capped to this interval: it runs once per ValueChanged, that is once per mouse-move during a drag,
        // dozens of times a second, and moving a window that often is enough to visibly stall other things driven by the same message loop
        // (the progress bar animation of the Showcase did). A value readout does not need to redraw faster to still look instant. The
        // update that is held back is made when the pause is over (see ScheduleTrailingUpdate).
        private const int MinUpdateIntervalMs = 40;

        private readonly IValuePopupHost _host;

        private InfoPopupForm _popup;
        private Timer _hideTimer;

        // The update of the popup that was held back by the throttle, made when the pause is over (with the value of that moment).
        private Timer _trailingTimer;

        private bool _enabled;
        private bool _widthStale = true;
        private int _lastUpdateTickCount = int.MinValue;

        public ValuePopup(IValuePopupHost host)
        {
            _host = host;
        }

        /// <summary>Whether the popup is used at all; turning it off takes it away at once.</summary>
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value == _enabled)
                    return;

                _enabled = value;

                if (!value)
                    Hide();
            }
        }

        /// <summary>The widest text of the values may have changed (the range or the format): measured again before the next showing.</summary>
        public void MarkWidthStale()
        {
            _widthStale = true;
        }

        /// <summary>The colors have changed: the popup, if there is one, is painted again.</summary>
        public void ColorsChanged()
        {
            if (_popup != null && !_popup.IsDisposed)
                _popup.Invalidate();
        }

        /// <summary>The popup as the user sees it, for the tests; null as long as it has not been needed.</summary>
        internal InfoPopupForm Form
        {
            get { return _popup; }
        }

        /// <summary>The timer that takes the popup away after an interaction, for the tests; null as long as it has not been needed.</summary>
        internal Timer LingerTimer
        {
            get { return _hideTimer; }
        }

        /// <summary>
        /// Shows the popup with the current value, or updates it. <paramref name="force"/> bypasses the throttle - used for the last
        /// value of an interaction, which must always be shown, even if it lands inside the window.
        /// </summary>
        public void ShowUpdate(bool force = false)
        {
            if (!_enabled)
                return;

            Show(force);
        }

        /// <summary>Shows (or updates) the popup, and lets it stay a little after the last interaction (<see cref="HideDelayMs"/>).</summary>
        public void ShowAfterInteraction(bool force = false)
        {
            if (!_enabled)
                return;

            Show(force);
            StartHideTimer();
        }

        // Independent of the interaction popup above (drag/click/arrow-key,
        // which lingers for HideDelayMs) - hovering shows it too, but
        // only for exactly as long as the mouse stays over the control, no
        // lingering. Skipped while dragging: the drag already owns the popup for that, and the mouse can leave
        // the control mid-drag (no Capture is set) without the drag itself
        // ending.
        public void MouseEnter(bool dragging)
        {
            if (!_enabled || dragging)
                return;

            if (_hideTimer != null)
                _hideTimer.Stop();

            Show(force: true);
        }

        public void MouseLeave(bool dragging)
        {
            if (!_enabled || dragging)
                return;

            // A drag that ends with the mouse outside of the control (a slide past the end) lets the mouse leave right after the
            // release: the popup is still in the short time it stays after an interaction, and the hide timer takes it away then.
            if (_hideTimer != null && _hideTimer.Enabled)
                return;

            Hide();
        }

        public void LostFocus()
        {
            if (!_enabled)
                return;

            // The popup is shown without taking the focus (an owned window that is not activated), so a loss of the focus is always the
            // user's: the focus goes where the user sent it, and the popup goes with it.
            if (_hideTimer != null)
                _hideTimer.Stop();

            Hide();
        }

        /// <summary>Takes the popup away (and the update that was held back).</summary>
        public void Hide()
        {
            if (_trailingTimer != null)
                _trailingTimer.Stop();

            if (_hideTimer != null && !_enabled)
                _hideTimer.Stop();

            if (_popup == null || _popup.IsDisposed)
                return;

            Point savedScroll = CaptureScrollPosition();
            _popup.Hide();
            RestoreScrollPosition(savedScroll);
        }

        public void Dispose()
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

        private void Show(bool force)
        {
            Control owner = _host.Owner;

            // A disabled control reacts to nothing, so it shows nothing either.
            if (owner.FindForm() == null || !owner.Enabled)
                return;

            int now = Environment.TickCount;
            bool alreadyVisible = _popup != null && !_popup.IsDisposed && _popup.Visible;
            int sinceLast = unchecked(now - _lastUpdateTickCount);

            if (!force && alreadyVisible && sinceLast < MinUpdateIntervalMs)
            {
                // Not shown now, but not forgotten: when the user stops (a fast slide to an end, the mouse held there), nothing else
                // would come, and the popup would stay at the value of the last update ("95" with the thumb at 100). The last value is
                // shown when the pause is over.
                ScheduleTrailingUpdate(MinUpdateIntervalMs - sinceLast);

                return;
            }

            if (_trailingTimer != null)
                _trailingTimer.Stop();

            _lastUpdateTickCount = now;

            Point savedScroll = CaptureScrollPosition();

            string text = _host.Format(_host.Value);
            InfoPopupForm popup = Popup;

            // A value wider than all that were measured (a format with more digits at some value): the popup grows to it and
            // stays so, rather than cutting the text.
            Size needed = SizeFor(MeasureText(text));

            if (needed.Width > popup.FixedSize.Width || needed.Height > popup.FixedSize.Height)
            {
                popup.FixedSize = new Size(Math.Max(needed.Width, popup.FixedSize.Width), Math.Max(needed.Height, popup.FixedSize.Height));
            }

            popup.SetContent(text);
            popup.ShowCenteredAbove(owner, _host.AnchorX);

            RestoreScrollPosition(savedScroll);
        }

        private InfoPopupForm Popup
        {
            get
            {
                if (_popup == null || _popup.IsDisposed)
                {
                    // Its own colors if it has some, else the colors of the control (gray, a set, the accent), asked for again each
                    // time: it follows them.
                    _popup = UIStyles.InfoPopups.CreateStandard();
                    _popup.FollowColors(() => _host.PopupColors);

                    _widthStale = true;
                }

                // Fixed size for the widest value so it doesn't jitter as digits are added/removed while dragging - just a
                // few px of breathing room around the text, like a tooltip. Worked out again when the range or the format has
                // changed.
                if (_widthStale)
                {
                    _widthStale = false;
                    _popup.FixedSize = SizeFor(MeasureWidestValue());
                }

                return _popup;
            }
        }

        private static Size SizeFor(Size text)
        {
            return new Size(text.Width + 8, text.Height + 6);
        }

        // The widest text of the values in the range: not only the two ends (a number with decimals is wider in the middle than
        // at the ends, "0.37" against "0" and "1"), so the range is measured in twenty steps, and the current value on top.
        private Size MeasureWidestValue()
        {
            Size widest = MeasureText(_host.Format(_host.Value));

            for (int step = 0; step <= 20; step++)
            {
                double fraction = step / 20.0;
                Size size = MeasureText(_host.Format(_host.Minimum * (1 - fraction) + _host.Maximum * fraction));

                widest = new Size(Math.Max(widest.Width, size.Width), Math.Max(widest.Height, size.Height));
            }

            return widest;
        }

        private static Size MeasureText(string text)
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
                _hideTimer = new Timer { Interval = HideDelayMs };
                _hideTimer.Tick += delegate
                {
                    _hideTimer.Stop();
                    Hide();
                };
            }

            _hideTimer.Interval = Math.Max(1, HideDelayMs);

            // Restart rather than let an already-running one finish early -
            // repeated arrow presses (or a fresh drag right after a click)
            // should keep the popup up continuously, only hiding after the
            // last interaction.
            _hideTimer.Stop();
            _hideTimer.Start();
        }

        private void ScheduleTrailingUpdate(int milliseconds)
        {
            if (_trailingTimer == null)
            {
                _trailingTimer = new Timer();
                _trailingTimer.Tick += delegate
                {
                    _trailingTimer.Stop();

                    if (_enabled && !_host.Owner.IsDisposed)
                        Show(force: true);
                };
            }

            // The first one that is waiting stays: later values only change what it will show.
            if (_trailingTimer.Enabled)
                return;

            _trailingTimer.Interval = Math.Max(1, milliseconds);
            _trailingTimer.Start();
        }

        // Show()/Hide() on the popup - a separate top-level window owned by
        // whatever Form the control lives in - can disturb that owner's
        // ActiveControl (e.g. hiding it seems to hand activation back to the
        // owner, which then falls back to selecting the first control in tab
        // order if nothing else claims it). If the control sits inside a
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
            for (Control control = _host.Owner.Parent; control != null; control = control.Parent)
            {
                ScrollableControl scrollable = control as ScrollableControl;
                if (scrollable != null && scrollable.AutoScroll)
                    return scrollable;
            }
            return null;
        }
    }
}

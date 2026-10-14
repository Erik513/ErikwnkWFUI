using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// A small, borderless popup with extra info next to a control or the mouse, like a
    /// tooltip. Its content is blocks of an optional header and an optional text
    /// (<see cref="SetContent(InfoPopupSection[])"/>). Reuse one instance for many show calls:
    /// it is moved and refilled instead of being closed and created again. Get one through
    /// <see cref="UIStyles.InfoPopups"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use: set the content, then show it next to a control (<see cref="ShowNextTo"/>), at the
    /// mouse (<see cref="ShowAtMouse"/>), centered above a point of a control
    /// (<see cref="ShowCenteredAbove"/>) or at the mouse after the <see cref="Delay"/>
    /// (<see cref="ShowAtMouseDelayed(Control, Point, InfoPopupSection[])"/>). Content without any text shows nothing. As the
    /// tooltip of controls, attach it (<see cref="AttachTo(Control, string)"/>): it then shows
    /// and hides itself as the mouse comes and goes.
    /// </para>
    /// <para>
    /// Look: the size follows the content, and the text wraps at a maximum width. With
    /// <see cref="FixedSize"/> the popup keeps one size and centers its content (a percentage
    /// over a slider). Header and text are white, or black on a background too light for white;
    /// every header and text can have its own color (<see cref="InfoPopupSection.HeaderColor"/>,
    /// <see cref="InfoPopupSection.TextColor"/>). <see cref="Colors"/> sets the background and
    /// <see cref="PopupFormBase.Transparency"/> how see-through the popup is.
    /// </para>
    /// <para>
    /// Window: the popup belongs to the window of the control it is shown for. It is placed
    /// inside that window (and on the screen) as far as it fits; a popup larger than the window
    /// sits at its top left. It is shown only while that window is in front, so not when
    /// another window or program covers it, and not over a minimized window. It stays above its
    /// window but not above other programs, moves with the window, and never takes the
    /// activation (not on show, not on a click), so the window keeps the focus. It hides itself
    /// after its <see cref="Duration"/>; hide it earlier with <c>Hide</c>. <c>Close</c> disposes
    /// it, as for any form shown with an owner, and it closes with its owner form.
    /// </para>
    /// <para>
    /// Compared with the standard <c>ToolTip</c>: the header and the text, the time before it
    /// shows (<see cref="Delay"/>, the ToolTip's InitialDelay), the time it stays
    /// (<see cref="Duration"/>, AutoPopDelay) and the colors are offered. The popup is shown
    /// when you call it or when it is attached to a control. Not offered: balloon style and
    /// icons, fading and animation, a separate time for moving to the next control
    /// (ReshowDelay), and showing while the window is not in front (ShowAlways). An ampersand is
    /// shown as it is, not as a mnemonic.
    /// </para>
    /// <para>
    /// Set by the library, so an application's own values are replaced: <c>BackColor</c> and
    /// the text colors (use <see cref="Colors"/>), the fonts, the border style and
    /// <c>ShowInTaskbar</c>. <c>Opacity</c> can still be set directly. The rounded corners come
    /// from Windows 11; on Windows 10 the popup is a plain rectangle. <c>RightToLeft</c> is not
    /// supported.
    /// </para>
    /// </remarks>
    // In three files: this one (the content, showing and placing), InfoPopupForm.Layout.cs (measuring and painting) and
    // InfoPopupForm.Attach.cs (the popup as the tooltip of controls).
    public partial class InfoPopupForm : PopupFormBase
    {
        private const int MaxTextWidth96 = 260;

        private const int Padding96 = 20;

        private const int FixedPadding96 = 4;

        private const int BlockGap96 = 8;

        private const int HeaderGap96 = 3;

        private const int ScreenMargin = PopupSupport.ScreenMargin;

        private const int AnchorGap96 = 6;

        private const int MouseOffset96 = 12;

        private const int AnchorOffsetX = 8;

        private const int AnchorOffsetY = -10;


        private const TextFormatFlags MeasureFlags =
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.Left;


        private Control _lastAnchor;

        private Timer _showDelayTimer;

        private Control _pendingAnchor;

        private Point _pendingMouseScreenPosition;

        private Func<InfoPopupSection[]> _pendingContent;

        private InfoPopupSection[] _sections = new InfoPopupSection[0];

        private UIColorSet _colors;

        private Func<UIColorSet> _colorsSource;

        private PopupDuration _duration = PopupDuration.Medium;

        private PopupDelay _delay = PopupDelay.Medium;

        private Timer _hideTimer;

        private Size _fixedSize = Size.Empty;


        public InfoPopupForm()
        {
            DoubleBuffered = true;
            BackColor = GetBackColor();

            _showDelayTimer = new Timer { Interval = _delay.ToMilliseconds() };
            _showDelayTimer.Tick += OnShowDelayTimerTick;

            _hideTimer = new Timer { Interval = _duration.ToInfoMilliseconds() };
            _hideTimer.Tick += delegate
            {
                _hideTimer.Stop();
                Hide();
            };

            // A popup is reused for a long time: a set that follows the accent (CreatePrimary) is looked at again when the
            // accent changes while the popup has a window, and at every show (see Place).
            ThemeWatcher.Attach(this, () =>
            {
                BackColor = GetBackColor();
                Invalidate();
            });
        }


        /// <summary>
        /// How long <see cref="ShowAtMouseDelayed(Control, Point, InfoPopupSection[])"/> waits before it shows the popup: not at all,
        /// 0.2, 0.4 (the default) or 0.8 seconds.
        /// </summary>
        public PopupDelay Delay
        {
            get { return _delay; }
            set
            {
                _delay = value;

                if (_showDelayTimer != null)
                    _showDelayTimer.Interval = value.ToMilliseconds();
            }
        }


        /// <summary>
        /// How long the popup stays after it was shown (or moved) before it hides itself: 3, 5
        /// (the default) or 8 seconds. Every show call starts the time again, so a popup that
        /// is shown from a MouseMove handler stays while the mouse moves over its control.
        /// </summary>
        public PopupDuration Duration
        {
            get { return _duration; }
            set
            {
                _duration = value;

                if (_hideTimer != null)
                    _hideTimer.Interval = value.ToInfoMilliseconds();
            }
        }


        protected override void OnTransparencyChanged()
        {
            BackColor = GetBackColor();
        }


        /// <summary>
        /// The colors of the popup: its background is the set's basic color. Left null, the
        /// neutral gray (<c>UIStyles.Colors.Sets.Gray</c>);
        /// <c>UIStyles.InfoPopups.CreatePrimary</c> makes one in the accent color.
        /// </summary>
        public UIColorSet Colors
        {
            get { return _colorsSource?.Invoke() ?? _colors; }
            set
            {
                _colors = value;
                _colorsSource = null;

                if (IsDisposed)
                    return;

                BackColor = GetBackColor();
                Invalidate();
            }
        }


        // A set that is asked for again each time (the primary set, which follows the accent).
        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorsSource = source;
            _colors = null;

            if (IsDisposed)
                return;

            BackColor = GetBackColor();
            Invalidate();
        }


        /// <summary>
        /// The client size the popup keeps whatever its content, with the content centered; for
        /// one short value that changes while the popup is shown ("9%" and "100%" must not
        /// resize it). Empty (the default) sizes the popup to its content. In pixels.
        /// </summary>
        public Size FixedSize
        {
            get { return _fixedSize; }
            set
            {
                _fixedSize = value;
                UpdateSize();
            }
        }


        // ---- the content ----

        /// <summary>
        /// Sets what the popup shows: one block per section, each with a header, a text, or
        /// both; a section without either is left out.
        /// </summary>
        public void SetContent(params InfoPopupSection[] sections)
        {
            InfoPopupSection[] kept = InfoPopupSection.Normalize(sections);

            if (InfoPopupSection.SameContent(kept, _sections))
                return;

            _sections = kept;
            UpdateSize();
        }


        /// <summary>Sets a single text, without a header.</summary>
        public void SetContent(string text)
        {
            SetContent(new InfoPopupSection(null, text));
        }


        /// <summary>Sets a header with its text, each in its own color or, left empty, the automatic one.</summary>
        public void SetContent(string header, string text, Color headerColor = default(Color), Color textColor = default(Color))
        {
            SetContent(new InfoPopupSection(header, text) { HeaderColor = headerColor, TextColor = textColor });
        }


        private bool HasContent
        {
            get { return _sections.Length > 0; }
        }


        // ---- showing it ----

        /// <summary>Shows (or moves) the popup at the bottom-right of the control <paramref name="anchor"/>.</summary>
        public void ShowNextTo(Control anchor)
        {
            if (!CanShow(anchor))
                return;

            Place(anchor, GetLocationNextTo(anchor));
        }


        /// <summary>Shows (or moves) the popup near <paramref name="mouseScreenPosition"/> - typical use is a MouseMove handler.</summary>
        public void ShowAtMouse(Control anchor, Point mouseScreenPosition)
        {
            if (!CanShow(anchor))
                return;

            Place(anchor, GetLocationNearMouse(anchor, mouseScreenPosition));
        }


        /// <summary>
        /// Shows (or moves) the popup centered horizontally on <paramref name="anchorLocalX"/> (a pixel x
        /// inside <paramref name="anchor"/>) and just above <paramref name="anchor"/>; below it if there is
        /// no room above. Typical use is a live value readout over a slider thumb.
        /// </summary>
        public void ShowCenteredAbove(Control anchor, int anchorLocalX)
        {
            if (!CanShow(anchor))
                return;

            Point anchorTop = anchor.PointToScreen(new Point(anchorLocalX, 0));
            int x = anchorTop.X - Width / 2;
            int gap = Scale(AnchorGap96);
            int y = anchorTop.Y - Height - gap;

            if (y < PopupSupport.GetPlacementArea(anchor).Top + ScreenMargin)
                y = anchorTop.Y + anchor.Height + gap;

            Place(anchor, new Point(x, y));
        }


        /// <summary>
        /// Like <see cref="ShowAtMouse"/> with the given content, but after the <see cref="Delay"/>
        /// (at once for <see cref="PopupDelay.Instant"/>). For hover tooltips: passing the mouse
        /// over several items does not flash a popup for each. Call
        /// <see cref="CancelPendingShow"/> on MouseLeave to cancel a show that has not happened yet.
        /// </summary>
        public void ShowAtMouseDelayed(Control anchor, Point mouseScreenPosition, params InfoPopupSection[] sections)
        {
            ShowAtMouseDelayed(anchor, mouseScreenPosition, () => sections);
        }


        // The content is asked for when the popup shows, not when the show is asked for.
        private void ShowAtMouseDelayed(Control anchor, Point mouseScreenPosition, Func<InfoPopupSection[]> content)
        {
            if (IsDisposed || anchor == null || anchor.IsDisposed)
                return;

            if (_delay == PopupDelay.Instant)
            {
                CancelPendingShow();
                SetContent(content());
                ShowAtMouse(anchor, mouseScreenPosition);

                return;
            }

            _pendingAnchor = anchor;
            _pendingMouseScreenPosition = mouseScreenPosition;
            _pendingContent = content;

            _showDelayTimer.Stop();
            _showDelayTimer.Start();
        }


        /// <summary>Cancels a show scheduled by <see cref="ShowAtMouseDelayed(Control, Point, InfoPopupSection[])"/> that has not happened yet - does not hide the popup if it is already showing.</summary>
        public void CancelPendingShow()
        {
            if (_showDelayTimer != null)
                _showDelayTimer.Stop();

            _pendingAnchor = null;
            _pendingContent = null;
        }


        private void OnShowDelayTimerTick(object sender, EventArgs e)
        {
            _showDelayTimer.Stop();

            Control anchor = _pendingAnchor;
            Func<InfoPopupSection[]> content = _pendingContent;

            if (anchor == null || anchor.IsDisposed || content == null)
                return;

            SetContent(content());
            ShowAtMouse(anchor, _pendingMouseScreenPosition);
        }


        // Nothing to show without an anchor or without content; a popup that is up goes away
        // when its content is taken.
        private bool CanShow(Control anchor)
        {
            if (IsDisposed || anchor == null || anchor.IsDisposed)
                return false;

            if (!HasContent)
            {
                Hide();

                return false;
            }

            return true;
        }


        private void Place(Control anchor, Point location)
        {
            Form ownerForm = anchor.FindForm();

            if (!PopupSupport.CanShowOver(ownerForm))
                return;

            // Another window's control: the popup is shown again as that window's own, not kept
            // above the window it was shown for before.
            if (Visible && Owner != ownerForm)
                Hide();

            BackColor = GetBackColor();
            Location = PopupSupport.KeepInside(location, Size, PopupSupport.GetPlacementArea(anchor), Screen.FromControl(anchor).WorkingArea);

            // Not BringToFront(): an owned window lies above its owner anyway, and BringToFront moves the window without
            // SWP_NOACTIVATE - the popup then became the window in front, and with its hiding Windows chose the next one,
            // which could be another program.
            if (!Visible)
                Show(ownerForm);

            Follower.Attach(this, ownerForm);
            _lastAnchor = anchor;
            NoteShownFor(anchor);

            _hideTimer.Stop();
            _hideTimer.Start();
        }


        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!Visible)
            {
                Follower.Detach();
                _hideTimer.Stop();
                _lastAnchor = null;
            }
        }


        private Point GetLocationNextTo(Control anchor)
        {
            return anchor.PointToScreen(
                new Point(anchor.Width + AnchorOffsetX, -Height + anchor.Height + AnchorOffsetY));
        }


        // Right and below the mouse, on the other side of it where that does not fit.
        private Point GetLocationNearMouse(Control anchor, Point mouseScreenPosition)
        {
            int offset = Scale(MouseOffset96);
            Point result = new Point(mouseScreenPosition.X + offset, mouseScreenPosition.Y + offset);
            Rectangle area = PopupSupport.GetPlacementArea(anchor);

            if (result.X + Width > area.Right - ScreenMargin)
                result.X = mouseScreenPosition.X - Width - offset;

            if (result.Y + Height > area.Bottom - ScreenMargin)
                result.Y = mouseScreenPosition.Y - Height - offset;

            return result;
        }


        // ---- colors ----

        private UIColorSet GetColors()
        {
            return Colors ?? UIStyles.Colors.Sets.Gray;
        }


        // The automatic text color and the background, worked out for both popups in PopupSupport.
        private Color GetForeColor()
        {
            return PopupSupport.GetTextColor(GetColors());
        }


        private Color GetBackColor()
        {
            return PopupSupport.GetBackground(GetColors(), Opacity);
        }


        protected override int CornerRadius96
        {
            get { return IsFixed ? 6 : 12; }
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (Control control in new List<Control>(_bindings.Keys))
                    Detach(control);

                Follower.Detach();

                if (_hideTimer != null)
                {
                    _hideTimer.Stop();
                    _hideTimer.Dispose();
                    _hideTimer = null;
                }

                if (_showDelayTimer != null)
                {
                    _showDelayTimer.Stop();
                    _showDelayTimer.Tick -= OnShowDelayTimerTick;
                    _showDelayTimer.Dispose();
                    _showDelayTimer = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}

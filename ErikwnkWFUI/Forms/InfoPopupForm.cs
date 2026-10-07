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
    /// (<see cref="ShowAtMouseDelayed"/>). Content without any text shows nothing. As the
    /// tooltip of controls, attach it (<see cref="AttachTo(Control, string)"/>): it then shows
    /// and hides itself as the mouse comes and goes.
    /// </para>
    /// <para>
    /// Look: the size follows the content, and the text wraps at a maximum width. With
    /// <see cref="FixedSize"/> the popup keeps one size and centers its content (a percentage
    /// over a slider). Header and text are white, or black on a background too light for white;
    /// every header and text can have its own color (<see cref="InfoPopupSection.HeaderColor"/>,
    /// <see cref="InfoPopupSection.TextColor"/>). <see cref="Colors"/> sets the background and
    /// <see cref="Transparency"/> how see-through the popup is.
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
    public class InfoPopupForm : Form
    {
        private const int MaxTextWidth96 = 260;
        private const int Padding96 = 20;
        private const int FixedPadding96 = 4;
        private const int BlockGap96 = 8;
        private const int HeaderGap96 = 3;
        private const int ScreenMargin = 10;
        private const int AnchorOffsetX = 8;
        private const int AnchorOffsetY = -10;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private const TextFormatFlags MeasureFlags =
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.Left;

        private readonly OwnerFollower _follower = new OwnerFollower();
        private Control _lastAnchor;
        private Timer _showDelayTimer;
        private Control _pendingAnchor;
        private Point _pendingMouseScreenPosition;
        private Func<InfoPopupSection[]> _pendingContent;
        private InfoPopupSection[] _sections = new InfoPopupSection[0];
        private UIColorSet _colors;
        private PopupTransparency _transparency = PopupTransparency.Medium;
        private PopupDuration _duration = PopupDuration.Medium;
        private PopupDelay _delay = PopupDelay.Medium;
        private Timer _hideTimer;
        private Size _fixedSize = Size.Empty;

        public InfoPopupForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            Opacity = PopupTransparency.Medium.ToOpacity();
            BackColor = GetBackColor();

            _showDelayTimer = new Timer { Interval = _delay.ToMilliseconds() };
            _showDelayTimer.Tick += OnShowDelayTimerTick;

            _hideTimer = new Timer { Interval = _duration.ToInfoMilliseconds() };
            _hideTimer.Tick += delegate
            {
                _hideTimer.Stop();
                Hide();
            };

            Load += delegate { ApplyRoundedCorners(); };
        }

        /// <summary>
        /// How long <see cref="ShowAtMouseDelayed"/> waits before it shows the popup: not at all,
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

        /// <summary>How see-through the popup is: not at all, slightly, clearly (the default) or strongly.</summary>
        public PopupTransparency Transparency
        {
            get { return _transparency; }
            set
            {
                _transparency = value;
                Opacity = value.ToOpacity();
                BackColor = GetBackColor();
            }
        }

        /// <summary>
        /// The colors of the popup: its background is the set's basic color. Left null, the
        /// neutral gray (<c>UIStyles.Colors.Sets.Gray</c>);
        /// <c>UIStyles.InfoPopups.CreatePrimary</c> makes one in the accent color.
        /// </summary>
        public UIColorSet Colors
        {
            get { return _colors; }
            set
            {
                _colors = value;
                BackColor = GetBackColor();
                Invalidate();
            }
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

        // A popup must never take the activation away from the window it is shown for (the window
        // of a slider that is being dragged, say).
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        // The same for a click on the popup: it does not activate either.
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= WS_EX_NOACTIVATE;

                return parameters;
            }
        }

        // ---- the content ----

        /// <summary>
        /// Sets what the popup shows: one block per section, each with a header, a text, or
        /// both; a section without either is left out.
        /// </summary>
        public void SetContent(params InfoPopupSection[] sections)
        {
            InfoPopupSection[] kept = Normalize(sections);

            if (SameContent(kept, _sections))
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

        private static InfoPopupSection[] Normalize(InfoPopupSection[] sections)
        {
            System.Collections.Generic.List<InfoPopupSection> kept = new System.Collections.Generic.List<InfoPopupSection>();

            if (sections != null)
            {
                foreach (InfoPopupSection section in sections)
                {
                    if (section == null)
                        continue;

                    bool hasHeader = !string.IsNullOrWhiteSpace(section.Header);
                    bool hasText = !string.IsNullOrWhiteSpace(section.Text);

                    if (hasHeader || hasText)
                        kept.Add(new InfoPopupSection(hasHeader ? section.Header : null, hasText ? section.Text : null)
                        {
                            HeaderColor = section.HeaderColor,
                            TextColor = section.TextColor
                        });
                }
            }

            return kept.ToArray();
        }

        private static bool SameContent(InfoPopupSection[] first, InfoPopupSection[] second)
        {
            if (first.Length != second.Length)
                return false;

            for (int i = 0; i < first.Length; i++)
            {
                if (first[i].Header != second[i].Header || first[i].Text != second[i].Text
                    || first[i].HeaderColor != second[i].HeaderColor || first[i].TextColor != second[i].TextColor)
                    return false;
            }

            return true;
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
            int y = anchorTop.Y - Height - 6;

            if (y < GetPlacementArea(anchor).Top + ScreenMargin)
                y = anchorTop.Y + anchor.Height + 6;

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

        /// <summary>Cancels a show scheduled by <see cref="ShowAtMouseDelayed"/> that has not happened yet - does not hide the popup if it is already showing.</summary>
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

            BackColor = GetBackColor();
            Location = KeepInside(location, GetPlacementArea(anchor), Screen.FromControl(anchor).WorkingArea);

            if (!Visible)
            {
                Show(ownerForm);
                BringToFront();
            }

            _follower.Attach(this, ownerForm);
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
                _follower.Detach();
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
            Point result = new Point(mouseScreenPosition.X + 12, mouseScreenPosition.Y + 12);
            Rectangle area = GetPlacementArea(anchor);

            if (result.X + Width > area.Right - ScreenMargin)
                result.X = mouseScreenPosition.X - Width - 12;

            if (result.Y + Height > area.Bottom - ScreenMargin)
                result.Y = mouseScreenPosition.Y - Height - 12;

            return result;
        }

        // Where the popup may be: inside the window of its control, and on the screen where that
        // window reaches beyond it. A control without a form has the screen.
        private static Rectangle GetPlacementArea(Control anchor)
        {
            Rectangle screen = Screen.FromControl(anchor).WorkingArea;
            Form form = anchor.FindForm();

            if (form == null)
                return screen;

            Rectangle inside = Rectangle.Intersect(form.Bounds, screen);

            return inside.IsEmpty ? screen : inside;
        }

        // Moves the popup inside the area, with a margin to its edge where there is room for
        // it. A popup that is larger than the area cannot fit: it sits at the start of the area
        // (the top left), as far as the screen shows it.
        private Point KeepInside(Point location, Rectangle area, Rectangle screen)
        {
            return new Point(
                KeepInside(location.X, Width, area.Left, area.Right, screen.Left, screen.Right),
                KeepInside(location.Y, Height, area.Top, area.Bottom, screen.Top, screen.Bottom));
        }

        private static int KeepInside(int position, int size, int start, int end, int screenStart, int screenEnd)
        {
            if (size > end - start)
                return Math.Max(screenStart, Math.Min(start, screenEnd - size));

            int margin = size + 2 * ScreenMargin <= end - start ? ScreenMargin : 0;
            int first = start + margin;
            int last = end - margin - size;

            return Math.Max(first, Math.Min(position, last));
        }

        // ---- attaching to controls ----

        // What the popup knows about a control it is attached to.
        private sealed class Binding
        {
            public Control Control;
            public Func<InfoPopupSection[]> Content;
            public Control Parent;
            public bool Inside;
            public bool Shown;
            public MouseEventHandler OnMove;
            public EventHandler OnLeave;
            public MouseEventHandler OnDown;
            public KeyEventHandler OnKey;
            public EventHandler OnHidden;
            public EventHandler OnDisposed;
            public EventHandler OnParentChanged;
            public MouseEventHandler OnParentMove;
            public EventHandler OnParentLeave;
        }

        private readonly Dictionary<Control, Binding> _bindings = new Dictionary<Control, Binding>();

        /// <summary>
        /// Shows this popup as the tooltip of <paramref name="control"/>: after the
        /// <see cref="Delay"/> once the mouse rests on it, at the mouse, once for each time the
        /// mouse enters. It goes away when the mouse leaves, on a click or a key press, and when
        /// the control is hidden or disposed (which also detaches it). One popup can serve any
        /// number of controls, each with its own content. A disabled control gets it too.
        /// </summary>
        public void AttachTo(Control control, string text)
        {
            AttachTo(control, () => new[] { new InfoPopupSection(null, text) });
        }

        /// <summary>Attaches a header with its text; see <see cref="AttachTo(Control, string)"/>.</summary>
        public void AttachTo(Control control, string header, string text)
        {
            AttachTo(control, () => new[] { new InfoPopupSection(header, text) });
        }

        /// <summary>Attaches the blocks; see <see cref="AttachTo(Control, string)"/>.</summary>
        public void AttachTo(Control control, params InfoPopupSection[] sections)
        {
            AttachTo(control, () => sections);
        }

        /// <summary>
        /// Attaches content that is worked out each time the popup is about to show (a value
        /// that changes, a text for the current state); see <see cref="AttachTo(Control, string)"/>.
        /// </summary>
        public void AttachTo(Control control, Func<InfoPopupSection[]> content)
        {
            if (control == null || content == null || IsDisposed)
                return;

            Detach(control);

            Binding binding = new Binding { Control = control, Content = content };

            binding.OnMove = (sender, e) => Hover(binding, Control.MousePosition);
            binding.OnLeave = (sender, e) => Leave(binding);
            binding.OnDown = (sender, e) => Leave(binding);
            binding.OnKey = (sender, e) => Leave(binding);
            binding.OnHidden = (sender, e) =>
            {
                if (!control.Visible)
                    Leave(binding);
            };
            binding.OnDisposed = (sender, e) => Detach(control);
            binding.OnParentChanged = (sender, e) => HookParent(binding);

            // A disabled control gets no mouse events of its own: its parent sees the mouse and
            // the control is found by where it is.
            binding.OnParentMove = (sender, e) =>
            {
                if (!control.Enabled && control.Bounds.Contains(e.Location))
                    Hover(binding, Control.MousePosition);
                else if (binding.Inside && !control.Enabled)
                    Leave(binding);
            };
            binding.OnParentLeave = (sender, e) =>
            {
                if (binding.Inside && !control.Enabled)
                    Leave(binding);
            };

            control.MouseMove += binding.OnMove;
            control.MouseLeave += binding.OnLeave;
            control.MouseDown += binding.OnDown;
            control.KeyDown += binding.OnKey;
            control.VisibleChanged += binding.OnHidden;
            control.Disposed += binding.OnDisposed;
            control.ParentChanged += binding.OnParentChanged;
            HookParent(binding);

            _bindings[control] = binding;
        }

        /// <summary>Takes the popup off <paramref name="control"/> again; a popup that is up for it goes away.</summary>
        public void Detach(Control control)
        {
            Binding binding;

            if (control == null || !_bindings.TryGetValue(control, out binding))
                return;

            _bindings.Remove(control);
            Leave(binding);

            control.MouseMove -= binding.OnMove;
            control.MouseLeave -= binding.OnLeave;
            control.MouseDown -= binding.OnDown;
            control.KeyDown -= binding.OnKey;
            control.VisibleChanged -= binding.OnHidden;
            control.Disposed -= binding.OnDisposed;
            control.ParentChanged -= binding.OnParentChanged;
            UnhookParent(binding);
        }

        private void HookParent(Binding binding)
        {
            UnhookParent(binding);

            binding.Parent = binding.Control.Parent;

            if (binding.Parent != null)
            {
                binding.Parent.MouseMove += binding.OnParentMove;
                binding.Parent.MouseLeave += binding.OnParentLeave;
            }
        }

        private void UnhookParent(Binding binding)
        {
            if (binding.Parent == null)
                return;

            binding.Parent.MouseMove -= binding.OnParentMove;
            binding.Parent.MouseLeave -= binding.OnParentLeave;
            binding.Parent = null;
        }

        // The mouse is on the control: once for each time it enters, the popup is scheduled, and
        // not again while it is up or after it has gone away by itself.
        private void Hover(Binding binding, Point mouseScreenPosition)
        {
            binding.Inside = true;

            if (binding.Shown)
                return;

            ShowAtMouseDelayed(binding.Control, mouseScreenPosition, binding.Content);
        }

        private void Leave(Binding binding)
        {
            binding.Inside = false;
            binding.Shown = false;

            // Only what this control asked for: another control's popup is not touched.
            if (_pendingAnchor == binding.Control)
                CancelPendingShow();

            if (Visible && _lastAnchor == binding.Control)
                Hide();
        }

        private void NoteShownFor(Control anchor)
        {
            Binding binding;

            if (_bindings.TryGetValue(anchor, out binding))
                binding.Shown = true;
        }

        // ---- colors ----

        private UIColorSet GetColors()
        {
            return _colors ?? UIStyles.Colors.Sets.Gray;
        }

        // The automatic text color: white, or black when the set's color is too light for white.
        // On the color itself one of the two always reads (at least 4.58:1).
        private Color GetForeColor()
        {
            Color basic = GetColors().Basic;

            return UIColors.GetContrastRatio(Color.White, basic) >= UIColors.GetContrastRatio(Color.Black, basic)
                ? Color.White
                : Color.Black;
        }

        // The background: the set's basic color, kept readable through the transparency. The
        // whole window is see-through, so the text fades with it and the page behind (white or
        // black, the worst) shines through the background; with a white page behind, white text
        // on a mid-tone color can drop far below 4.5:1. The color is shifted away from the text
        // (darker for white text, lighter for black) just far enough for the text to read over
        // both a white and a black page. A color that reads already stays as it is.
        private Color GetBackColor()
        {
            Color back = GetColors().Basic;
            Color text = GetForeColor();
            double step = text == Color.White ? -0.01 : 0.01;

            for (int i = 0; i < 100 && WorstContrast(text, back) < 4.5; i++)
                back = UIColors.ShiftLightness(back, step);

            return back;
        }

        // The lower of the contrasts the text has over a white page and over a black page.
        private double WorstContrast(Color text, Color back)
        {
            double over = double.MaxValue;

            foreach (Color page in new[] { Color.White, Color.Black })
            {
                Color seenBack = Mix(back, page, Opacity);
                Color seenText = Mix(text, page, Opacity);

                over = Math.Min(over, UIColors.GetContrastRatio(seenText, seenBack));
            }

            return over;
        }

        // A color seen through the window: its share of the opacity, the rest from the page.
        private static Color Mix(Color color, Color page, double opacity)
        {
            return Color.FromArgb(
                (int)Math.Round(color.R * opacity + page.R * (1 - opacity)),
                (int)Math.Round(color.G * opacity + page.G * (1 - opacity)),
                (int)Math.Round(color.B * opacity + page.B * (1 - opacity)));
        }

        // ---- measuring and painting ----

        private int Scale(int value96)
        {
            return (int)Math.Round(value96 * DeviceDpi / 96.0);
        }

        private bool IsFixed
        {
            get { return !_fixedSize.IsEmpty; }
        }

        // The width the text may take: the maximum width, or in the fixed size what is left.
        private int GetTextWidth()
        {
            return IsFixed
                ? Math.Max(1, _fixedSize.Width - 2 * Scale(FixedPadding96))
                : Scale(MaxTextWidth96);
        }

        // The size of one block's header and text, wrapped at the text width.
        private void MeasureBlock(InfoPopupSection section, int textWidth, out Size header, out Size text)
        {
            header = Size.Empty;
            text = Size.Empty;

            if (section.Header != null)
                header = TextRenderer.MeasureText(section.Header, UIFonts.Title, new Size(textWidth, int.MaxValue), MeasureFlags);

            if (section.Text != null)
                text = TextRenderer.MeasureText(section.Text, UIFonts.Normal, new Size(textWidth, int.MaxValue), MeasureFlags);
        }

        private Size MeasureContent(int textWidth)
        {
            int width = 0;
            int height = 0;

            for (int i = 0; i < _sections.Length; i++)
            {
                Size header;
                Size text;
                MeasureBlock(_sections[i], textWidth, out header, out text);

                if (i > 0)
                    height += Scale(BlockGap96);

                height += header.Height + text.Height;

                if (header.Height > 0 && text.Height > 0)
                    height += Scale(HeaderGap96);

                width = Math.Max(width, Math.Max(header.Width, text.Width));
            }

            return new Size(width, height);
        }

        // A borderless top-level form cannot go below the system's minimum tracking size
        // (about 136x39) unless MinimumSize is set: it is locked to the wanted size.
        private void UpdateSize()
        {
            Size client;

            if (IsFixed)
            {
                client = _fixedSize;
            }
            else
            {
                Size content = MeasureContent(GetTextWidth());
                int padding = Scale(Padding96);
                client = new Size(content.Width + 2 * padding, content.Height + 2 * padding);
            }

            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;
            ClientSize = client;
            MinimumSize = Size;
            MaximumSize = Size;

            // A popup that is up and changes size must still fit inside the window.
            if (Visible && _lastAnchor != null && !_lastAnchor.IsDisposed)
                Location = KeepInside(Location, GetPlacementArea(_lastAnchor), Screen.FromControl(_lastAnchor).WorkingArea);

            ApplyRoundedCorners();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!HasContent)
                return;

            int textWidth = GetTextWidth();
            Size content = MeasureContent(textWidth);
            int padding = IsFixed ? Scale(FixedPadding96) : Scale(Padding96);

            // In the fixed size the content sits in the middle, otherwise at the padding.
            int x = padding;
            int y = IsFixed ? Math.Max(0, (ClientSize.Height - content.Height) / 2) : padding;

            Color autoColor = GetForeColor();
            TextFormatFlags align = IsFixed ? TextFormatFlags.HorizontalCenter : TextFormatFlags.Left;
            TextFormatFlags flags = (MeasureFlags & ~TextFormatFlags.Left) | align;

            for (int i = 0; i < _sections.Length; i++)
            {
                InfoPopupSection section = _sections[i];
                Size header;
                Size text;
                MeasureBlock(section, textWidth, out header, out text);

                if (i > 0)
                    y += Scale(BlockGap96);

                if (header.Height > 0)
                {
                    TextRenderer.DrawText(e.Graphics, section.Header, UIFonts.Title, new Rectangle(x, y, textWidth, header.Height), section.HeaderColor.IsEmpty ? autoColor : section.HeaderColor, flags);
                    y += header.Height;
                }

                if (header.Height > 0 && text.Height > 0)
                    y += Scale(HeaderGap96);

                if (text.Height > 0)
                {
                    TextRenderer.DrawText(e.Graphics, section.Text, UIFonts.Normal, new Rectangle(x, y, textWidth, text.Height), section.TextColor.IsEmpty ? autoColor : section.TextColor, flags);
                    y += text.Height;
                }
            }
        }

        // Win11 rounds window corners itself via DWM (anti-aliased, all four identical): just
        // ask for the radius. A window Region fights this and leaves one corner square; on
        // Win10 the call does nothing and the popup is a plain rectangle.
        private void ApplyRoundedCorners()
        {
            if (!IsHandleCreated)
                return;

            int preference = IsFixed ? DWMWCP_ROUNDSMALL : DWMWCP_ROUND;

            try
            {
                DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            }
            catch
            {
                // before Windows 11: no rounded corners
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyRoundedCorners();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (Control control in new List<Control>(_bindings.Keys))
                    Detach(control);

                _follower.Detach();

                if (_hideTimer != null)
                {
                    _hideTimer.Stop();
                    _hideTimer.Dispose();
                    _hideTimer = null;
                }
            }

            if (disposing && _showDelayTimer != null)
            {
                _showDelayTimer.Stop();
                _showDelayTimer.Tick -= OnShowDelayTimerTick;
                _showDelayTimer.Dispose();
                _showDelayTimer = null;
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>One block of content for <see cref="InfoPopupForm"/>: a header, a text, or both. A part left null or empty is not shown.</summary>
    public class InfoPopupSection
    {
        public string Header { get; set; }
        public string Text { get; set; }

        /// <summary>The color of the header. Left empty, it is white, or black on a background too light for white.</summary>
        public Color HeaderColor { get; set; }

        /// <summary>The color of the text. Left empty, it is white, or black on a background too light for white.</summary>
        public Color TextColor { get; set; }

        public InfoPopupSection(string header, string text)
        {
            Header = header;
            Text = text;
        }
    }
}

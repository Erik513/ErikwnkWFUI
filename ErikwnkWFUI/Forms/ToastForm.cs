using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// A small, rounded, self-dismissing toast: an optional symbol, an optional title and a
    /// message. Call the static <see cref="ShowToast(string, Form, UIColorSet, PopupTransparency, PopupDuration, PopupDelay, ToastIcon, string, ToastPosition)"/>
    /// (or <c>UIStyles.Toasts</c>) rather than constructing this directly. Only one toast shows
    /// at a time; a new one closes the one that is up.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Window.</b> The toast belongs to its window: it is shown only while that window is in
    /// front, stays above it but not above other windows or programs, moves with it and never
    /// takes the activation. A toast that cannot be shown because its window is not in front
    /// is not lost: it waits and is shown when the window is activated again. At most
    /// <see cref="MaxPendingToasts"/> wait for one window (the newest; the same content twice in
    /// a row counts once), one that has waited longer than <see cref="PendingLifetime"/> is
    /// dropped as out of date, and several are shown one after the other. A window that closes
    /// takes its waiting toasts with it. A call from another thread is carried over to the
    /// thread of the window.
    /// </para>
    /// <para>
    /// <b>Place and size.</b> By default the toast is centered in the lower part of the window;
    /// <see cref="ToastPosition"/> puts it in the middle, above the middle, at the top or
    /// bottom or in a corner, with a fixed distance to the edge. It stays inside the window (and
    /// on the screen) as far as it fits; a window narrower than the toast gets it at its left
    /// edge. The width is fixed; a long message makes the toast higher, up to four lines (more
    /// ends in an ellipsis). The size and the places inside follow the scaling of the screen the
    /// window is on.
    /// </para>
    /// <para>
    /// <b>Content.</b> A message, an optional title (one line, bold; an ellipsis ends what is too
    /// long) and an optional symbol (<see cref="ToastIcon"/>, or any <c>UIStyles.Symbols</c>
    /// symbol). Title and message may have their own colors (<see cref="InfoPopupSection"/>, as
    /// in the info popup); they are used as given and not corrected for readability. A toast
    /// without a title and without a message shows nothing. An ampersand is shown as it is.
    /// </para>
    /// <para>
    /// <b>Closing.</b> The toast closes itself after its <see cref="Duration"/>, or at once on a
    /// click. A toast the mouse is on when its time is up stays until the mouse has left it.
    /// </para>
    /// <para>
    /// Compared with the balloon of a <c>NotifyIcon</c> (the standard notification): the text, a
    /// title, a symbol, the colors and the time it stays are offered. Not offered: an event for
    /// a click on the toast or for its closing, and a toast that stays until it is clicked.
    /// </para>
    /// </remarks>
    // In three files: this one (the settings of one toast and its controls), ToastForm.Queue.cs (which toast is shown when)
    // and ToastForm.Layout.cs (size and place).
    public partial class ToastForm : PopupFormBase
    {
        // The measures at 96 dpi; ApplyDpi scales them for the screen the window is on.
        private const int ToastWidth96 = 350;
        private const int ToastHeight96 = 80;
        private const int MaxMessageLines = 4;
        private const int EdgeDistance96 = 20;

        // The room inside the toast: around the content, in front of the text when a symbol is shown (the symbol sits in
        // front of that), and to the right of it.
        private const int ContentPaddingHorizontal96 = 15;
        private const int ContentPaddingVertical96 = 10;
        private const int TextIndentWithSymbol96 = 30;
        private const int TextRightPadding96 = 10;
        private const int IconSize96 = 30;
        private const int IconLeft96 = 10;

        // The height: at least this much above and below the text, a little slack for the lines of the label (they are a
        // bit higher than the measure), and the room between the title and the message.
        private const int MinVerticalMargin96 = 20;
        private const int LineSlack96 = 6;
        private const int TitleGap96 = 2;

        private int _dpi = 96;

        /// <summary>How many toasts wait for one window that is not in front; the oldest are dropped.</summary>
        public const int MaxPendingToasts = 3;

        /// <summary>How long a toast waits for its window to come to the front before it is dropped. One minute by default.</summary>
        public static TimeSpan PendingLifetime { get; set; } = TimeSpan.FromSeconds(60);

        private static ToastForm _currentToast;
        private Form _ownerForm;

        private Timer _closeTimer;
        private Label _titleLabel;
        private Label _messageLabel;
        private Label _iconLabel;
        private Font _iconFont;
        private InfoPopupSection _content = new InfoPopupSection(null, null);
        private Panel _contentPanel;

        public ToastForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            CreateControls();
            CreateTimer();
            ApplyDpi(DeviceDpi);
        }

        /// <summary>Shows a toast in <paramref name="owner"/>, at <paramref name="position"/>, closing itself after <paramref name="duration"/>. If <paramref name="owner"/> is not in front, the toast waits until it is.</summary>
        /// <param name="message">The text of the toast.</param>
        /// <param name="owner">The window the toast belongs to and is shown over.</param>
        /// <param name="colors">A color set for the background of the toast (its text is white or black) instead of the neutral gray: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="Color"/>. <c>UIStyles.Toasts.ShowPrimary</c> uses the accent.</param>
        /// <param name="transparency">How see-through the toast is; medium by default.</param>
        /// <param name="duration">How long the toast stays: short, medium (2.5 seconds, the default) or long. The time runs from when it is shown, not from when it was asked for.</param>
        /// <param name="delay">How long to wait before the toast is shown: not at all (the default), 0.2, 0.4 or 0.8 seconds. Whether the window is in front is checked when the wait is over.</param>
        /// <param name="icon">The symbol for the kind of message: none (the default), a check mark, an info, a warning or an error.</param>
        /// <param name="symbol">Any other symbol instead of <paramref name="icon"/>, one of <c>UIStyles.Symbols</c> (<c>UIStyles.Symbols.Copy</c>, say); null (the default) uses <paramref name="icon"/>.</param>
        /// <param name="position">Where the toast is shown in the window: below the middle (centered, in the lower part; the default), the middle, above the middle, or at the top or bottom, centered or in a corner; the corners and edges keep a fixed distance from the window's edge.</param>
        public static void ShowToast(
            string message,
            Form owner,
            UIColorSet colors = null,
            PopupTransparency transparency = PopupTransparency.Medium,
            PopupDuration duration = PopupDuration.Medium,
            PopupDelay delay = PopupDelay.Instant,
            ToastIcon icon = ToastIcon.None,
            string symbol = null,
            ToastPosition position = ToastPosition.BelowCenter)
        {
            ShowToast(new InfoPopupSection(null, message), owner, colors, transparency, duration, delay, icon, symbol, position);
        }

        /// <summary>Like the call with a message, but with a title above the message and, if wanted, own colors for each (<see cref="InfoPopupSection.HeaderColor"/> for the title, <see cref="InfoPopupSection.TextColor"/> for the message). A part left empty is not shown; with neither there is no toast.</summary>
        /// <param name="content">The title (<see cref="InfoPopupSection.Header"/>) and the message (<see cref="InfoPopupSection.Text"/>).</param>
        /// <param name="owner">The window the toast belongs to and is shown over.</param>
        /// <param name="colors">A color set for the background, as for the call with a message.</param>
        /// <param name="transparency">How see-through the toast is; medium by default.</param>
        /// <param name="duration">How long the toast stays; medium by default.</param>
        /// <param name="delay">How long to wait before the toast is shown; not at all by default.</param>
        /// <param name="icon">The symbol for the kind of message; none by default.</param>
        /// <param name="symbol">Any other symbol instead of <paramref name="icon"/>, one of <c>UIStyles.Symbols</c>; null (the default) uses <paramref name="icon"/>.</param>
        /// <param name="position">Where the toast is shown in the window: below the middle (centered, in the lower part; the default), the middle, above the middle, or at the top or bottom, centered or in a corner; the corners and edges keep a fixed distance from the window's edge.</param>
        public static void ShowToast(
            InfoPopupSection content,
            Form owner,
            UIColorSet colors = null,
            PopupTransparency transparency = PopupTransparency.Medium,
            PopupDuration duration = PopupDuration.Medium,
            PopupDelay delay = PopupDelay.Instant,
            ToastIcon icon = ToastIcon.None,
            string symbol = null,
            ToastPosition position = ToastPosition.BelowCenter)
        {
            InfoPopupSection[] kept = InfoPopupSection.Normalize(new[] { content });

            if (owner == null || owner.IsDisposed || kept.Length == 0)
                return;

            ToastRequest request = new ToastRequest
            {
                Content = kept[0],
                Colors = colors,
                Transparency = transparency,
                Duration = duration,
                Delay = delay,
                Icon = icon,
                Symbol = symbol,
                Position = position
            };

            // A toast is often asked for when a task has finished, on another thread: it is shown
            // on the thread of its window, where it has its message loop and its timer.
            if (owner.IsHandleCreated && owner.InvokeRequired)
            {
                owner.BeginInvoke(new Action(() => Present(request, owner)));

                return;
            }

            Present(request, owner);
        }

        /// <summary>
        /// The colors of the toast: its background is the set's basic color and the text white
        /// or black. Left null, the neutral gray (<c>UIStyles.Colors.Sets.Gray</c>);
        /// <c>UIStyles.Toasts.ShowPrimary</c> makes one in the accent color.
        /// </summary>
        public UIColorSet Colors
        {
            get { return _colors; }
            set
            {
                _colors = value;
                ApplyColors();
            }
        }

        private UIColorSet _colors;

        // The background (kept readable through the transparency) and the text on it.
        private void ApplyColors()
        {
            if (IsDisposed)
                return;

            UIColorSet colors = _colors ?? UIStyles.Colors.Sets.Gray;
            Color text = PopupSupport.GetTextColor(colors);

            BackColor = PopupSupport.GetBackground(colors, Opacity);

            if (_iconLabel != null)
                _iconLabel.ForeColor = text;

            if (_titleLabel != null)
                _titleLabel.ForeColor = _content.HeaderColor.IsEmpty ? text : _content.HeaderColor;

            if (_messageLabel != null)
                _messageLabel.ForeColor = _content.TextColor.IsEmpty ? text : _content.TextColor;
        }

        /// <summary>The symbol at the left of the toast; none by default, then the text has the whole width.</summary>
        public ToastIcon Symbol
        {
            get { return _symbol; }
            set
            {
                _symbol = value;

                if (IsDisposed)
                    return;

                ApplyIcon();
                ApplyTextPadding();
                FitToMessage();
            }
        }

        private ToastIcon _symbol = ToastIcon.None;

        /// <summary>
        /// Any symbol instead of <see cref="Symbol"/>, one of <c>UIStyles.Symbols</c> (say
        /// <c>UIStyles.Symbols.Copy</c>) or a character of your own that the symbol font has.
        /// Null or empty (the default) uses <see cref="Symbol"/>.
        /// </summary>
        public string CustomSymbol
        {
            get { return _customSymbol; }
            set
            {
                _customSymbol = value;

                if (IsDisposed)
                    return;

                ApplyIcon();
                ApplyTextPadding();
                FitToMessage();
            }
        }

        private string _customSymbol;

        // What is drawn at the left: the symbol of one's own, or that of the kind of message.
        private string GetShownGlyph()
        {
            return string.IsNullOrWhiteSpace(_customSymbol) ? GetGlyph(_symbol) : _customSymbol;
        }

        private static string GetGlyph(ToastIcon icon)
        {
            switch (icon)
            {
                case ToastIcon.Success: return UIStyles.Symbols.Check;
                case ToastIcon.Info: return UIStyles.Symbols.Info;
                case ToastIcon.Warning: return UIStyles.Symbols.Warning;
                case ToastIcon.Error: return UIStyles.Symbols.Error;
                default: return "";
            }
        }

        // The glyph, in the font that has it (some symbols come from the Windows icon font).
        private void ApplyIcon()
        {
            string glyph = GetShownGlyph();
            Font previous = _iconFont;

            _iconLabel.Visible = glyph.Length > 0;
            _iconLabel.Text = glyph;

            if (glyph.Length > 0)
            {
                _iconFont = new Font(UIStyles.Symbols.GetFontName(glyph), UIFonts.Icon.Size);
                _iconLabel.Font = _iconFont;
            }
            else
            {
                // Without a symbol the label must not keep a font that is about to be given back.
                _iconFont = null;
                _iconLabel.Font = UIFonts.Icon;
            }

            if (previous != null)
                previous.Dispose();
        }

        // The text starts after the symbol, or near the edge without one.
        private void ApplyTextPadding()
        {
            int left = GetShownGlyph().Length == 0 ? 0 : TextIndentWithSymbol96;

            _messageLabel.Padding = new Padding(Dpi.Scale(left, _dpi), 0, Dpi.Scale(TextRightPadding96, _dpi), 0);
            _titleLabel.Padding = _messageLabel.Padding;
        }

        /// <summary>How long the toast stays before it closes itself.</summary>
        public PopupDuration Duration
        {
            get { return _duration; }
            set
            {
                _duration = value;

                if (_closeTimer != null)
                    _closeTimer.Interval = value.ToToastMilliseconds();
            }
        }

        private PopupDuration _duration = PopupDuration.Medium;

        // The background is kept readable through the transparency.
        protected override void OnTransparencyChanged()
        {
            ApplyColors();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_closeTimer != null)
                {
                    _closeTimer.Stop();
                    _closeTimer.Tick -= OnCloseTimerTick;
                    _closeTimer.Dispose();
                    _closeTimer = null;
                }

                if (_iconFont != null)
                {
                    _iconFont.Dispose();
                    _iconFont = null;
                }

                if (_currentToast == this)
                    _currentToast = null;
            }

            base.Dispose(disposing);
        }

        private void CreateControls()
        {
            _contentPanel = CreateContentPanel();

            _iconLabel = CreateIconLabel();
            _titleLabel = CreateTitleLabel();
            _messageLabel = CreateMessageLabel();

            // The one added last is docked first: the title at the top, then the message fills the rest.
            _contentPanel.Controls.Add(_iconLabel);
            _contentPanel.Controls.Add(_messageLabel);
            _contentPanel.Controls.Add(_titleLabel);

            Controls.Add(_contentPanel);
            ApplyColors();

            // A click anywhere on the toast closes it; the hand shows that it can be clicked.
            Cursor = Cursors.Hand;
            Click += OnToastClick;
            ForEachControl(_contentPanel, control => control.Click += OnToastClick);
        }

        private static void ForEachControl(Control parent, Action<Control> action)
        {
            action(parent);

            foreach (Control child in parent.Controls)
                ForEachControl(child, action);
        }

        private Panel CreateContentPanel()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(ContentPaddingHorizontal96, ContentPaddingVertical96, ContentPaddingHorizontal96, ContentPaddingVertical96),
                BackColor = Color.Transparent
            };
        }

        private Label CreateIconLabel()
        {
            return new Label
            {
                Text = "",
                Font = UIFonts.Icon,
                Size = new Size(IconSize96, IconSize96),
                Location = new Point(IconLeft96, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Visible = false
            };
        }

        private Label CreateTitleLabel()
        {
            return new Label
            {
                Text = "",
                Font = UIFonts.Title,
                TextAlign = ContentAlignment.BottomLeft,
                BackColor = Color.Transparent,
                AutoSize = false,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, TextRightPadding96, 0),
                UseMnemonic = false,
                AutoEllipsis = true,
                Visible = false
            };
        }

        private Label CreateMessageLabel()
        {
            return new Label
            {
                Text = "",
                Font = UIFonts.Normal,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, TextRightPadding96, 0),
                UseMnemonic = false,
                AutoEllipsis = true
            };
        }

        private void CreateTimer()
        {
            _closeTimer = new Timer();
            _closeTimer.Interval = _duration.ToToastMilliseconds();
            _closeTimer.Tick += OnCloseTimerTick;
        }

        private void SetContent(InfoPopupSection content)
        {
            _content = content;
            _titleLabel.Text = content.Header ?? "";
            _titleLabel.Visible = content.Header != null;
            _messageLabel.Text = content.Text ?? "";
            _messageLabel.TextAlign = content.Header != null ? ContentAlignment.TopLeft : ContentAlignment.MiddleLeft;
            ApplyColors();
            FitToMessage();
        }

        private static void CloseCurrentToast()
        {
            if (_currentToast == null || _currentToast.IsDisposed)
                return;

            _currentToast.Close();
            _currentToast = null;
        }

        // How often a toast whose time is up looks whether the mouse has left it.
        private const int HoverCheckMilliseconds = 200;

        // Where the mouse is, in screen coordinates. Replaceable so a test can put it somewhere.
        internal static Func<Point> CursorPosition = () => Cursor.Position;

        // The time is up. A toast the mouse is on stays (so a long message can be read at ease)
        // and closes when the mouse has left it.
        private void OnCloseTimerTick(object sender, EventArgs e)
        {
            _closeTimer.Stop();

            if (Bounds.Contains(CursorPosition()))
            {
                _closeTimer.Interval = HoverCheckMilliseconds;
                _closeTimer.Start();

                return;
            }

            Dismiss();
        }

        private void OnToastClick(object sender, EventArgs e)
        {
            Dismiss();
        }

        // Closed by its own time or by a click (not replaced by a newer toast): the next
        // waiting one is due.
        private void Dismiss()
        {
            if (IsDisposed)
                return;

            _closeTimer.Stop();

            Form owner = _ownerForm;
            Close();
            ShowNextPending(owner);
        }
    }
}
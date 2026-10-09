using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// A small, rounded, self-dismissing confirmation toast (checkmark +
    /// an optional title and the message), closing itself after a short, medium or long <see cref="PopupDuration"/> -
    /// call the static <see cref="ShowToast(string, Form, UIColorSet, PopupTransparency, PopupDuration, PopupDelay)"/> rather than
    /// constructing this directly. Only one toast shows at a time; showing
    /// a new one closes whatever's currently up. The toast belongs to its window: it is
    /// shown only while that window is in front, stays above its window but not above other
    /// windows or programs, moves with the window and never takes the activation.
    /// </summary>
    /// <remarks>
    /// A toast that cannot be shown because its window is not in front is not lost: it waits,
    /// and is shown when the window is activated again. At most <see cref="MaxPendingToasts"/>
    /// wait for one window (the newest; the same text twice counts once), and one that has
    /// waited longer than <see cref="PendingLifetime"/> is dropped, as it would be out of date.
    /// Several waiting toasts are shown one after the other. A window that closes takes its
    /// waiting toasts with it.
    ///
    /// <para>
    /// A toast without a title and without a message shows nothing. The title is one line (an
    /// ellipsis ends what is too long), in bold; title and message may have their own colors
    /// (<see cref="InfoPopupSection"/>, as in the info popup).
    /// A message without text shows no toast. A call from another thread is carried over to
    /// the thread of the window. The toast is placed inside the window (and on the screen) as
    /// far as it fits; a window narrower than the toast gets it at its left edge. A long message
    /// makes the toast higher, up to four lines (more ends in an ellipsis); its
    /// width stays. The size and the places inside follow the scaling of the screen the window
    /// is on. An ampersand is shown as it is.
    /// </para>
    /// <para>
    /// Compared with the balloon of a <c>NotifyIcon</c> (the standard notification): the text,
    /// a title, the colors of both, and the time it stays (<see cref="Duration"/>) are
    /// offered. Not offered: other icons than the check mark (warning, error), an event for a
    /// click on the toast or for its closing, and a toast that stays until it is clicked.
    /// </para>
    /// </remarks>
    public class ToastForm : PopupFormBase
    {
        // The measures at 96 dpi; ApplyDpi scales them for the screen the window is on.
        private const int ToastWidth96 = 350;
        private const int ToastHeight96 = 80;
        private const int MaxMessageLines = 4;
        private int _dpi = 96;

        /// <summary>How many toasts wait for one window that is not in front; the oldest are dropped.</summary>
        public const int MaxPendingToasts = 3;

        /// <summary>How long a toast waits for its window to come to the front before it is dropped. One minute by default.</summary>
        public static TimeSpan PendingLifetime { get; set; } = TimeSpan.FromSeconds(60);

        private sealed class PendingToast
        {
            public InfoPopupSection Content;
            public DateTime Time;
            public PopupTransparency Transparency;
            public PopupDuration Duration;
            public UIColorSet Colors;
        }

        private static readonly Dictionary<Form, List<PendingToast>> _pending = new Dictionary<Form, List<PendingToast>>();

        private static ToastForm _currentToast;
        private Form _ownerForm;

        private Timer _closeTimer;
        private Label _titleLabel;
        private Label _messageLabel;
        private Label _successIcon;
        private InfoPopupSection _content = new InfoPopupSection(null, null);
        private Panel _contentPanel;

        public ToastForm()
        {
            ConfigureForm();
            CreateControls();
            CreateTimer();
            ApplyDpi(DeviceDpi);

        }

        /// <summary>Shows a toast near the bottom-center of <paramref name="owner"/>, closing itself after <paramref name="duration"/>. If <paramref name="owner"/> is not in front, the toast waits until it is.</summary>
        /// <param name="message">The text of the toast.</param>
        /// <param name="owner">The window the toast belongs to and is shown over.</param>
        /// <param name="colors">A color set for the background of the toast (its text is white or black) instead of the neutral gray: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="Color"/>. <c>UIStyles.Toasts.ShowPrimary</c> uses the accent.</param>
        /// <param name="transparency">How see-through the toast is; medium by default.</param>
        /// <param name="duration">How long the toast stays: short, medium (2.5 seconds, the default) or long. The time runs from when it is shown, not from when it was asked for.</param>
        /// <param name="delay">How long to wait before the toast is shown: not at all (the default), 0.2, 0.4 or 0.8 seconds. Whether the window is in front is checked when the wait is over.</param>
        public static void ShowToast(
            string message,
            Form owner,
            UIColorSet colors = null,
            PopupTransparency transparency = PopupTransparency.Medium,
            PopupDuration duration = PopupDuration.Medium,
            PopupDelay delay = PopupDelay.Instant)
        {
            ShowToast(new InfoPopupSection(null, message), owner, colors, transparency, duration, delay);
        }

        /// <summary>Like the call with a message, but with a title above the message and, if wanted, own colors for each (<see cref="InfoPopupSection.HeaderColor"/> for the title, <see cref="InfoPopupSection.TextColor"/> for the message). A part left empty is not shown; with neither there is no toast.</summary>
        /// <param name="content">The title (<see cref="InfoPopupSection.Header"/>) and the message (<see cref="InfoPopupSection.Text"/>).</param>
        /// <param name="owner">The window the toast belongs to and is shown over.</param>
        /// <param name="colors">A color set for the background, as for the call with a message.</param>
        /// <param name="transparency">How see-through the toast is; medium by default.</param>
        /// <param name="duration">How long the toast stays; medium by default.</param>
        /// <param name="delay">How long to wait before the toast is shown; not at all by default.</param>
        public static void ShowToast(
            InfoPopupSection content,
            Form owner,
            UIColorSet colors = null,
            PopupTransparency transparency = PopupTransparency.Medium,
            PopupDuration duration = PopupDuration.Medium,
            PopupDelay delay = PopupDelay.Instant)
        {
            InfoPopupSection[] kept = InfoPopupSection.Normalize(new[] { content });

            if (owner == null || owner.IsDisposed || kept.Length == 0)
                return;

            content = kept[0];

            // A toast is often asked for when a task has finished, on another thread: it is shown
            // on the thread of its window, where it has its message loop and its timer.
            if (owner.IsHandleCreated && owner.InvokeRequired)
            {
                owner.BeginInvoke(new Action(() => ShowToast(content, owner, colors, transparency, duration, delay)));

                return;
            }

            if (delay != PopupDelay.Instant)
            {
                Schedule(content, owner, colors, transparency, duration, delay);

                return;
            }

            if (!PopupSupport.CanShowOver(owner))
            {
                Enqueue(content, owner, colors, transparency, duration);

                return;
            }

            ShowNow(content, owner, colors, transparency, duration);
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

            if (_successIcon != null)
                _successIcon.ForeColor = text;

            if (_titleLabel != null)
                _titleLabel.ForeColor = _content.HeaderColor.IsEmpty ? text : _content.HeaderColor;

            if (_messageLabel != null)
                _messageLabel.ForeColor = _content.TextColor.IsEmpty ? text : _content.TextColor;
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

        // The toasts that wait for their delay; one timer each, gone when it has fired.
        private static readonly List<Timer> _scheduled = new List<Timer>();

        private static void Schedule(InfoPopupSection content, Form owner, UIColorSet colors, PopupTransparency transparency, PopupDuration duration, PopupDelay delay)
        {
            Timer timer = new Timer { Interval = delay.ToMilliseconds() };
            _scheduled.Add(timer);

            timer.Tick += delegate
            {
                timer.Stop();
                _scheduled.Remove(timer);
                timer.Dispose();
                ShowToast(content, owner, colors, transparency, duration, PopupDelay.Instant);
            };

            timer.Start();
        }

        private static void ShowNow(InfoPopupSection content, Form owner, UIColorSet colors, PopupTransparency transparency, PopupDuration duration)
        {
            CloseCurrentToast();

            ToastForm toast = new ToastForm();
            _currentToast = toast;
            toast._ownerForm = owner;
            toast.ApplyDpi(OwnerDpi(owner));
            toast.Transparency = transparency;
            toast.Duration = duration;
            toast.Colors = colors;

            toast.SetContent(content);
            toast.Location = PopupSupport.KeepInside(
                GetToastLocation(owner, toast),
                toast.Size,
                PopupSupport.GetPlacementArea(owner),
                Screen.FromControl(owner).WorkingArea);

            toast._closeTimer.Start();
            toast.Show(owner);
            toast.Follower.Attach(toast, owner);
        }

        // ---- toasts that wait for their window ----

        private static void Enqueue(InfoPopupSection content, Form owner, UIColorSet colors, PopupTransparency transparency, PopupDuration duration)
        {
            List<PendingToast> queue;

            if (!_pending.TryGetValue(owner, out queue))
            {
                queue = new List<PendingToast>();
                _pending[owner] = queue;
                owner.Activated += OnOwnerActivated;
                owner.FormClosed += OnOwnerClosed;
            }

            // The same content twice in a row is one message.
            if (queue.Count > 0 && InfoPopupSection.SameContent(new[] { queue[queue.Count - 1].Content }, new[] { content }))
                return;

            queue.Add(new PendingToast { Content = content, Time = DateTime.UtcNow, Transparency = transparency, Duration = duration, Colors = colors });

            while (queue.Count > MaxPendingToasts)
                queue.RemoveAt(0);
        }

        private static void Forget(Form owner)
        {
            if (!_pending.Remove(owner))
                return;

            owner.Activated -= OnOwnerActivated;
            owner.FormClosed -= OnOwnerClosed;
        }

        private static void OnOwnerActivated(object sender, EventArgs e)
        {
            ShowNextPending((Form)sender);
        }

        private static void OnOwnerClosed(object sender, FormClosedEventArgs e)
        {
            Forget((Form)sender);
        }

        // Shows the oldest toast that waits for <paramref name="owner"/> and is not out of date;
        // the next one follows when this one has closed.
        private static void ShowNextPending(Form owner)
        {
            List<PendingToast> queue;

            if (owner == null || owner.IsDisposed || !_pending.TryGetValue(owner, out queue))
                return;

            DateTime now = DateTime.UtcNow;
            queue.RemoveAll(toast => now - toast.Time >= PendingLifetime);

            if (queue.Count == 0)
            {
                Forget(owner);

                return;
            }

            // Not in front yet: it stays and waits for the next activation.
            if (!PopupSupport.CanShowOver(owner))
                return;

            PendingToast next = queue[0];
            queue.RemoveAt(0);

            if (queue.Count == 0)
                Forget(owner);

            ShowNow(next.Content, owner, next.Colors, next.Transparency, next.Duration);
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

                if (_currentToast == this)
                    _currentToast = null;
            }

            base.Dispose(disposing);
        }

        private void ConfigureForm()
        {
            AutoScaleMode = AutoScaleMode.None;
        }

        private void CreateControls()
        {
            _contentPanel = CreateContentPanel();

            _successIcon = CreateSuccessIcon();
            _titleLabel = CreateTitleLabel();
            _messageLabel = CreateMessageLabel();

            // The one added last is docked first: the title at the top, then the message fills the rest.
            _contentPanel.Controls.Add(_messageLabel);
            _contentPanel.Controls.Add(_titleLabel);
            _contentPanel.Controls.Add(_successIcon);

            Controls.Add(_contentPanel);
            ApplyColors();
        }

        private Panel CreateContentPanel()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = Color.Transparent
            };
        }

        private Label CreateSuccessIcon()
        {
            return new Label
            {
                Text = UIStyles.Symbols.Check,
                Font = UIFonts.Icon,
                Size = new Size(30, 30),
                Location = new Point(10, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
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
                Padding = new Padding(40, 0, 10, 0),
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
                Padding = new Padding(40, 0, 10, 0),
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

        // The size of the toast and the places inside it follow the screen's scaling: a toast on a
        // screen at 150% is 1.5 times as large, so its text keeps its room. The window it is
        // shown for says which screen that is.
        // The scaling of the screen a window is on. Replaceable so a test can give another one.
        internal static Func<Form, int> OwnerDpi = owner => owner.DeviceDpi;

        internal void ApplyDpi(int dpi)
        {
            if (IsDisposed)
                return;

            _dpi = dpi;
            _contentPanel.Padding = new Padding(Scaled(15, dpi), Scaled(10, dpi), Scaled(15, dpi), Scaled(10, dpi));
            _successIcon.Size = new Size(Scaled(30, dpi), Scaled(30, dpi));
            _messageLabel.Padding = new Padding(Scaled(40, dpi), 0, Scaled(10, dpi), 0);
            _titleLabel.Padding = _messageLabel.Padding;
            FitToMessage();
        }

        // The toast is as wide as always and as high as its message needs, from one line (the
        // usual height) to a few lines; what is more than that ends in an ellipsis. The check
        // mark sits in the middle of the height.
        private void FitToMessage()
        {
            int width = Scaled(ToastWidth96, _dpi);
            int textWidth = width - _contentPanel.Padding.Horizontal - _messageLabel.Padding.Horizontal;
            int lineHeight = TextRenderer.MeasureText("Ag", _messageLabel.Font, Size.Empty, TextFormatFlags.NoPadding).Height;
            Size measured = TextRenderer.MeasureText(
                _messageLabel.Text,
                _messageLabel.Font,
                new Size(Math.Max(1, textWidth), int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

            // The title is one line, with a little room to the message below it.
            int titleHeight = 0;

            if (_content.Header != null)
            {
                titleHeight = TextRenderer.MeasureText("Ag", _titleLabel.Font, Size.Empty, TextFormatFlags.NoPadding).Height
                    + Scaled(2, _dpi);
                _titleLabel.Height = titleHeight;
            }

            int textHeight = titleHeight + Math.Min(measured.Height, lineHeight * MaxMessageLines);
            int height = Math.Max(Scaled(ToastHeight96, _dpi), textHeight + 2 * Scaled(20, _dpi));

            Size = new Size(width, height);

            // The room above and below the text is what is left of the height, less a little
            // slack (the label's lines are a bit higher than the measure), so the message shows
            // no more lines than its limit.
            int slack = string.IsNullOrEmpty(_messageLabel.Text) ? 0 : Scaled(6, _dpi);
            int vertical = Math.Max(0, (height - textHeight - slack) / 2);
            _contentPanel.Padding = new Padding(_contentPanel.Padding.Left, vertical, _contentPanel.Padding.Right, vertical);
            _successIcon.Location = new Point(Scaled(10, _dpi), (height - _successIcon.Height) / 2);
        }

        private static int Scaled(int value96, int dpi)
        {
            return (int)Math.Round(value96 * dpi / 96.0);
        }

        private static void CloseCurrentToast()
        {
            if (_currentToast == null || _currentToast.IsDisposed)
                return;

            _currentToast.Close();
            _currentToast = null;
        }

        private static Point GetToastLocation(Form owner, ToastForm toast)
        {
            return owner.PointToScreen(new Point(
                (owner.ClientSize.Width - toast.Width) / 2,
                (int)(owner.ClientSize.Height * 0.75) - toast.Height / 2));
        }

        private void OnCloseTimerTick(object sender, EventArgs e)
        {
            _closeTimer.Stop();

            Form owner = _ownerForm;
            Close();

            // Closed by its own time (not replaced by a newer toast): the next waiting one is due.
            ShowNextPending(owner);
        }
    }
}
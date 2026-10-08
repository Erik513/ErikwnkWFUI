using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// A small, rounded, self-dismissing confirmation toast (checkmark +
    /// message), closing itself after a short, medium or long <see cref="PopupDuration"/> -
    /// call the static <see cref="ShowToast"/> rather than
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
    /// A message without text shows no toast. A call from another thread is carried over to
    /// the thread of the window. The toast is placed inside the window (and on the screen) as
    /// far as it fits; a window narrower than the toast gets it at its left edge. A long message
    /// makes the toast higher, up to four lines (more ends in an ellipsis); its
    /// width stays. The size and the places inside follow the scaling of the screen the window
    /// is on. An ampersand is shown as it is.
    /// </para>
    /// <para>
    /// Compared with the balloon of a <c>NotifyIcon</c> (the standard notification): the text,
    /// the time it stays (<see cref="Duration"/>) and the colors are offered. Not offered: a
    /// title, other icons than the check mark (warning, error), an event for a click on the
    /// toast or for its closing, and a toast that stays until it is clicked.
    /// </para>
    /// </remarks>
    public class ToastForm : Form
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
            public string Message;
            public DateTime Time;
            public PopupTransparency Transparency;
            public PopupDuration Duration;
            public UIColorSet Colors;
        }

        private static readonly Dictionary<Form, List<PendingToast>> _pending = new Dictionary<Form, List<PendingToast>>();

        private static ToastForm _currentToast;
        private Form _ownerForm;

        private readonly OwnerFollower _follower = new OwnerFollower();
        private Timer _closeTimer;
        private Label _messageLabel;
        private Label _successIcon;
        private Panel _contentPanel;

        public ToastForm()
        {
            ConfigureForm();
            CreateControls();
            CreateTimer();
            ApplyDpi(DeviceDpi);

            SizeChanged += OnToastSizeChanged;
            Load += OnToastLoad;
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
            if (owner == null || owner.IsDisposed || string.IsNullOrWhiteSpace(message))
                return;

            // A toast is often asked for when a task has finished, on another thread: it is shown
            // on the thread of its window, where it has its message loop and its timer.
            if (owner.IsHandleCreated && owner.InvokeRequired)
            {
                owner.BeginInvoke(new Action(() => ShowToast(message, owner, colors, transparency, duration, delay)));

                return;
            }

            if (delay != PopupDelay.Instant)
            {
                Schedule(message, owner, colors, transparency, duration, delay);

                return;
            }

            if (!PopupSupport.CanShowOver(owner))
            {
                Enqueue(message, owner, colors, transparency, duration);

                return;
            }

            ShowNow(message, owner, colors, transparency, duration);
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

            if (_messageLabel != null)
                _messageLabel.ForeColor = text;
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

        /// <summary>How see-through the toast is: not at all, slightly, clearly (the default) or strongly.</summary>
        public PopupTransparency Transparency
        {
            get { return _transparency; }
            set
            {
                _transparency = value;

                if (IsDisposed)
                    return;

                Opacity = value.ToOpacity();
                ApplyColors();
            }
        }

        private PopupTransparency _transparency = PopupTransparency.Medium;

        // The toasts that wait for their delay; one timer each, gone when it has fired.
        private static readonly List<Timer> _scheduled = new List<Timer>();

        private static void Schedule(string message, Form owner, UIColorSet colors, PopupTransparency transparency, PopupDuration duration, PopupDelay delay)
        {
            Timer timer = new Timer { Interval = delay.ToMilliseconds() };
            _scheduled.Add(timer);

            timer.Tick += delegate
            {
                timer.Stop();
                _scheduled.Remove(timer);
                timer.Dispose();
                ShowToast(message, owner, colors, transparency, duration, PopupDelay.Instant);
            };

            timer.Start();
        }

        private static void ShowNow(string message, Form owner, UIColorSet colors, PopupTransparency transparency, PopupDuration duration)
        {
            CloseCurrentToast();

            ToastForm toast = new ToastForm();
            _currentToast = toast;
            toast._ownerForm = owner;
            toast.ApplyDpi(OwnerDpi(owner));
            toast.Transparency = transparency;
            toast.Duration = duration;
            toast.Colors = colors;

            toast.SetMessage(message);
            toast.Location = PopupSupport.KeepInside(
                GetToastLocation(owner, toast),
                toast.Size,
                PopupSupport.GetPlacementArea(owner),
                Screen.FromControl(owner).WorkingArea);

            toast._closeTimer.Start();
            toast.Show(owner);
            toast._follower.Attach(toast, owner);
        }

        // ---- toasts that wait for their window ----

        private static void Enqueue(string message, Form owner, UIColorSet colors, PopupTransparency transparency, PopupDuration duration)
        {
            List<PendingToast> queue;

            if (!_pending.TryGetValue(owner, out queue))
            {
                queue = new List<PendingToast>();
                _pending[owner] = queue;
                owner.Activated += OnOwnerActivated;
                owner.FormClosed += OnOwnerClosed;
            }

            // The same text twice in a row is one message.
            if (queue.Count > 0 && queue[queue.Count - 1].Message == message)
                return;

            queue.Add(new PendingToast { Message = message, Time = DateTime.UtcNow, Transparency = transparency, Duration = duration, Colors = colors });

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

            ShowNow(next.Message, owner, next.Colors, next.Transparency, next.Duration);
        }

        // The toast must not take the activation from the window it confirms something in.
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        private const int WS_EX_NOACTIVATE = 0x08000000;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= WS_EX_NOACTIVATE;

                return parameters;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SizeChanged -= OnToastSizeChanged;
                Load -= OnToastLoad;
                _follower.Detach();

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
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            ShowInTaskbar = false;
            Opacity = PopupTransparency.Medium.ToOpacity();
        }

        private void CreateControls()
        {
            _contentPanel = CreateContentPanel();

            _successIcon = CreateSuccessIcon();
            _messageLabel = CreateMessageLabel();

            _contentPanel.Controls.Add(_messageLabel);
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

        private void SetMessage(string message)
        {
            _messageLabel.Text = string.IsNullOrWhiteSpace(message)
                ? ""
                : message;
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

            int textHeight = Math.Min(measured.Height, lineHeight * MaxMessageLines);
            int height = Math.Max(Scaled(ToastHeight96, _dpi), textHeight + 2 * Scaled(20, _dpi));

            Size = new Size(width, height);

            // The room above and below the text is what is left of the height, less a little
            // slack (the label's lines are a bit higher than the measure), so the message shows
            // no more lines than its limit.
            int vertical = Math.Max(0, (height - textHeight - Scaled(6, _dpi)) / 2);
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

        // Win11 rounds window corners itself via DWM (anti-aliased, all four identical, with the
        // thin border of the info popup); a window Region never gets that. Before Windows 11
        // the call does nothing and the toast is a plain rectangle - the same as
        // InfoPopupForm.ApplyRoundedCorners.
        private void ApplyRoundedCorners()
        {
            if (!IsHandleCreated)
                return;

            int pref = DWMWCP_ROUND;
            try
            {
                DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
            }
            catch
            {
                // pre-Win11 - no rounded corners, plain rectangle
            }
        }

        private void OnToastLoad(object sender, EventArgs e)
        {
            ApplyRoundedCorners();
        }

        private void OnToastSizeChanged(object sender, EventArgs e)
        {
            ApplyRoundedCorners();
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
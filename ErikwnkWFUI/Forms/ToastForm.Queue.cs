using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Forms
{
    // Which toast is shown when: the delay, the wait for the window to come to the front, the waiting toasts of a window and the next one when a toast has closed.
    public partial class ToastForm
    {

        // Everything one toast is asked for with; kept while it waits for its delay or its window.
        private sealed class ToastRequest
        {
            public InfoPopupSection Content;
            public UIColorSet Colors;
            public PopupTransparency Transparency;
            public PopupDuration Duration;
            public PopupDelay Delay;
            public ToastIcon Icon;
            public string CustomSymbol;
            public ToastPosition Position;

            // When it began to wait for its window.
            public DateTime Time;

            // The same message with the same symbol and background in the same place.
            public bool IsSameAs(ToastRequest other)
            {
                return Icon == other.Icon
                    && CustomSymbol == other.CustomSymbol
                    && Position == other.Position
                    && SameBackground(Colors, other.Colors)
                    && InfoPopupSection.SameContent(new[] { Content }, new[] { other.Content });
            }
        }

        // A set is not compared as an object (the same color asked for twice makes two sets) but by its background.
        private static bool SameBackground(UIColorSet a, UIColorSet b)
        {
            return a == null || b == null ? a == b : a.Basic == b.Basic;
        }

        private static readonly Dictionary<Form, List<ToastRequest>> _pending = new Dictionary<Form, List<ToastRequest>>();

        // The toasts that wait for their delay; one timer each, gone when it has fired. Nothing here reads the list: the
        // tests use it to reach those timers (to let one fire, or to see that none is left).
        private static readonly List<Timer> _scheduled = new List<Timer>();

        // Shows the toast, or lets it wait for its delay or for its window.
        private static void Present(ToastRequest request, Form owner)
        {
            if (owner.IsDisposed)
                return;

            if (request.Delay != PopupDelay.Instant)
            {
                Schedule(request, owner);

                return;
            }

            ShowOrWait(request, owner);
        }

        private static void Schedule(ToastRequest request, Form owner)
        {
            Timer timer = new Timer { Interval = request.Delay.ToMilliseconds() };
            _scheduled.Add(timer);

            timer.Tick += delegate
            {
                timer.Stop();
                _scheduled.Remove(timer);
                timer.Dispose();

                if (!owner.IsDisposed)
                    ShowOrWait(request, owner);
            };

            timer.Start();
        }

        // Whether the window is in front is looked at now, when the toast is due.
        private static void ShowOrWait(ToastRequest request, Form owner)
        {
            if (PopupSupport.CanShowOver(owner))
                ShowNow(request, owner);
            else
                Enqueue(request, owner);
        }

        private static void ShowNow(ToastRequest request, Form owner)
        {
            CloseCurrentToast();

            ToastForm toast = new ToastForm();
            _currentToast = toast;
            toast._ownerForm = owner;
            toast.ApplyDpi(OwnerDpi(owner));
            toast.Transparency = request.Transparency;
            toast.Duration = request.Duration;
            toast.Colors = request.Colors;
            toast.MessageIcon = request.Icon;
            toast.CustomSymbol = request.CustomSymbol;

            toast.SetContent(request.Content);
            toast.Location = PopupSupport.KeepInside(
                GetToastLocation(GetClientArea(owner), toast.Size, request.Position, Dpi.Scale(EdgeDistance96, toast._dpi)),
                toast.Size,
                PopupSupport.GetPlacementArea(owner),
                Screen.FromControl(owner).WorkingArea);

            toast._closeTimer.Start();
            toast.Show(owner);
            toast.Follower.Attach(toast, owner);
        }

        // ---- toasts that wait for their window ----

        private static void Enqueue(ToastRequest request, Form owner)
        {
            List<ToastRequest> queue;

            if (!_pending.TryGetValue(owner, out queue))
            {
                queue = new List<ToastRequest>();
                _pending[owner] = queue;
                owner.Activated += OnOwnerActivated;
                owner.FormClosed += OnOwnerClosed;
            }

            // The same toast twice in a row is one message.
            if (queue.Count > 0 && queue[queue.Count - 1].IsSameAs(request))
                return;

            request.Time = DateTime.UtcNow;
            queue.Add(request);

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
            List<ToastRequest> queue;

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

            ToastRequest next = queue[0];
            queue.RemoveAt(0);

            if (queue.Count == 0)
                Forget(owner);

            ShowNow(next, owner);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>Something that wants to hear when a setting of Windows (such as the animation effects) has changed.</summary>
    internal interface IAnimationSettingsListener
    {
        /// <summary>Called from the thread of the system events: it must only post a message or the like to the thread of its window.</summary>
        void NotifySettingChanged();
    }

    /// <summary>
    /// Whether Windows lets windows animate (Settings, Accessibility, "Animation effects"), and a way to hear when that changes: a window
    /// gets <c>WM_SETTINGCHANGE</c> only for itself, never for the controls in it, so a control that moves (a label with a moving text, a spinner,
    /// a progress bar with a moving text) cannot hear it itself. It listens to the system for them, and only while at least one of them is
    /// waiting for it.
    /// </summary>
    internal static class AnimationSettings
    {
        private static readonly object Gate = new object();
        private static readonly List<IAnimationSettingsListener> Listeners = new List<IAnimationSettingsListener>();

        // ---- the setting ----

        /// <summary>Whether the animations of Windows are on (where Windows cannot say: yes). Replaceable so a test can switch it.</summary>
        internal static Func<bool> Enabled = Read;

        private static bool Read()
        {
            int on = 1;

            return !SystemParametersInfo(SPI_GETCLIENTAREAANIMATION, 0, ref on, 0) || on != 0;
        }

        // ---- the listeners ----

        /// <summary>The listeners that are waiting.</summary>
        internal static int Count
        {
            get
            {
                lock (Gate)
                    return Listeners.Count;
            }
        }

        internal static void Add(IAnimationSettingsListener listener)
        {
            lock (Gate)
            {
                if (Listeners.Contains(listener))
                    return;

                Listeners.Add(listener);

                if (Listeners.Count == 1)
                    SystemEvents.UserPreferenceChanged += OnPreferenceChanged;
            }
        }

        internal static void Remove(IAnimationSettingsListener listener)
        {
            lock (Gate)
            {
                if (Listeners.Remove(listener) && Listeners.Count == 0)
                    SystemEvents.UserPreferenceChanged -= OnPreferenceChanged;
            }
        }

        // (From the thread of the system events: it only tells each listener, which posts a message.)
        private static void OnPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            Raise();
        }

        /// <summary>Tells every waiting listener; what the system event does, and what a test calls.</summary>
        internal static void Raise()
        {
            IAnimationSettingsListener[] listeners;

            lock (Gate)
                listeners = Listeners.ToArray();

            foreach (IAnimationSettingsListener listener in listeners)
            {
                // One that fails (its window is gone) must not keep the others from hearing it.
                try
                {
                    listener.NotifySettingChanged();
                }
                catch (Exception)
                {
                    // it leaves the list when its window is destroyed
                }
            }
        }
    }

    /// <summary>
    /// The listener of a control that is not a label: it runs <c>whenChanged</c> on the thread of the control (a message is posted to its window), or does
    /// nothing while the control has no window.
    /// </summary>
    internal sealed class ControlAnimationListener : IAnimationSettingsListener
    {
        private readonly Control _owner;
        private readonly Action _whenChanged;

        public ControlAnimationListener(Control owner, Action whenChanged)
        {
            _owner = owner;
            _whenChanged = whenChanged;
        }

        public void NotifySettingChanged()
        {
            if (_owner.IsDisposed || !_owner.IsHandleCreated)
                return;

            _owner.BeginInvoke(_whenChanged);
        }
    }
}

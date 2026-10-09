using System.Collections.Generic;
using Microsoft.Win32;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// Tells the labels that move (or could) when a setting of Windows has changed, such as the animation
    /// effects: a window gets <c>WM_SETTINGCHANGE</c> only for itself, never for the controls in it, so the
    /// labels cannot hear it themselves. It listens to the system for them, and only while at least one label
    /// is waiting for it.
    /// </summary>
    internal static class LabelSettingsWatcher
    {
        private static readonly object Gate = new object();
        private static readonly List<ThemedLabel> Labels = new List<ThemedLabel>();

        // The labels that are waiting.
        internal static int Count
        {
            get
            {
                lock (Gate)
                    return Labels.Count;
            }
        }

        internal static void Add(ThemedLabel label)
        {
            lock (Gate)
            {
                if (Labels.Contains(label))
                    return;

                Labels.Add(label);

                if (Labels.Count == 1)
                    SystemEvents.UserPreferenceChanged += OnPreferenceChanged;
            }
        }

        internal static void Remove(ThemedLabel label)
        {
            lock (Gate)
            {
                if (Labels.Remove(label) && Labels.Count == 0)
                    SystemEvents.UserPreferenceChanged -= OnPreferenceChanged;
            }
        }

        // (From the thread of the system events: it only posts a message to each label.)
        private static void OnPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            Raise();
        }

        // Tells every waiting label; what the event does, and what a test calls.
        internal static void Raise()
        {
            ThemedLabel[] labels;

            lock (Gate)
                labels = Labels.ToArray();

            foreach (ThemedLabel label in labels)
                label.NotifySettingChanged();
        }
    }
}

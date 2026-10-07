using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;

namespace ErikwnkWFUI.Showcase
{
    // The ToastForm tab.
    public partial class ShowcaseForm
    {
        private void AddToastsSection(PropertyTable table)
        {
            table.AddSection("Show");

            table.AddRow("ShowToast", CreateToastButton("Show toast", 160, PopupTransparency.Medium, 0, PopupDuration.Medium));

            table.AddRow("Delayed (5 s)", CreateToastButton("Show toast in 5 seconds", 220, PopupTransparency.Medium, 5000, PopupDuration.Medium));

            table.AddSection("Transparency");

            foreach (PopupTransparency level in new[] { PopupTransparency.Opaque, PopupTransparency.Low, PopupTransparency.Medium, PopupTransparency.High })
            {
                table.AddRow(
                    level == PopupTransparency.Medium ? "Medium (default)" : level.ToString(),
                    CreateToastButton("Show toast", 160, level, 0, PopupDuration.Medium));
            }

            table.AddSection("Duration");

            foreach (PopupDuration duration in new[] { PopupDuration.Short, PopupDuration.Medium, PopupDuration.Long })
            {
                table.AddRow(
                    duration == PopupDuration.Medium ? "Medium (default)" : duration.ToString(),
                    CreateToastButton("Show toast", 160, PopupTransparency.Medium, 0, duration));
            }
        }

        // A delay shows how a toast behaves when its window is not in front by then: switch to
        // another window within the time, and the toast waits and appears when you come back.
        private Button CreateToastButton(string caption, int width, PopupTransparency transparency, int delayMilliseconds, PopupDuration duration)
        {
            Button button = UIStyles.Buttons.CreateStandard(caption, size: new Size(width, 32));

            button.Click += delegate
            {
                string message = "Sample toast message";

                if (delayMilliseconds <= 0)
                {
                    ToastForm.ShowToast(message, this, transparency, duration);

                    return;
                }

                Timer delay = new Timer { Interval = delayMilliseconds };

                delay.Tick += delegate
                {
                    delay.Stop();
                    delay.Dispose();
                    ToastForm.ShowToast(message, this, transparency, duration);
                };

                delay.Start();
            };

            return button;
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Showcase
{
    // The ToastForm tab.
    public partial class ShowcaseForm
    {
        private void AddToastsSection(PropertyTable table)
        {
            table.AddSection("Factory methods");

            table.AddRow("ShowStandard", CreateToastButton("Show toast", 160, null, PopupTransparency.Medium, PopupDuration.Medium, PopupDelay.Instant));
            table.AddRow("ShowPrimary", CreateToastButton("Show toast", 160, UIStyles.Colors.Sets.Primary, PopupTransparency.Medium, PopupDuration.Medium, PopupDelay.Instant));
            table.AddRow("ShowStandard(colors)", CreateToastButton("Show toast", 160, UIStyles.Colors.Sets.Green, PopupTransparency.Medium, PopupDuration.Medium, PopupDelay.Instant));

            table.AddSection("Transparency");

            foreach (PopupTransparency level in new[] { PopupTransparency.Opaque, PopupTransparency.Low, PopupTransparency.Medium, PopupTransparency.High })
            {
                table.AddRow(
                    level == PopupTransparency.Medium ? "Medium (default)" : level.ToString(),
                    CreateToastButton("Show toast", 160, null, level, PopupDuration.Medium, PopupDelay.Instant));
            }

            table.AddSection("Duration");

            foreach (PopupDuration duration in new[] { PopupDuration.Short, PopupDuration.Medium, PopupDuration.Long })
            {
                table.AddRow(
                    duration == PopupDuration.Medium ? "Medium (default)" : duration.ToString(),
                    CreateToastButton("Show toast", 160, null, PopupTransparency.Medium, duration, PopupDelay.Instant));
            }

            table.AddSection("Delay");

            foreach (PopupDelay delay in new[] { PopupDelay.Instant, PopupDelay.Short, PopupDelay.Medium, PopupDelay.Long })
            {
                table.AddRow(
                    delay == PopupDelay.Instant ? "Instant (default)" : delay.ToString(),
                    CreateToastButton("Show toast", 160, null, PopupTransparency.Medium, PopupDuration.Medium, delay));
            }
        }

        private Button CreateToastButton(string caption, int width, UIColorSet colors, PopupTransparency transparency, PopupDuration duration, PopupDelay delay)
        {
            Button button = UIStyles.Buttons.CreateStandard(caption, size: new Size(width, 32));

            button.Click += delegate
            {
                ToastForm.ShowToast("Sample toast message", this, colors, transparency, duration, delay);
            };

            return button;
        }
    }
}

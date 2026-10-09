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

            table.AddSection("CreateStandard / CreatePrimary - the settings as properties");

            Toaster standard = UIStyles.Toasts.CreateStandard(UIStyles.Colors.Sets.Green, PopupTransparency.Low, PopupDuration.Long);
            table.AddRow("CreateStandard(colors)", CreateToasterButton(standard, "Show toast"));
            table.AddRow("CreatePrimary", CreateToasterButton(UIStyles.Toasts.CreatePrimary(), "Show toast"));

            Toaster changing = UIStyles.Toasts.CreateStandard();
            Button cycle = CreateToasterButton(changing, "Show toast");
            cycle.Click += delegate
            {
                changing.Duration = changing.Duration == PopupDuration.Short ? PopupDuration.Long : PopupDuration.Short;
            };
            table.AddRow("Duration changed after each click", cycle);

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

            table.AddSection("Icon");

            foreach (ToastIcon icon in new[] { ToastIcon.None, ToastIcon.Success, ToastIcon.Info, ToastIcon.Warning, ToastIcon.Error })
            {
                ToastIcon shown = icon;
                string message = icon == ToastIcon.Success ? "The file was saved."
                    : icon == ToastIcon.Info ? "A new version is available."
                    : icon == ToastIcon.Warning ? "The disk is almost full."
                    : icon == ToastIcon.Error ? "The file could not be saved."
                    : "Sample toast message";
                Button button = UIStyles.Buttons.CreateStandard("Show toast", size: new Size(160, 32));
                button.Click += delegate { UIStyles.Toasts.ShowStandard(message, this, icon: shown); };
                table.AddRow(icon == ToastIcon.None ? "None (default)" : icon.ToString(), button);
            }

            table.AddRow("Own symbol", CreateCustomSymbolButton(UIStyles.Symbols.Copy, "Copied to the clipboard."));
            table.AddRow("Own symbol (Delete)", CreateCustomSymbolButton(UIStyles.Symbols.Delete, "The entry was deleted."));

            table.AddSection("Position");

            foreach (ToastPosition position in Enum.GetValues(typeof(ToastPosition)))
            {
                ToastPosition shown = position;
                Button button = UIStyles.Buttons.CreateStandard("Show toast", size: new Size(160, 32));
                button.Click += delegate { UIStyles.Toasts.ShowStandard(shown.ToString(), this, position: shown); };
                table.AddRow(position == ToastPosition.BelowCenter ? "BelowCenter (default)" : position.ToString(), button);
            }

            table.AddSection("Content");

            table.AddRow("With a title", CreateContentToastButton(null,
                new InfoPopupSection("Saved", "The file was written to disk.")));

            table.AddRow("A long message", CreateContentToastButton(null,
                new InfoPopupSection("Saved", "The file was written to disk, and a copy of it was put into the backup folder next to it, so nothing is lost.")));

            table.AddRow("Own colors", CreateContentToastButton(null,
                new InfoPopupSection("A red title", "A text in its own light green.")
                {
                    HeaderColor = Color.FromArgb(255, 110, 110),
                    TextColor = Color.FromArgb(150, 255, 150)
                }));

            table.AddRow("On a light color", CreateContentToastButton(UIStyles.Colors.Sets.Yellow,
                new InfoPopupSection("Light background", "The text turns black when white would not read.")));
        }

        private Button CreateCustomSymbolButton(string symbol, string message)
        {
            Button button = UIStyles.Buttons.CreateStandard("Show toast", size: new Size(160, 32));

            button.Click += delegate
            {
                UIStyles.Toasts.ShowStandard(message, this, symbol: symbol);
            };

            return button;
        }

        private Button CreateToasterButton(Toaster toaster, string caption)
        {
            Button button = UIStyles.Buttons.CreateStandard(caption, size: new Size(160, 32));

            button.Click += delegate
            {
                toaster.Show("Sample toast message", this);
            };

            return button;
        }

        private Button CreateContentToastButton(UIColorSet colors, InfoPopupSection content)
        {
            Button button = UIStyles.Buttons.CreateStandard("Show toast", size: new Size(160, 32));

            button.Click += delegate
            {
                ToastForm.ShowToast(content, this, colors);
            };

            return button;
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

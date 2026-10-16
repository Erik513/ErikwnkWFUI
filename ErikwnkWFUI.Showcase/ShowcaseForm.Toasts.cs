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
        private const string SampleToastMessage = "Sample toast message";

        private void AddToastsSection(PropertyTable table)
        {
            UIColorSet accent = UIStyles.Colors.Sets.Primary;

            table.AddSection("Factory methods");

            table.AddRow("ShowStandard", CreateToastButton(() => UIStyles.Toasts.ShowStandard(SampleToastMessage, this, icon: ToastIcon.Success)));
            table.AddRow("ShowPrimary", CreateToastButton(() => UIStyles.Toasts.ShowPrimary(SampleToastMessage, this, icon: ToastIcon.Success)));
            table.AddRow("ShowStandard(colors: Yellow)", CreateToastButton(() => UIStyles.Toasts.ShowStandard(SampleToastMessage, this, UIStyles.Colors.Sets.Yellow, icon: ToastIcon.Success)));

            table.AddSection("CreateStandard / CreatePrimary - the settings as properties");

            Toaster primary = UIStyles.Toasts.CreatePrimary(icon: ToastIcon.Success);
            table.AddRow("CreatePrimary", CreateToastButton(() => primary.Show(SampleToastMessage, this)));

            Toaster standard = UIStyles.Toasts.CreateStandard(UIStyles.Colors.Sets.Yellow, PopupTransparency.Low, PopupDuration.Long, icon: ToastIcon.Success);
            table.AddRow("CreateStandard(colors: Yellow)", CreateToastButton(() => standard.Show(SampleToastMessage, this)));

            Toaster changing = UIStyles.Toasts.CreatePrimary(icon: ToastIcon.Info);
            table.AddRow("Duration changed after each click", CreateToastButton(() =>
            {
                changing.Show(SampleToastMessage, this);
                changing.Duration = changing.Duration == PopupDuration.Short ? PopupDuration.Long : PopupDuration.Short;
            }));

            table.AddSection("Transparency");

            foreach (PopupTransparency level in new[] { PopupTransparency.Opaque, PopupTransparency.Low, PopupTransparency.Medium, PopupTransparency.High })
            {
                PopupTransparency shown = level;
                table.AddRow(
                    level == PopupTransparency.Medium ? "Medium (default)" : level.ToString(),
                    CreateToastButton(() => UIStyles.Toasts.ShowPrimary(SampleToastMessage, this, shown, icon: ToastIcon.Success)));
            }

            table.AddSection("Duration");

            foreach (PopupDuration duration in new[] { PopupDuration.Short, PopupDuration.Medium, PopupDuration.Long })
            {
                PopupDuration shown = duration;
                table.AddRow(
                    duration == PopupDuration.Medium ? "Medium (default)" : duration.ToString(),
                    CreateToastButton(() => UIStyles.Toasts.ShowPrimary(SampleToastMessage, this, duration: shown, icon: ToastIcon.Success)));
            }

            table.AddSection("Delay");

            foreach (PopupDelay delay in new[] { PopupDelay.Instant, PopupDelay.Short, PopupDelay.Medium, PopupDelay.Long })
            {
                PopupDelay shown = delay;
                table.AddRow(
                    delay == PopupDelay.Instant ? "Instant (default)" : delay.ToString(),
                    CreateToastButton(() => UIStyles.Toasts.ShowPrimary(SampleToastMessage, this, delay: shown, icon: ToastIcon.Success)));
            }

            table.AddSection("Icon");

            foreach (ToastIcon icon in new[] { ToastIcon.None, ToastIcon.Success, ToastIcon.Info, ToastIcon.Warning, ToastIcon.Error })
            {
                ToastIcon shown = icon;
                string message = icon == ToastIcon.Success ? "The file was saved."
                    : icon == ToastIcon.Info ? "A new version is available."
                    : icon == ToastIcon.Warning ? "The disk is almost full."
                    : icon == ToastIcon.Error ? "The file could not be saved."
                    : SampleToastMessage;

                table.AddRow(
                    icon == ToastIcon.None ? "None (default)" : icon.ToString(),
                    CreateToastButton(() => UIStyles.Toasts.ShowPrimary(message, this, icon: shown)));
            }

            table.AddRow("Own symbol", CreateToastButton(() => UIStyles.Toasts.ShowPrimary("Copied to the clipboard.", this, symbol: UIStyles.Symbols.Copy)));
            table.AddRow("Own symbol (Delete)", CreateToastButton(() => UIStyles.Toasts.ShowPrimary("The entry was deleted.", this, symbol: UIStyles.Symbols.Delete)));

            table.AddSection("Position");

            foreach (ToastPosition position in Enum.GetValues(typeof(ToastPosition)))
            {
                ToastPosition shown = position;
                table.AddRow(
                    position == ToastPosition.BelowCenter ? "BelowCenter (default)" : position.ToString(),
                    CreateToastButton(() => UIStyles.Toasts.ShowPrimary(shown.ToString(), this, icon: ToastIcon.Info, position: shown)));
            }

            table.AddSection("Content");

            InfoPopupSection saved = new InfoPopupSection("Saved", "The file was written to disk.");
            InfoPopupSection longMessage = new InfoPopupSection("Saved", "The file was written to disk, and a copy of it was put into the backup folder next to it, so nothing is lost.");
            InfoPopupSection ownColors = new InfoPopupSection("A red title", "A text in its own light green.")
            {
                HeaderColor = Color.FromArgb(255, 110, 110),
                TextColor = Color.FromArgb(150, 255, 150)
            };
            InfoPopupSection onLight = new InfoPopupSection("Light background", "The text turns black when white would not read.");

            table.AddRow("With a title", CreateToastButton(() => UIStyles.Toasts.ShowStandard(saved, this, accent, icon: ToastIcon.Success)));
            table.AddRow("A long message", CreateToastButton(() => UIStyles.Toasts.ShowStandard(longMessage, this, accent, icon: ToastIcon.Success)));
            table.AddRow("Own colors", CreateToastButton(() => UIStyles.Toasts.ShowStandard(ownColors, this, icon: ToastIcon.Success)));
            table.AddRow("On a light color", CreateToastButton(() => UIStyles.Toasts.ShowStandard(onLight, this, UIStyles.Colors.Sets.Yellow, icon: ToastIcon.Success)));
        }

        // The button of a row: a click shows that row's toast.
        private Button CreateToastButton(Action show)
        {
            Button button = UIStyles.Buttons.CreateStandard("Show toast", size: new Size(160, 32));
            button.Click += delegate { show(); };

            return button;
        }
    }
}

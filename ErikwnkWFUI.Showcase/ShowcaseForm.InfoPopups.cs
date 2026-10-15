using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Showcase
{
    // The InfoPopupForm tab.
    public partial class ShowcaseForm
    {
        private static readonly InfoPopupSection[] SampleBlocks =
        {
            new InfoPopupSection("Info", "This is a sample InfoPopupForm with a longer text that wraps onto a second line.")
        };

        private void AddInfoPopupsSection(PropertyTable table)
        {
            // The popups are rebuilt with the page, so they take the current accent.
            DisposeInfoPopups();

            table.AddSection("Factory methods");

            table.AddRow("CreateStandard", CreateClickButton(UIStyles.InfoPopups.CreateStandard(), "Show popup", 160,
                popup => popup.SetContent(SampleBlocks)));

            table.AddRow("CreatePrimary", CreateClickButton(UIStyles.InfoPopups.CreatePrimary(), "Show popup", 160,
                popup => popup.SetContent(SampleBlocks)));

            table.AddRow("CreateStandard(colors: Green)", CreateClickButton(UIStyles.InfoPopups.CreateStandard(UIStyles.Colors.Sets.Green), "Show popup", 160,
                popup => popup.SetContent(SampleBlocks)));

            table.AddSection("Transparency");

            foreach (PopupTransparency level in new[] { PopupTransparency.Opaque, PopupTransparency.Low, PopupTransparency.Medium, PopupTransparency.High })
            {
                table.AddRow(level == PopupTransparency.Medium ? "Medium (default)" : level.ToString(),
                    CreateClickButton(UIStyles.InfoPopups.CreatePrimary(transparency: level), "Show popup", 160,
                        popup => popup.SetContent(SampleBlocks)));
            }

            table.AddSection("Duration");

            foreach (PopupDuration duration in new[] { PopupDuration.Short, PopupDuration.Medium, PopupDuration.Long })
            {
                table.AddRow(duration == PopupDuration.Medium ? "Medium (default)" : duration.ToString(),
                    CreateClickButton(UIStyles.InfoPopups.CreatePrimary(duration: duration), "Show popup", 160,
                        popup => popup.SetContent(SampleBlocks)));
            }

            table.AddSection("Delay");

            foreach (PopupDelay delay in new[] { PopupDelay.Instant, PopupDelay.Short, PopupDelay.Medium, PopupDelay.Long })
            {
                table.AddRow(delay == PopupDelay.Medium ? "Medium (default)" : delay.ToString(),
                    CreateHoverButton(UIStyles.InfoPopups.CreatePrimary(delay: delay), "Rest on me", 160, true));
            }

            table.AddSection("Fixed size - one short value, like the 70% over a slider");

            table.AddRow("Standard", CreateClickButton(UIStyles.InfoPopups.CreateStandard(fixedSize: new Size(70, 32)), "Show value 70%", 160,
                popup => popup.SetContent("70%")));

            table.AddRow("Primary", CreateClickButton(UIStyles.InfoPopups.CreatePrimary(new Size(70, 32)), "Show value 70%", 160,
                popup => popup.SetContent("70%")));

            table.AddSection("Content");

            table.AddRow("Several blocks", CreateClickButton(UIStyles.InfoPopups.CreatePrimary(), "Show popup", 160,
                popup => popup.SetContent(
                    new InfoPopupSection("First block", "A header with its text."),
                    new InfoPopupSection("Second block", "Another header with its text."),
                    new InfoPopupSection(null, "A block with a text only."),
                    new InfoPopupSection("A block with a header only", null))));

            table.AddRow("Own colors", CreateClickButton(UIStyles.InfoPopups.CreateStandard(), "Show popup", 160,
                popup => popup.SetContent(
                    new InfoPopupSection("A red header", "A plain text, white on the gray.") { HeaderColor = Color.FromArgb(255, 110, 110) },
                    new InfoPopupSection("A plain header", "A text in its own light green.") { TextColor = Color.FromArgb(150, 255, 150) })));

            table.AddRow("On a light color", CreateClickButton(UIStyles.InfoPopups.CreateStandard(UIStyles.Colors.Sets.Yellow), "Show popup", 160,
                popup => popup.SetContent("Light background", "The text turns black when white would not read.")));

            table.AddSection("AttachTo - the popup as a tooltip");

            InfoPopupForm tooltip = UIStyles.InfoPopups.CreatePrimary();
            _infoPopups.Add(tooltip);

            Button textButton = UIStyles.Buttons.CreateStandard("Rest the mouse on me", size: new Size(200, 32));
            tooltip.AttachTo(textButton, "A plain text as the tooltip.");
            table.AddRow("Text", textButton);

            Button blocksButton = UIStyles.Buttons.CreateStandard("Header and text", size: new Size(200, 32));
            tooltip.AttachTo(blocksButton, new InfoPopupSection("Save", "Writes the file to disk."), new InfoPopupSection("Shortcut", "Ctrl+S"));
            table.AddRow("Blocks", blocksButton);

            Button clockButton = UIStyles.Buttons.CreateStandard("The time when it shows", size: new Size(200, 32));
            tooltip.AttachTo(clockButton, () => new[] { new InfoPopupSection("Now", DateTime.Now.ToString("T")) });
            table.AddRow("Function", clockButton);

            Button disabledButton = UIStyles.Buttons.CreateStandard("Disabled", size: new Size(200, 32));
            disabledButton.Enabled = false;
            tooltip.AttachTo(disabledButton, "Disabled", "Also a disabled control can say why.");
            table.AddRow("Disabled control", disabledButton);

            table.AddSection("Places");

            table.AddRow("ShowNextTo", CreateClickButton(UIStyles.InfoPopups.CreatePrimary(), "Show popup", 160,
                popup => popup.SetContent(SampleBlocks)));

            table.AddRow("ShowAtMouse", CreateHoverButton(UIStyles.InfoPopups.CreatePrimary(), "Move over me", 160, false));
            table.AddRow("ShowCenteredAbove", CreateCenteredAboveButton(UIStyles.InfoPopups.CreatePrimary(new Size(70, 32)), "Click anywhere on me", 160));
            table.AddRow("Delayed at mouse", CreateHoverButton(UIStyles.InfoPopups.CreatePrimary(), "Rest on me", 160, true));
        }

        // A click-to-preview button: the popup is shown next to it and hides itself after its
        // duration (a real consumer shows it on MouseEnter and hides it on MouseLeave; see the
        // hover rows).
        private Button CreateClickButton(InfoPopupForm popup, string caption, int width, Action<InfoPopupForm> setContent)
        {
            Button button = UIStyles.Buttons.CreateStandard(caption, size: new Size(width, 32));
            _infoPopups.Add(popup);

            button.Click += delegate
            {
                setContent(popup);
                popup.ShowNextTo(button);
            };

            return button;
        }

        // Shown while the mouse is over the button, at the mouse - at once, or after a delay.
        private Button CreateHoverButton(InfoPopupForm popup, string caption, int width, bool delayed)
        {
            Button button = UIStyles.Buttons.CreateStandard(caption, size: new Size(width, 32));
            _infoPopups.Add(popup);

            button.MouseMove += delegate
            {
                if (delayed)
                {
                    popup.ShowAtMouseDelayed(button, Cursor.Position, SampleBlocks);
                }
                else
                {
                    popup.SetContent(SampleBlocks);
                    popup.ShowAtMouse(button, Cursor.Position);
                }
            };

            button.MouseLeave += delegate
            {
                popup.CancelPendingShow();
                popup.Hide();
            };

            return button;
        }

        // A value over the point you click, like the readout over a slider thumb.
        private Button CreateCenteredAboveButton(InfoPopupForm popup, string caption, int width)
        {
            Button button = UIStyles.Buttons.CreateStandard(caption, size: new Size(width, 32));
            _infoPopups.Add(popup);

            button.MouseDown += (sender, e) =>
            {
                popup.SetContent((int)Math.Round(100.0 * e.X / button.Width) + "%");
                popup.ShowCenteredAbove(button, e.X);
            };

            return button;
        }

        private void DisposeInfoPopups()
        {
            foreach (InfoPopupForm popup in _infoPopups)
                popup.Dispose();

            _infoPopups.Clear();
        }
    }
}

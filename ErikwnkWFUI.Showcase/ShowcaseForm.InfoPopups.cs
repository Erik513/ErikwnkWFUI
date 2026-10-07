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

            table.AddRow("CreateStandard(colors)", CreateClickButton(UIStyles.InfoPopups.CreateStandard(UIStyles.Colors.Sets.Green), "Show popup", 160,
                popup => popup.SetContent(SampleBlocks)));

            table.AddRow("Fixed: Standard", CreateClickButton(UIStyles.InfoPopups.CreateStandard(fixedSize: new Size(70, 32)), "Show popup", 160,
                popup => popup.SetContent("70%")));

            table.AddRow("Fixed: Primary", CreateClickButton(UIStyles.InfoPopups.CreatePrimary(new Size(70, 32)), "Show popup", 160,
                popup => popup.SetContent("70%")));

            table.AddSection("Content");

            table.AddRow("Several blocks", CreateClickButton(UIStyles.InfoPopups.CreateStandard(), "Show popup", 160,
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

            table.AddSection("Places");

            table.AddRow("ShowNextTo", CreateClickButton(UIStyles.InfoPopups.CreateStandard(), "Show popup", 160,
                popup => popup.SetContent(SampleBlocks)));

            table.AddRow("ShowAtMouse", CreateHoverButton(UIStyles.InfoPopups.CreateStandard(), "Move over me", 160, false));
            table.AddRow("ShowCenteredAbove", CreateCenteredAboveButton(UIStyles.InfoPopups.CreateStandard(fixedSize: new Size(70, 32)), "Show popup", 160));
            table.AddRow("Delayed at mouse", CreateHoverButton(UIStyles.InfoPopups.CreateStandard(), "Rest on me", 160, true));
        }

        // A click-to-preview button: the popup is shown next to it and a timer hides it again
        // (a real consumer shows it on MouseEnter and hides it on MouseLeave; see the hover rows).
        private Button CreateClickButton(InfoPopupForm popup, string caption, int width, Action<InfoPopupForm> setContent)
        {
            Button button = UIStyles.Buttons.CreateStandard(caption, size: new Size(width, 32));
            Timer hideTimer = RegisterInfoPopup(popup);

            hideTimer.Tick += delegate
            {
                hideTimer.Stop();
                popup.Hide();
            };

            button.Click += delegate
            {
                setContent(popup);
                popup.ShowNextTo(button);
                hideTimer.Stop();
                hideTimer.Start();
            };

            return button;
        }

        // Shown while the mouse is over the button, at the mouse - at once, or after a delay.
        private Button CreateHoverButton(InfoPopupForm popup, string caption, int width, bool delayed)
        {
            Button button = UIStyles.Buttons.CreateStandard(caption, size: new Size(width, 32));
            RegisterInfoPopup(popup);

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
            Timer hideTimer = RegisterInfoPopup(popup);

            hideTimer.Tick += delegate
            {
                hideTimer.Stop();
                popup.Hide();
            };

            button.MouseDown += (sender, e) =>
            {
                popup.SetContent((int)Math.Round(100.0 * e.X / button.Width) + "%");
                popup.ShowCenteredAbove(button, e.X);
                hideTimer.Stop();
                hideTimer.Start();
            };

            return button;
        }

        private Timer RegisterInfoPopup(InfoPopupForm popup)
        {
            Timer hideTimer = new Timer { Interval = 3000 };

            _infoPopups.Add(popup);
            _infoPopupTimers.Add(hideTimer);

            return hideTimer;
        }

        private void DisposeInfoPopups()
        {
            foreach (Timer timer in _infoPopupTimers)
            {
                timer.Stop();
                timer.Dispose();
            }

            foreach (InfoPopupForm popup in _infoPopups)
                popup.Dispose();

            _infoPopupTimers.Clear();
            _infoPopups.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using MessageBox = ErikwnkWFUI.Forms.MessageBox;
using MessageBoxButtons = ErikwnkWFUI.Forms.MessageBoxButtons;
using MessageBoxIcon = ErikwnkWFUI.Forms.MessageBoxIcon;

namespace ErikwnkWFUI.Showcase
{
    // The buttons that open MessageBox, ToastForm and InfoPopupForm.
    public partial class ShowcaseForm
    {
        private void AddPopupsSection(PropertyTable table)
        {
            table.AddSection("Popups");

            Button messageBoxButton = UIStyles.Buttons.CreateStandard("Show MessageBox", size: new Size(200, 32));
            messageBoxButton.Click += delegate
            {
                MessageBox.Show(
                    "This is a sample MessageBox.",
                    "Sample",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    this);
            };

            Button toastButton = UIStyles.Buttons.CreateStandard("Show ToastForm", size: new Size(160, 32));
            toastButton.Click += delegate
            {
                ToastForm.ShowToast("Sample toast message", this);
            };

            // The popups are rebuilt with the page, so they take the current accent.
            DisposeInfoPopups();

            table.AddRow("MessageBox", messageBoxButton);
            table.AddRow("ToastForm", toastButton);
            table.AddRow("InfoPopups.CreateStandard", CreateInfoPopupButton(UIStyles.InfoPopups.CreateStandard("Info"), "This is a sample InfoPopupForm.", "Show InfoPopupForm", 180));
            table.AddRow("InfoPopups.CreatePrimary", CreateInfoPopupButton(UIStyles.InfoPopups.CreatePrimary("Info"), "This is a sample InfoPopupForm.", "Show InfoPopupForm", 180));
            table.AddRow("InfoPopups.CreateStandardCompact", CreateInfoPopupButton(UIStyles.InfoPopups.CreateStandardCompact(new Size(70, 32)), "70%", "Show compact InfoPopupForm", 220));
            table.AddRow("InfoPopups.CreatePrimaryCompact", CreateInfoPopupButton(UIStyles.InfoPopups.CreatePrimaryCompact(new Size(70, 32)), "70%", "Show compact InfoPopupForm", 220));
        }

        // InfoPopupForm is designed as a hover tooltip - real consumers show it on MouseEnter
        // and hide it on MouseLeave. A click-to-preview button has no such pairing, so a timer
        // hides the popup again; the same timer restarts on every click.
        private Button CreateInfoPopupButton(InfoPopupForm popup, string text, string caption, int width)
        {
            Button button = UIStyles.Buttons.CreateStandard(caption, size: new Size(width, 32));
            Timer hideTimer = new Timer { Interval = 3000 };

            hideTimer.Tick += delegate
            {
                hideTimer.Stop();
                popup.Hide();
            };

            button.Click += delegate
            {
                popup.ShowInfo(text, button);
                hideTimer.Stop();
                hideTimer.Start();
            };

            _infoPopups.Add(popup);
            _infoPopupTimers.Add(hideTimer);

            return button;
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

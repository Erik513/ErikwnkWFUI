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

            Button infoPopupButton = UIStyles.Buttons.CreateStandard("Show InfoPopupForm", size: new Size(180, 32));
            infoPopupButton.Click += delegate
            {
                _infoPopup.ShowInfo("This is a sample InfoPopupForm.", infoPopupButton);

                // InfoPopupForm is designed as a hover tooltip - real
                // consumers show it on MouseEnter and hide it on MouseLeave.
                // A click-to-preview button has no such pairing, so without
                // this it would just stay open forever; restart the same
                // timer on every click instead of leaking a new one each time.
                _infoPopupHideTimer.Stop();
                _infoPopupHideTimer.Start();
            };

            Button compactInfoPopupButton = UIStyles.Buttons.CreateStandard("Show InfoPopupForm (Compact)", size: new Size(220, 32));
            compactInfoPopupButton.Click += delegate
            {
                // Fixed-size mode meant for a short value like a slider's
                // percentage (see VolumeSlider) - not the free-form text the
                // plain demo above uses.
                _compactInfoPopup.ShowInfo("70%", compactInfoPopupButton);

                _compactInfoPopupHideTimer.Stop();
                _compactInfoPopupHideTimer.Start();
            };

            table.AddRow("MessageBox", messageBoxButton);
            table.AddRow("ToastForm", toastButton);
            table.AddRow("InfoPopupForm", infoPopupButton);
            table.AddRow("InfoPopupForm.Compact", compactInfoPopupButton);
        }
    }
}

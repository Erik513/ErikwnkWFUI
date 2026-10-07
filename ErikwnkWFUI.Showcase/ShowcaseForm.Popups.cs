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
    // The buttons that open MessageBox and ToastForm.
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

            table.AddRow("MessageBox", messageBoxButton);
            table.AddRow("ToastForm", toastButton);

            foreach (PopupTransparency level in new[] { PopupTransparency.Opaque, PopupTransparency.Low, PopupTransparency.High })
            {
                PopupTransparency shown = level;
                Button levelButton = UIStyles.Buttons.CreateStandard("Show ToastForm (" + shown + ")", size: new Size(200, 32));
                levelButton.Click += delegate { ToastForm.ShowToast("Sample toast message", this, shown); };
                table.AddRow("ToastForm (" + shown + ")", levelButton);
            }
        }
    }
}

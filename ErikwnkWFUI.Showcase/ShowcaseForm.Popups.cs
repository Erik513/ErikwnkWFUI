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
    // The button that opens MessageBox (ToastForm and InfoPopupForm have tabs of their own).
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

            table.AddRow("MessageBox", messageBoxButton);
        }
    }
}

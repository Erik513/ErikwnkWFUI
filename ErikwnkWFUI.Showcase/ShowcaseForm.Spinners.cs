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
using ProgressBar = System.Windows.Forms.ProgressBar;
using TrackBar = ErikwnkWFUI.Controls.TrackBar;

namespace ErikwnkWFUI.Showcase
{
    // The spinners.
    public partial class ShowcaseForm
    {
        // Like the progress bars: one row for each factory method, the variants of a property side by side in two columns (the label
        // names them from left to right). The plain rows show a default and a larger, heavier ring; the progress rows a ring of 40 and
        // of 48 px (smaller rings leave the digits out). The status color and the percentage need a load that counts up, so those spinners
        // run on the shared animation timer (like the ProgressBar rows). The control always draws a centred circle, so a stretched
        // column is fine.
        private void AddSpinnerSection(PropertyTable table)
        {
            table.AddSection("Spinners (size: 24 (default) | size: 40, thickness: 5)");

            AddSpinnerRow(table, "CreateStandard",
                UIStyles.Spinners.CreateStandard(),
                UIStyles.Spinners.CreateStandard(40, 5));
            AddSpinnerRow(table, "CreatePrimary",
                UIStyles.Spinners.CreatePrimary(),
                UIStyles.Spinners.CreatePrimary(40, 5));
            AddSpinnerRow(table, "CreateStandard(colors: Yellow)",
                UIStyles.Spinners.CreateStandard(24, 0, UIStyles.Colors.Sets.Yellow),
                UIStyles.Spinners.CreateStandard(40, 5, UIStyles.Colors.Sets.Yellow));
            AddSpinnerRow(table, "CreateGreen",
                UIStyles.Spinners.CreateGreen(),
                UIStyles.Spinners.CreateGreen(40, 5));
            AddSpinnerRow(table, "CreateStatus",
                UIStyles.Spinners.CreateStatus(),
                UIStyles.Spinners.CreateStatus(40, 5), animate: true);

            table.AddSection("Spinners: progress (size: 40 (default) | size: 48, thickness: 5)");

            AddSpinnerRow(table, "CreateProgressStandard",
                UIStyles.Spinners.CreateProgressStandard(),
                UIStyles.Spinners.CreateProgressStandard(48, 5), animate: true, rowHeight: 56);
            AddSpinnerRow(table, "CreateProgressPrimary",
                UIStyles.Spinners.CreateProgressPrimary(),
                UIStyles.Spinners.CreateProgressPrimary(48, 5), animate: true, rowHeight: 56);
            AddSpinnerRow(table, "CreateProgressStandard(colors: Yellow)",
                UIStyles.Spinners.CreateProgressStandard(40, 0, UIStyles.Colors.Sets.Yellow),
                UIStyles.Spinners.CreateProgressStandard(48, 5, UIStyles.Colors.Sets.Yellow), animate: true, rowHeight: 56);
            AddSpinnerRow(table, "CreateProgressGreen",
                UIStyles.Spinners.CreateProgressGreen(),
                UIStyles.Spinners.CreateProgressGreen(48, 5), animate: true, rowHeight: 56);
            AddSpinnerRow(table, "CreateProgressStatus",
                UIStyles.Spinners.CreateProgressStatus(),
                UIStyles.Spinners.CreateProgressStatus(48, 5), animate: true, rowHeight: 56);

            table.AddSection("Spinners: states, text and speed");

            Spinner stopped = UIStyles.Spinners.CreatePrimary(40, 5);
            stopped.IsSpinning = false;
            Spinner disabled = UIStyles.Spinners.CreatePrimary(40, 5);
            disabled.Enabled = false;
            AddSpinnerRow(table, "IsSpinning = false | Enabled = false", stopped, disabled);

            Spinner slow = UIStyles.Spinners.CreateGreen(40, 5);
            slow.Speed = SpinnerSpeed.Slow;
            Spinner medium = UIStyles.Spinners.CreateGreen(40, 5);
            Spinner fast = UIStyles.Spinners.CreateGreen(40, 5);
            fast.Speed = SpinnerSpeed.Fast;
            slow.Anchor = medium.Anchor = fast.Anchor = AnchorStyles.Left;
            table.AddRow("Speed = Slow | Medium | Fast",
                PropertyColumn.Percent(slow, 100f / 3),
                PropertyColumn.Percent(medium, 100f / 3),
                PropertyColumn.Percent(fast, 100f / 3));

            Spinner determinate = UIStyles.Spinners.CreateProgressPrimary();
            determinate.IsIndeterminate = false;
            Spinner determinateStatus = UIStyles.Spinners.CreateProgressStatus();
            determinateStatus.IsIndeterminate = false;
            AddSpinnerRow(table, "IsIndeterminate = false", determinate, determinateStatus, animate: true);

            Spinner right = UIStyles.Spinners.CreatePrimary(32);
            right.Text = "Loading data...";
            right.Width = 170;
            Spinner below = UIStyles.Spinners.CreatePrimary(60);
            below.Text = "Please wait";
            below.TextPosition = SpinnerTextPosition.Below;
            below.Width = 120;
            below.Height = 84;
            AddSpinnerRow(table, "Text | TextPosition = Below", right, below, rowHeight: 96);
        }

        private void AddSpinnerRow(PropertyTable table, string label, Spinner a, Spinner b, bool animate = false, int rowHeight = 0)
        {
            a.Anchor = AnchorStyles.Left;
            b.Anchor = AnchorStyles.Left;

            if (animate)
            {
                AnimateProgressBar(v => a.Value = v);
                AnimateProgressBar(v => b.Value = v);
            }

            PropertyColumn left = PropertyColumn.Percent(a, TwoColumnPercent);
            PropertyColumn rightColumn = PropertyColumn.Percent(b, TwoColumnPercent);

            if (rowHeight > 0)
                table.AddRow(label, rowHeight, left, rightColumn);
            else
                table.AddRow(label, left, rightColumn);
        }
    }
}

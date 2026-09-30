using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Showcase
{
    // The first tab: a short guide for someone who has never seen the
    // library's code. Built from plain text blocks, so it reads like a page
    // of documentation rather than a table of controls.
    public partial class ShowcaseForm
    {
        private const int GuideSideMargin = 28;

        private static Control CreateGuidePage()
        {
            List<Control> blocks = new List<Control>();

            blocks.Add(Heading("Welcome to ErikwnkWFUI"));
            blocks.Add(Paragraph(
                "ErikwnkWFUI is a WinForms control library with its own dark and light design: borderless windows with a custom title bar, " +
                "themed buttons, inputs, lists, tables, tabs and dialogs. It targets .NET Framework 4.8 and .NET 8 (Windows). " +
                "This window is its guide - every control has a tab here, and everything you see on it is created with a single line of code."));

            blocks.Add(Heading("How to read this window"));
            blocks.Add(Paragraph(
                "- One tab per control. The text at the top of each tab says what the control is and how its variants differ.\n" +
                "- One row per factory method. The name on the left (for example CreatePrimary) is the method you call.\n" +
                "- The columns show the same control in its states, usually enabled and disabled.\n" +
                "- Theme and accent color are switched in the bar at the top; every page is rebuilt with the new look.\n" +
                "- The buttons on the right open the dialogs and popups. They belong to no single control, so they sit outside the tabs."));

            blocks.Add(Heading("Getting a control"));
            blocks.Add(Paragraph(
                "Everything goes through one class, UIStyles. Each kind of control has a nested class with factory methods - pick one, " +
                "set the usual properties (Location, Size, Text ...) and add it to your form. The result is a normal WinForms control."));
            blocks.Add(Code(
                "Button save = UIStyles.Buttons.CreatePrimary(\"Save\");\n" +
                "CheckBox remember = UIStyles.CheckBoxes.CreateStandard(\"Remember me\", true);\n" +
                "ToggleSwitch sound = UIStyles.ToggleSwitches.CreateStandard(true, \"On\", \"Off\");"));

            blocks.Add(Heading("Standard and Primary"));
            blocks.Add(Paragraph(
                "Almost every control comes as CreateStandard and CreatePrimary. Standard is the neutral look. Primary uses the current accent color " +
                "for whatever marks a selection or an emphasis: the selected row, the bar under the selected tab, the filled part of a slider. " +
                "Use Primary for the main thing on a screen and Standard for the rest."));
            blocks.Add(Paragraph(
                "Some controls have more variants where they make sense: buttons also come in Green (confirm) and Red (delete), progress bars in " +
                "Green and Status (the color follows the value) and in Transparent versions, panels in several background shades."));

            blocks.Add(Heading("Read-only and editable"));
            blocks.Add(Paragraph(
                "Tables and tab controls come in two kinds. The ReadOnly variants (CreateReadOnlyStandard, CreateReadOnlyPrimary) only show what you " +
                "give them: the user can sort a table or switch a tab, nothing more. The editable variants (CreateStandard, CreatePrimary) let the " +
                "user add, delete, cut and paste rows, or add, rename and close tabs. Pick the lighter one unless you need editing."));

            blocks.Add(Heading("Theme, accent color and language"));
            blocks.Add(Paragraph(
                "Two settings change the whole look, plus one for the texts the library brings along (menus, tooltips, dialog buttons). " +
                "Set them once before you build your windows. Many controls follow a later change on their own, but not all, " +
                "so the reliable way to switch at runtime is to rebuild the UI - which is what this window does."));
            blocks.Add(Code(
                "UIStyles.Colors.ApplyTheme(UIThemes.Light);          // or UIThemes.Dark\n" +
                "UIStyles.Colors.SetAccent(UIAccentColors.Purple);    // or any Color\n" +
                "UIStyles.Language = UILanguage.German;               // or UILanguage.English"));

            blocks.Add(Heading("Windows and dialogs"));
            blocks.Add(Paragraph(
                "StyledForm is the base of every window: borderless, with the library's own title bar, resizable and draggable. MessageBox, ToastForm " +
                "and InfoPopupForm are the matching dialogs and popups - try them with the buttons on the right."));
            blocks.Add(Code(
                "using (var form = new StyledForm(\"My App\"))\n" +
                "{\n" +
                "    form.ContentPanel.Controls.Add(save);\n" +
                "    Application.Run(form);\n" +
                "}"));

            blocks.Add(Heading("Good to know"));
            blocks.Add(Paragraph(
                "- Disabled controls get a muted look of their own; look at the last column of most tabs.\n" +
                "- Most controls also have properties for a one-off color change on a single instance.\n" +
                "- The drop-down list of a ComboBox and the arrows of a NumericUpDown are drawn by Windows and keep the system colors."));

            Panel scroll = UIStyles.Panels.CreateMedium();
            scroll.Dock = DockStyle.Fill;
            scroll.AutoScroll = true;
            scroll.Padding = new Padding(0, 8, 0, 24);

            // Docked top, so added last-to-first to end up in reading order.
            for (int i = blocks.Count - 1; i >= 0; i--)
                scroll.Controls.Add(blocks[i]);

            return scroll;
        }

        private static Control Heading(string text)
        {
            Label label = UIStyles.Labels.CreateTitle(text);
            label.BackColor = Color.Transparent;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Dock = DockStyle.Fill;

            Panel block = UIStyles.Panels.CreateMedium();
            block.Dock = DockStyle.Top;
            block.Height = label.Font.Height + 28;
            block.Padding = new Padding(GuideSideMargin, 16, GuideSideMargin, 4);
            block.Controls.Add(label);

            return block;
        }

        private static Control Paragraph(string text)
        {
            return CreateTextBlock(UIStyles.Panels.CreateMedium(), text, null, new Padding(GuideSideMargin, 2, GuideSideMargin, 6));
        }

        // A block of code in a monospaced font, on the darker background.
        private static Control Code(string text)
        {
            Panel panel = UIStyles.Panels.CreateDark();
            Font font = new Font("Consolas", 9.5f);

            Panel wrapper = UIStyles.Panels.CreateMedium();
            wrapper.Dock = DockStyle.Top;
            wrapper.Padding = new Padding(GuideSideMargin, 4, GuideSideMargin, 10);

            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(14, 10, 14, 10);

            Label label = UIStyles.Labels.CreateNormal(text);
            label.AutoSize = false;
            label.AutoEllipsis = false;
            label.Font = font;
            label.TextAlign = ContentAlignment.TopLeft;
            label.Dock = DockStyle.Fill;
            label.Disposed += delegate { font.Dispose(); };
            panel.Controls.Add(label);

            int lines = text.Split('\n').Length;
            wrapper.Height = lines * font.Height + panel.Padding.Vertical + wrapper.Padding.Vertical;
            wrapper.Controls.Add(panel);

            return wrapper;
        }

        // A text that wraps at the width of its block and makes the block
        // just tall enough for it.
        private static Control CreateTextBlock(Panel block, string text, Font font, Padding padding)
        {
            block.Dock = DockStyle.Top;
            block.Padding = padding;

            Label label = UIStyles.Labels.CreateNormal(text);
            label.AutoSize = false;
            label.AutoEllipsis = false;
            label.TextAlign = ContentAlignment.TopLeft;
            label.Dock = DockStyle.Fill;

            if (font != null)
                label.Font = font;

            block.Controls.Add(label);

            block.SizeChanged += delegate { FitDescriptionHeight(block, label); };
            FitDescriptionHeight(block, label);

            return block;
        }
    }
}

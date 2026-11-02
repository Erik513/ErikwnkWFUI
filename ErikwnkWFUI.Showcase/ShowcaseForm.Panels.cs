using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkCore.Updater;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using MessageBox = ErikwnkWFUI.Forms.MessageBox;
using MessageBoxButtons = ErikwnkWFUI.Forms.MessageBoxButtons;
using MessageBoxIcon = ErikwnkWFUI.Forms.MessageBoxIcon;
using FlowLayoutPanel = System.Windows.Forms.FlowLayoutPanel;
using Panel = System.Windows.Forms.Panel;
using TableLayoutPanel = System.Windows.Forms.TableLayoutPanel;

namespace ErikwnkWFUI.Showcase
{
    // The panels: flat and card panels, the flow, the cells, the version panel.
    public partial class ShowcaseForm
    {
        private void AddPanelsSection(PropertyTable table)
        {
            UIColorSet yellow = UIStyles.Colors.Sets.Yellow;
            const int swatchHeight = 42;

            table.AddSection("Panels");

            // The panels in each background of the theme, one row per factory method.
            (string Name, Panel Panel)[] swatches =
            {
                ("CreateStandard", UIStyles.Panels.CreateStandard()),
                ("CreatePrimary", UIStyles.Panels.CreatePrimary()),
                ("CreateStandard(colors: Yellow)", UIStyles.Panels.CreateStandard(yellow)),
                ("CreateDark", UIStyles.Panels.CreateDark()),
                ("CreateMedium", UIStyles.Panels.CreateMedium()),
                ("CreateElevated", UIStyles.Panels.CreateElevated())
            };

            foreach ((string name, Panel panel) in swatches)
            {
                // (The table does not stretch a control to the height of its row.)
                panel.MinimumSize = new Size(100, swatchHeight - 8);
                panel.Height = swatchHeight - 8;
                panel.Controls.Add(CreatePanelCaption(name, panel));
                table.AddRow(name, panel);
            }

            // BorderStyle alone: the frame in the color of the theme (BorderColor not set).
            Panel framed = UIStyles.Panels.CreateMedium();
            framed.BorderStyle = BorderStyle.FixedSingle;
            framed.Size = new Size(260, 50);
            framed.MinimumSize = framed.Size;
            framed.MaximumSize = framed.Size;
            framed.Dock = DockStyle.None;
            framed.Controls.Add(new Label { Text = "BorderStyle = FixedSingle", AutoSize = true, Location = new Point(6, 6), ForeColor = UIColors.TextPrimary, BackColor = Color.Transparent });
            table.AddRow("CreateMedium (BorderStyle = FixedSingle)", 60, framed);

            // The options of a panel in one: a frame (BorderStyle) in the library's color (BorderColor) and AutoScroll
            // (the scroll bars follow the theme, also the square where they meet).
            ErikwnkWFUI.Controls.Panel options = (ErikwnkWFUI.Controls.Panel)UIStyles.Panels.CreateMedium();
            options.BorderStyle = BorderStyle.FixedSingle;
            options.BorderColor = UIColors.Primary;
            options.AutoScroll = true;
            options.Size = new Size(260, 90);
            options.MinimumSize = options.Size;
            options.MaximumSize = options.Size;
            options.Dock = DockStyle.None;
            options.Controls.Add(new Label { Text = "BorderColor, AutoScroll: scroll in both directions", AutoSize = true, Location = new Point(6, 6), ForeColor = UIColors.TextPrimary, BackColor = Color.Transparent });
            options.Controls.Add(new Panel { Location = new Point(6, 30), Size = new Size(520, 160), BackColor = UIColors.PrimaryDark });
            table.AddRow("CreateMedium (BorderStyle, BorderColor, AutoScroll)", 110, options);

            // No background of its own: the page shows through (here, with a line of text on it).
            Panel transparent = UIStyles.Panels.CreateTransparent();
            transparent.Size = new Size(200, 24);
            transparent.MinimumSize = transparent.Size;
            transparent.Controls.Add(CreatePanelCaption("What is behind it shows", transparent));
            table.AddRow("CreateTransparent", transparent);

            table.AddSection("Cards");

            // A card: rounded corners that show the page behind them and a thin line.
            table.AddRow("CreateCardStandard", 92, CreateCardDemo(UIStyles.Panels.CreateCardStandard(), "A card"));
            table.AddRow("CreateCardPrimary", 92, CreateCardDemo(UIStyles.Panels.CreateCardPrimary(), "A card in the accent"));
            table.AddRow("CreateCardStandard(colors: Yellow)", 92, CreateCardDemo(UIStyles.Panels.CreateCardStandard(yellow), "A card in a color set"));

            // A card does not scroll itself (its scroll bars would lie over the rounded corners): a panel with AutoScroll is put in it.
            Panel scrollingCard = UIStyles.Panels.CreateCardStandard();
            scrollingCard.Size = new Size(260, 100);
            scrollingCard.MinimumSize = scrollingCard.Size;
            scrollingCard.MaximumSize = scrollingCard.Size;
            Panel scrollInside = UIStyles.Panels.CreateTransparent();
            scrollInside.Dock = DockStyle.Fill;
            scrollInside.AutoScroll = true;
            scrollInside.Controls.Add(new Label { Text = "A panel with AutoScroll in the card", AutoSize = true, Location = new Point(0, 0), ForeColor = UIColors.TextPrimary, BackColor = Color.Transparent });
            scrollInside.Controls.Add(new Panel { Location = new Point(0, 24), Size = new Size(520, 160), BackColor = UIColors.PrimaryDark });
            scrollingCard.Controls.Add(scrollInside);
            table.AddRow("CreateCardStandard (scrolling panel inside)", 120, scrollingCard);

            table.AddSection("Multi panels");

            AddMultiPanelsSection(table);

            table.AddSection("Table layout panels");

            TableLayoutPanel cells = UIStyles.TableLayoutPanels.CreateStandard(3, 2);
            ConfigureCellsDemo(cells);
            table.AddRow("CreateStandard", 72, cells);

            TableLayoutPanel cellsPrimary = UIStyles.TableLayoutPanels.CreatePrimary(3, 2);
            ConfigureCellsDemo(cellsPrimary);
            table.AddRow("CreatePrimary", 72, cellsPrimary);

            TableLayoutPanel cellsInColor = UIStyles.TableLayoutPanels.CreateStandard(3, 2, yellow);
            ConfigureCellsDemo(cellsInColor);
            table.AddRow("CreateStandard(colors: Yellow)", 72, cellsInColor);

            TableLayoutPanel cellsDark = UIStyles.TableLayoutPanels.CreateDark(3, 2);
            ConfigureCellsDemo(cellsDark);
            table.AddRow("CreateDark", 72, cellsDark);

            TableLayoutPanel cellsTransparent = UIStyles.TableLayoutPanels.CreateTransparent(3, 2);
            ConfigureCellsDemo(cellsTransparent);
            table.AddRow("CreateTransparent", 72, cellsTransparent);

            // BorderStyle alone: the frame around the table in the color of the theme (BorderColor not set).
            TableLayoutPanel cellsFramed = UIStyles.TableLayoutPanels.CreateStandard(3, 2);
            ConfigureCellsDemo(cellsFramed);
            cellsFramed.BorderStyle = BorderStyle.FixedSingle;
            table.AddRow("CreateStandard (BorderStyle = FixedSingle)", 72, cellsFramed);

            // The lines between the cells (CellBorderStyle) are off by default. CellBorderLines selects which are shown: only the
            // ones between the cells, or only the ones around the table.
            ErikwnkWFUI.Controls.TableLayoutPanel inner = (ErikwnkWFUI.Controls.TableLayoutPanel)UIStyles.TableLayoutPanels.CreateStandard(3, 2);
            ConfigureCellsDemo(inner);
            inner.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            inner.CellBorderLines = CellBorderLines.InnerOnly;
            inner.InnerCellBorderColor = UIColors.PrimaryLight;
            table.AddRow("CreateStandard (CellBorderLines: InnerOnly, InnerCellBorderColor)", 72, inner);

            ErikwnkWFUI.Controls.TableLayoutPanel outer = (ErikwnkWFUI.Controls.TableLayoutPanel)UIStyles.TableLayoutPanels.CreateStandard(3, 2);
            ConfigureCellsDemo(outer);
            outer.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            outer.CellBorderLines = CellBorderLines.OuterOnly;
            outer.OuterCellBorderColor = UIColors.PrimaryLight;
            table.AddRow("CreateStandard (CellBorderLines: OuterOnly, OuterCellBorderColor)", 72, outer);

            // Both kinds of lines in their own color: the ones around the table in red, the ones between the cells in green.
            ErikwnkWFUI.Controls.TableLayoutPanel twoColors = (ErikwnkWFUI.Controls.TableLayoutPanel)UIStyles.TableLayoutPanels.CreateStandard(3, 2);
            ConfigureCellsDemo(twoColors);
            twoColors.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            twoColors.OuterCellBorderColor = UIStyles.Colors.Sets.Red.Basic;
            twoColors.InnerCellBorderColor = UIStyles.Colors.Sets.Green.Basic;
            table.AddRow("CreateStandard (OuterCellBorderColor, InnerCellBorderColor)", 72, twoColors);

            table.AddSection("Flow layout panels");

            table.AddRow("CreateStandard", CreateFlowDemo(UIStyles.FlowLayoutPanels.CreateStandard()));
            table.AddRow("CreatePrimary", CreateFlowDemo(UIStyles.FlowLayoutPanels.CreatePrimary()));
            table.AddRow("CreateStandard(colors: Yellow)", CreateFlowDemo(UIStyles.FlowLayoutPanels.CreateStandard(yellow)));
            table.AddRow("CreateTransparent", CreateFlowDemo(UIStyles.FlowLayoutPanels.CreateTransparent()));

            // BorderStyle alone: the frame in the color of the theme (BorderColor not set).
            FlowLayoutPanel framedDefault = UIStyles.FlowLayoutPanels.CreateStandard();
            framedDefault.BorderStyle = BorderStyle.FixedSingle;
            table.AddRow("CreateStandard (BorderStyle = FixedSingle)", CreateFlowDemo(framedDefault));

            // A frame around the panel (BorderStyle) in the library's color.
            ErikwnkWFUI.Controls.FlowLayoutPanel framedFlow = (ErikwnkWFUI.Controls.FlowLayoutPanel)UIStyles.FlowLayoutPanels.CreateStandard();
            framedFlow.BorderStyle = BorderStyle.FixedSingle;
            framedFlow.BorderColor = UIColors.Primary;
            table.AddRow("CreateStandard (BorderStyle, BorderColor)", CreateFlowDemo(framedFlow));

            table.AddSection("Version panels");

            table.AddRow("CreateStandard", CreateVersionPanelDemo(UIStyles.VersionPanels.CreateStandard()));
            table.AddRow("CreatePrimary", CreateVersionPanelDemo(UIStyles.VersionPanels.CreatePrimary()));
            table.AddRow("CreateStandard(colors: Yellow)", CreateVersionPanelDemo(UIStyles.VersionPanels.CreateStandard(yellow)));

            // An update is known: the green button appears next to the version (a click shows the prompt of the AppUpdater).
            VersionPanel updateAvailable = CreateVersionPanelDemo(UIStyles.VersionPanels.CreateStandard());
            updateAvailable.ShowUpdate(
                new AppUpdater("owner", "repo", VersionPanelDemoClient, VersionPanelDemoClient),
                new UpdateCheckResult(new Version(9, 9, 9), "", null),
                new Version(1, 0, 0));
            table.AddRow("CreateStandard (ShowUpdate)", updateAvailable);
        }

        // Not asked at all: the demo update is made up (no download address, no release page).
        private static readonly System.Net.Http.HttpClient VersionPanelDemoClient = new System.Net.Http.HttpClient();

        // The strip does not dock here, so it gets a width of its own.
        private static VersionPanel CreateVersionPanelDemo(VersionPanel panel)
        {
            panel.Dock = DockStyle.None;
            panel.Width = 380;
            panel.MinimumSize = panel.Size;
            panel.MaximumSize = panel.Size;

            return panel;
        }

        // Two buttons in a flow panel, to see the background around them.
        private static FlowLayoutPanel CreateFlowDemo(FlowLayoutPanel flow)
        {
            flow.Controls.Add(UIStyles.Buttons.CreateStandard("One", size: new Size(70, 28)));
            flow.Controls.Add(UIStyles.Buttons.CreateStandard("Two", size: new Size(70, 28)));

            return flow;
        }

        private static Label CreatePanelCaption(string text, Control surface)
        {
            Label caption = UIStyles.Labels.CreateStandard(text);
            caption.AutoSize = true;
            caption.Location = new Point(6, 6);
            caption.BackColor = Color.Transparent;
            FollowSurface(caption, surface);

            return caption;
        }

        // The text of a label is the best one for the surface it stands on (the theme's text color would be dark on the dark
        // accent in the light theme); a transparent surface leaves the text as it is.
        private static void FollowSurface(Label label, Control surface)
        {
            void Update()
            {
                label.ForeColor = surface.BackColor.A == 0 ? UIColors.TextPrimary : (surface.BackColor.GetBrightness() > 0.5f ? Color.Black : Color.White);
            }

            // Later than the surface itself, which changes its color on the same event.
            EventHandler changed = (sender, e) =>
            {
                if (label.IsHandleCreated && !label.IsDisposed)
                    label.BeginInvoke((Action)Update);
            };

            Update();
            UIStyles.Colors.ThemeChanged += changed;
            label.Disposed += (sender, e) => UIStyles.Colors.ThemeChanged -= changed;
        }

        // A card with a line of text in it; it does not dock, so it gets its size here.
        private static Panel CreateCardDemo(Panel card, string text)
        {
            card.Size = new Size(240, 70);
            card.MinimumSize = card.Size;

            Label caption = UIStyles.Labels.CreateStandard(text);
            caption.Dock = DockStyle.Fill;
            caption.BackColor = Color.Transparent;
            FollowSurface(caption, card);
            card.Controls.Add(caption);

            return card;
        }

        // Six cells (no lines by default; CellBorderStyle.Single draws them in the color of the theme or one that is set).
        private static void ConfigureCellsDemo(TableLayoutPanel cells)
        {
            cells.Size = new Size(260, 60);
            cells.MinimumSize = cells.Size;

            for (int i = 0; i < 3; i++)
                cells.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));

            for (int i = 0; i < 2; i++)
                cells.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            for (int i = 0; i < 6; i++)
            {
                Label cell = UIStyles.Labels.CreateStandard("Cell " + (i + 1));
                cell.Dock = DockStyle.Fill;
                cell.BackColor = Color.Transparent;
                FollowSurface(cell, cells);
                cells.Controls.Add(cell);
            }
        }
    }
}

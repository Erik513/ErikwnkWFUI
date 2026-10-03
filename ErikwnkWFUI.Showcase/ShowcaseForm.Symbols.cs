using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Showcase
{
    // The symbols of UIStyles.Symbols, grouped, each with its name.
    public partial class ShowcaseForm
    {
        private static readonly (string Title, string[] Names)[] SymbolGroups =
        {
            ("Actions", new[] { "Add", "Remove", "Delete", "Edit", "Save", "Refresh", "Sync", "Update", "Search", "Settings", "Close", "Cancel", "Check", "Undo", "Redo", "Cut", "Copy", "Paste", "Print", "Download", "Upload", "Share", "Send", "Filter", "Sort", "ZoomIn", "ZoomOut", "Fullscreen" }),
            ("Files", new[] { "File", "Folder", "FolderOpen" }),
            ("Navigation", new[] { "Back", "Forward", "Up", "Down", "Home", "Menu", "ChevronLeft", "ChevronRight", "ChevronUp", "ChevronDown", "TriangleUp", "TriangleDown" }),
            ("Media", new[] { "Play", "Pause", "Stop", "Record", "SkipBack", "SkipForward", "Volume", "Mute", "Music", "Shuffle", "Repeat" }),
            ("Status", new[] { "Info", "Warning", "Error", "Help", "Lightning", "Heart" }),
            ("Window", new[] { "Minimize", "Maximize", "Restore" }),
            ("People, security", new[] { "User", "Logout", "Lock", "Unlock", "Power" }),
            ("Things", new[] { "Mail", "Calendar", "Clock", "Bell", "Link", "Pin", "Globe", "Eye" })
        };

        private const int SymbolCellWidth = 92;
        private const int SymbolCellHeight = 66;

        private void AddSymbolsSection(PropertyTable table)
        {
            Dictionary<string, string> all = typeof(UIStyles.Symbols)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral)
                .ToDictionary(field => field.Name, field => (string)field.GetRawConstantValue());

            List<string> listed = new List<string>();

            foreach ((string title, string[] names) in SymbolGroups)
            {
                listed.AddRange(names);
                table.AddRow(title, GetSymbolRowHeight(names.Length), CreateSymbolGrid(names, all));
            }

            // Anything added to UIStyles.Symbols later shows up here until it is
            // given a group above.
            string[] others = all.Keys.Where(name => !listed.Contains(name)).ToArray();

            if (others.Length > 0)
                table.AddRow("Others", GetSymbolRowHeight(others.Length), CreateSymbolGrid(others, all));
        }

        private static int GetSymbolRowHeight(int count)
        {
            int perRow = 8;
            int rows = (count + perRow - 1) / perRow;

            return rows * SymbolCellHeight + 14;
        }

        private static Control CreateSymbolGrid(string[] names, Dictionary<string, string> all)
        {
            FlowLayoutPanel flow = UIStyles.FlowLayoutPanels.CreateStandard();
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.WrapContents = true;

            foreach (string name in names)
            {
                Panel cell = UIStyles.Panels.CreateMedium();
                cell.Dock = DockStyle.None;
                cell.Size = new Size(SymbolCellWidth, SymbolCellHeight);
                cell.Margin = new Padding(0, 0, 4, 4);

                Label glyph = UIStyles.Labels.CreateNormal(all[name]);
                glyph.AutoEllipsis = false;
                glyph.Font = new Font(UIStyles.Symbols.GetFontName(all[name]), 20f);
                glyph.TextAlign = ContentAlignment.MiddleCenter;
                glyph.SetBounds(0, 4, SymbolCellWidth, 38);
                glyph.Disposed += delegate { glyph.Font.Dispose(); };

                Label caption = UIStyles.Labels.CreateMuted(name);
                caption.TextAlign = ContentAlignment.MiddleCenter;
                caption.SetBounds(0, 44, SymbolCellWidth, 18);

                cell.Controls.Add(glyph);
                cell.Controls.Add(caption);
                flow.Controls.Add(cell);
            }

            return flow;
        }
    }
}

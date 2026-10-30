using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    // Copying the selection (text and an HTML table), the right-click menu and the confirmation.
    public partial class ListView : System.Windows.Forms.ListView
    {

        /// <summary>How a Ctrl+C/Ctrl+Shift+C copy is confirmed. Defaults to <see cref="CopyConfirmationStyle.Toast"/> (unchanged from before this existed).</summary>
        [DefaultValue(CopyConfirmationStyle.Toast)]
        public CopyConfirmationStyle CopyConfirmation { get; set; } = CopyConfirmationStyle.Toast;

        /// <summary>
        /// The colors of the toast that confirms a copy (<see cref="CopyConfirmationStyle.Toast"/>): a color set (one of <c>UIStyles.Colors.Sets</c>,
        /// <c>UIStyles.Colors.CreateSet(color)</c> or a <c>Color</c>), null for the neutral gray. Until it is set it follows the look of the list:
        /// gray for the standard one, the color of the set of <c>CreateStandard(colors)</c>, the accent for <c>CreatePrimary</c>.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIColorSet ToastColors
        {
            get { return _toastColors.Value; }
            set { _toastColors.Value = value; }
        }

        private readonly ColorSetHolder _toastColors = new ColorSetHolder();

        /// <summary>Whether a click with the left mouse button somewhere else in the window (not on a button, a tool bar or a menu) takes the selection away. On by default.</summary>
        [DefaultValue(true)]
        public bool DeselectOnOutsideClick { get; set; } = true;

        // The menu has the shape and the wording of the DataGridViews': Copy, Copy with header | Copy all, Copy all with
        // header | Select all. The items are fields, so that OnUIStringsLanguageChanged can set their texts again.
        private ToolStripMenuItem _copySelectionItem;
        private ToolStripMenuItem _copySelectionWithHeaderItem;
        private ToolStripMenuItem _copyAllItem;
        private ToolStripMenuItem _copyAllWithHeaderItem;
        private ToolStripMenuItem _selectAllItem;

        private ContextMenuStrip BuildContextMenu()
        {
            // (No item has an image: the room for one on the left would stay empty.)
            var menu = new ContextMenuStrip
            {
                ShowImageMargin = false
            };

            menu.FollowSelectionBackColor(() => ContextMenuSelectionColor);

            _copySelectionItem = new ToolStripMenuItem(
                UIStrings.Get("ListView.CopySelection"), null, (sender, e) => CopySelection());
            _copySelectionWithHeaderItem = new ToolStripMenuItem(
                UIStrings.Get("ListView.CopySelectionWithHeader"), null, (sender, e) => CopySelectionAsTable());
            _copyAllItem = new ToolStripMenuItem(
                UIStrings.Get("ListView.CopyAll"), null, (sender, e) => { SelectAll(); CopySelection(); });
            _copyAllWithHeaderItem = new ToolStripMenuItem(
                UIStrings.Get("ListView.CopyAllWithHeader"), null, (sender, e) => { SelectAll(); CopySelectionAsTable(); });
            _selectAllItem = new ToolStripMenuItem(
                UIStrings.Get("ListView.SelectAll"), null, (sender, e) => SelectAll());

            menu.Items.Add(_copySelectionItem);
            menu.Items.Add(_copySelectionWithHeaderItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_copyAllItem);
            menu.Items.Add(_copyAllWithHeaderItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_selectAllItem);

            menu.Opening += (sender, e) =>
            {
                bool hasSelection = SelectedItems.Count > 0;
                bool hasItems = Items.Count > 0;

                _copySelectionItem.Enabled = hasSelection;
                _copySelectionWithHeaderItem.Enabled = hasSelection;
                _copyAllItem.Enabled = hasItems;
                _copyAllWithHeaderItem.Enabled = hasItems;
                _selectAllItem.Enabled = hasItems;
            };

            return menu;
        }

        // The menu is in the language the application switched to.
        private void OnUIStringsLanguageChanged(object sender, EventArgs e)
        {
            if (_copySelectionItem == null)
            {
                return;
            }

            _copySelectionItem.Text = UIStrings.Get("ListView.CopySelection");
            _copySelectionWithHeaderItem.Text = UIStrings.Get("ListView.CopySelectionWithHeader");
            _copyAllItem.Text = UIStrings.Get("ListView.CopyAll");
            _copyAllWithHeaderItem.Text = UIStrings.Get("ListView.CopyAllWithHeader");
            _selectAllItem.Text = UIStrings.Get("ListView.SelectAll");
        }

        // The arrow keys are native; Ctrl+C, Ctrl+Shift+C and Ctrl+A are not and are handled here.
        private void OnListViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift && e.KeyCode == Keys.C)
            {
                CopySelectionAsTable();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelection();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.A)
            {
                SelectAll();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void CopySelection()
        {
            CopySelectionCore(includeHeader: false);
        }

        // Like CopySelection, with a row of column captions in front, so that it pastes as a table.
        private void CopySelectionAsTable()
        {
            CopySelectionCore(includeHeader: true);
        }

        private void CopySelectionCore(bool includeHeader)
        {
            // (In the order shown, not in the order the rows were selected.)
            var selectedItems = SelectedItems.Cast<ListViewItem>().OrderBy(item => item.Index).ToList();
            if (selectedItems.Count == 0)
            {
                return;
            }

            // Every column, in the order shown.
            var orderedColumns = GetColumnsInDisplayOrder();

            List<string> headerCells = null;
            if (includeHeader)
            {
                headerCells = orderedColumns.Select(column => column.Text).ToList();
            }

            var plainTextBuilder = new StringBuilder();
            if (headerCells != null)
            {
                plainTextBuilder.AppendLine(string.Join("\t", headerCells));
            }

            var rows = new List<List<string>>();
            foreach (var item in selectedItems)
            {
                var cells = new List<string>();
                foreach (var column in orderedColumns)
                {
                    var dataColumn = column.Index;
                    cells.Add(dataColumn < item.SubItems.Count ? item.SubItems[dataColumn].Text : string.Empty);
                }

                rows.Add(cells);
                plainTextBuilder.AppendLine(string.Join("\t", cells));
            }

            // Plain text for everything, and HTML beside it, which Word, Outlook, browsers and Excel paste as a table.
            var dataObject = new DataObject();
            dataObject.SetText(plainTextBuilder.ToString(), TextDataFormat.UnicodeText);
            dataObject.SetData(DataFormats.Html, BuildCfHtmlTable(headerCells, rows));

            // (Another program holds the clipboard: nothing was copied, so there is nothing to confirm.)
            if (!ClipboardAccess.TrySetDataObject(dataObject, true))
                return;

            var message = selectedItems.Count == 1
                ? UIStrings.Get("ListView.RowCopied")
                : string.Format(UIStrings.Get("ListView.RowsCopied"), selectedItems.Count);
            ShowCopyToast(includeHeader ? message + UIStrings.Get("ListView.WithHeaderSuffix") : message);
        }

        // The HTML table in the CF_HTML envelope of the clipboard (byte offsets of the start and end of the HTML and of the
        // fragment). The offsets count UTF-8 bytes, which is what .NET writes, not characters - umlauts are two.
        private static string BuildCfHtmlTable(IReadOnlyList<string> headerCells, IReadOnlyList<List<string>> rows)
        {
            var table = BuildHtmlTable(headerCells, rows);

            const string HeaderTemplate =
                "Version:0.9\r\n" +
                "StartHTML:{0:0000000000}\r\n" +
                "EndHTML:{1:0000000000}\r\n" +
                "StartFragment:{2:0000000000}\r\n" +
                "EndFragment:{3:0000000000}\r\n";
            const string HtmlPrefix = "<html><head><meta charset=\"utf-8\"></head><body><!--StartFragment-->";
            const string HtmlSuffix = "<!--EndFragment--></body></html>";

            // (The offsets have a fixed width, so the header is as long with zeros as with the real offsets.)
            var headerLength = Encoding.UTF8.GetByteCount(string.Format(HeaderTemplate, 0, 0, 0, 0));
            var startHtml = headerLength;
            var startFragment = startHtml + Encoding.UTF8.GetByteCount(HtmlPrefix);
            var endFragment = startFragment + Encoding.UTF8.GetByteCount(table);
            var endHtml = endFragment + Encoding.UTF8.GetByteCount(HtmlSuffix);

            return string.Format(HeaderTemplate, startHtml, endHtml, startFragment, endFragment) + HtmlPrefix + table + HtmlSuffix;
        }

        private static string BuildHtmlTable(IReadOnlyList<string> headerCells, IReadOnlyList<List<string>> rows)
        {
            var builder = new StringBuilder();
            builder.Append("<table style=\"border-collapse:collapse;font-family:Segoe UI,sans-serif;font-size:9pt;\">");

            if (headerCells != null)
            {
                builder.Append("<tr>");
                foreach (var cell in headerCells)
                {
                    builder.Append("<th style=\"border:1px solid #999;padding:4px 8px;background:#eee;text-align:left;\">");
                    builder.Append(WebUtility.HtmlEncode(cell));
                    builder.Append("</th>");
                }

                builder.Append("</tr>");
            }

            foreach (var row in rows)
            {
                builder.Append("<tr>");
                foreach (var cell in row)
                {
                    builder.Append("<td style=\"border:1px solid #999;padding:4px 8px;\">");
                    builder.Append(WebUtility.HtmlEncode(cell));
                    builder.Append("</td>");
                }

                builder.Append("</tr>");
            }

            builder.Append("</table>");
            return builder.ToString();
        }

        // (The tooltip of the cell texts shows the confirmation too.)
        private void ShowCopyToast(string message)
        {
            CopyConfirmationDisplay.Show(CopyConfirmation, message, this, _cellToolTip, ToastColors);
        }
    }
}

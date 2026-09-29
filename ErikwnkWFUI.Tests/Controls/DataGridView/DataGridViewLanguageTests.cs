using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;
using WfuiReadOnlyDataGridView = ErikwnkWFUI.Controls.ReadOnlyDataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// Every DataGridView/ReadOnlyDataGridView-specific string is protected
/// against a language switch two separate ways, both checked here:
///
/// - Each key actually has a real translation in every language (via the
///   shared LanguageTestHelper, same as any other control would use it) -
///   catches a key that exists in English but was never added to German
///   (or vice versa), where UIStrings.Get would otherwise silently fall
///   back to showing the raw key itself.
/// - This control's own OnUIStringsLanguageChanged handlers actually
///   refresh what's on screen live, not just at construction - a key can
///   be perfectly translated and still show stale text if nothing re-reads
///   it after Language changes.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class DataGridViewLanguageTests
{
    [Theory]
    [InlineData("DataGridView.WithHeaderSuffix")]
    [InlineData("DataGridView.DeleteRow")]
    [InlineData("DataGridView.DeleteRowHeader")]
    [InlineData("DataGridView.AddRow")]
    [InlineData("DataGridView.EnumerationHeader")]
    [InlineData("DataGridView.RowNumber")]
    [InlineData("DataGridView.CellCopied")]
    [InlineData("DataGridView.CellsCopied")]
    [InlineData("DataGridView.CellCut")]
    [InlineData("DataGridView.CellsCut")]
    [InlineData("DataGridView.CellCleared")]
    [InlineData("DataGridView.CellsCleared")]
    [InlineData("DataGridView.RowPasted")]
    [InlineData("DataGridView.RowsPasted")]
    [InlineData("DataGridView.CellsPasted")]
    [InlineData("DataGridView.RowDeleted")]
    [InlineData("DataGridView.RowsDeleted")]
    [InlineData("DataGridView.RowInserted")]
    [InlineData("DataGridView.RowCut")]
    [InlineData("DataGridView.RowsCut")]
    [InlineData("DataGridView.ContextMenuCut")]
    [InlineData("DataGridView.ContextMenuCutRows")]
    [InlineData("DataGridView.ContextMenuCopySelection")]
    [InlineData("DataGridView.ContextMenuCopySelectionWithHeader")]
    [InlineData("DataGridView.ContextMenuCopyAll")]
    [InlineData("DataGridView.ContextMenuCopyAllWithHeader")]
    [InlineData("DataGridView.ContextMenuSelectAll")]
    [InlineData("DataGridView.ContextMenuPaste")]
    [InlineData("DataGridView.ContextMenuClear")]
    [InlineData("DataGridView.ContextMenuDeleteRows")]
    [InlineData("DataGridView.ContextMenuInsertRowAbove")]
    [InlineData("DataGridView.ContextMenuInsertRowBelow")]
    public void Key_IsTranslatedForEveryLanguage(string key)
    {
        LanguageTestHelper.AssertTranslatedForEveryLanguage(key);
    }

    // The copy entries are meant to look identical in ListView and both
    // DataGridViews (same flat shape, same wording) - the two controls
    // keep their own string keys so neither depends on the other's
    // namespace, which is exactly why nothing but this test would notice
    // one of them being reworded on its own.
    [Theory]
    [InlineData("ListView.CopySelection", "DataGridView.ContextMenuCopySelection")]
    [InlineData("ListView.CopyAll", "DataGridView.ContextMenuCopyAll")]
    [InlineData("ListView.CopySelectionWithHeader", "DataGridView.ContextMenuCopySelectionWithHeader")]
    [InlineData("ListView.CopyAllWithHeader", "DataGridView.ContextMenuCopyAllWithHeader")]
    [InlineData("ListView.SelectAll", "DataGridView.ContextMenuSelectAll")]
    [InlineData("ListView.WithHeaderSuffix", "DataGridView.WithHeaderSuffix")]
    public void CopyMenuTexts_MatchListViewsInEveryLanguage(string listViewKey, string dataGridViewKey)
    {
        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                Assert.Equal(UIStrings.Get(listViewKey), UIStrings.Get(dataGridViewKey));
            });
        }
    }

    [Fact]
    public void DeleteRowColumn_HeaderTextAndTooltip_UpdateLiveWhenLanguageChanges()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        grid.ShowDeleteRowColumn = true;
        string englishHeader = grid.Columns["__deleteRow"]!.HeaderText;
        string englishTooltip = grid.Columns["__deleteRow"]!.HeaderCell.ToolTipText;

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            Assert.NotEqual(englishHeader, grid.Columns["__deleteRow"]!.HeaderText);
            Assert.NotEqual(englishTooltip, grid.Columns["__deleteRow"]!.HeaderCell.ToolTipText);
        });
    }

    [Fact]
    public void ContextMenuItems_Text_UpdateLiveWhenLanguageChanges()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        AssertEveryMenuTextChangesWithLanguage((WfuiContextMenuStrip)grid.ContextMenuStrip!);
    }

    [Fact]
    public void ReadOnlyContextMenuItems_Text_UpdateLiveWhenLanguageChanges()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        AssertEveryMenuTextChangesWithLanguage((WfuiContextMenuStrip)grid.ContextMenuStrip!);
    }

    // Recursive so a future submenu's children are covered as well, same
    // shape ListViewLanguageTests walks.
    private static void AssertEveryMenuTextChangesWithLanguage(WfuiContextMenuStrip menu)
    {
        List<(ToolStripItem Item, string EnglishText)> textItems = CollectTextItems(menu.Items);

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            foreach ((ToolStripItem item, string englishText) in textItems)
            {
                Assert.NotEqual(englishText, item.Text);
            }
        });
    }

    private static List<(ToolStripItem, string)> CollectTextItems(ToolStripItemCollection items)
    {
        List<(ToolStripItem, string)> result = new List<(ToolStripItem, string)>();

        foreach (ToolStripItem item in items)
        {
            // Separators have no text either way - nothing to compare.
            if (!string.IsNullOrEmpty(item.Text))
            {
                result.Add((item, item.Text));
            }

            if (item is ToolStripDropDownItem dropDown)
            {
                result.AddRange(CollectTextItems(dropDown.DropDownItems));
            }
        }

        return result;
    }

    [Fact]
    public void EnumerationColumn_HeaderTextAndTooltip_UpdateLiveWhenLanguageChanges()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.ShowEnumeration = true;
        string englishTooltip = grid.Columns["__enumeration"]!.HeaderCell.ToolTipText;

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            // The header text itself ("#") is deliberately the same symbol
            // in both languages (see UIStrings) - only the tooltip carries
            // an actual translated sentence.
            Assert.NotEqual(englishTooltip, grid.Columns["__enumeration"]!.HeaderCell.ToolTipText);
        });
    }
}

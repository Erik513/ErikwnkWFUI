using System.ComponentModel;
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
    [InlineData("DataGridView.ContextMenuCut")]
    [InlineData("DataGridView.ContextMenuCopy")]
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
        WfuiContextMenuStrip menu = (WfuiContextMenuStrip)grid.ContextMenuStrip!;
        string?[] englishTexts = new string?[menu.Items.Count];
        for (int i = 0; i < menu.Items.Count; i++)
        {
            englishTexts[i] = menu.Items[i].Text;
        }

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            for (int i = 0; i < menu.Items.Count; i++)
            {
                // Separators have no text either way - nothing to compare.
                if (string.IsNullOrEmpty(englishTexts[i]))
                {
                    continue;
                }

                Assert.NotEqual(englishTexts[i], menu.Items[i].Text);
            }
        });
    }

    [Fact]
    public void ReadOnlyContextMenuItems_Text_UpdateLiveWhenLanguageChanges()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        WfuiContextMenuStrip menu = (WfuiContextMenuStrip)grid.ContextMenuStrip!;
        string?[] englishTexts = new string?[menu.Items.Count];
        for (int i = 0; i < menu.Items.Count; i++)
        {
            englishTexts[i] = menu.Items[i].Text;
        }

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            for (int i = 0; i < menu.Items.Count; i++)
            {
                Assert.NotEqual(englishTexts[i], menu.Items[i].Text);
            }
        });
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

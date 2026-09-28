using System.Collections.Generic;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// Same two checks as DataGridViewLanguageTests, using the same shared
/// LanguageTestHelper - see its own remarks on why both matter (a key can
/// be translated but still shown stale if nothing re-reads it after
/// Language changes).
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class ListViewLanguageTests
{
    [Theory]
    [InlineData("ListView.CopySelection")]
    [InlineData("ListView.CopyAll")]
    [InlineData("ListView.AsTable")]
    [InlineData("ListView.RowCopied")]
    [InlineData("ListView.RowsCopied")]
    [InlineData("ListView.WithHeaderSuffix")]
    public void Key_IsTranslatedForEveryLanguage(string key)
    {
        LanguageTestHelper.AssertTranslatedForEveryLanguage(key);
    }

    [Fact]
    public void ContextMenuItems_Text_UpdateLiveWhenLanguageChanges()
    {
        using WfuiListView listView = new WfuiListView();
        WfuiContextMenuStrip menu = (WfuiContextMenuStrip)listView.ContextMenuStrip!;

        // BuildContextMenu nests the real actions ("Copy selection"/"As
        // table") one level down, inside each top-level item's own
        // DropDownItems - collected recursively so this doesn't need to
        // know that shape by hand.
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
}

using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiTabControl = ErikwnkWFUI.Controls.ReadOnlyTabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// The themed tab control: the layouts of the standard control (multiline, fill to right, the strip and the page area).
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class TabControlLayoutTests : TabControlTestBase
{
    // ---- layouts the standard control offers ----

    [Fact]
    public void Multiline_StacksTheTabRows_AboveThePageArea()
    {
        using WfuiTabControl tabs = CreateTabs(pageCount: 12);
        tabs.Width = 200;
        tabs.Multiline = true;

        Assert.True(tabs.RowCount > 1);

        int lowestTabEdge = 0;

        for (int i = 0; i < tabs.TabCount; i++)
        {
            lowestTabEdge = System.Math.Max(lowestTabEdge, tabs.GetTabRect(i).Bottom);
        }

        Rectangle area = tabs.InvokePrivate<Rectangle>("GetPageAreaBounds");
        Assert.True(area.Top <= lowestTabEdge);
        Assert.True(tabs.DisplayRectangle.Top >= lowestTabEdge);
    }

    [Fact]
    public void FillToRight_StretchesEveryTabRowAcrossTheControl_AndStillRenders()
    {
        using WfuiTabControl tabs = CreateTabs(pageCount: 12);
        tabs.Width = 200;
        tabs.Multiline = true;
        tabs.SizeMode = TabSizeMode.FillToRight;

        using Bitmap bitmap = Render(tabs);

        System.Collections.Generic.Dictionary<int, int> rightEdgeByRow = new System.Collections.Generic.Dictionary<int, int>();

        for (int i = 0; i < tabs.TabCount; i++)
        {
            Rectangle rect = tabs.GetTabRect(i);
            rightEdgeByRow[rect.Top] = System.Math.Max(rightEdgeByRow.ContainsKey(rect.Top) ? rightEdgeByRow[rect.Top] : 0, rect.Right);
        }

        Assert.All(rightEdgeByRow.Values, right => Assert.True(right >= tabs.Width - 6));
    }

    [Fact]
    public void RightToLeftLayout_StillCreatesAndRenders()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.RightToLeft = RightToLeft.Yes;
        tabs.RightToLeftLayout = true;

        using Bitmap bitmap = Render(tabs);

        Assert.Equal(3, tabs.TabCount);
    }

    [Fact]
    public void ToolTips_KeepTheStandardDefault_AndThePagesToolTipText()
    {
        using WfuiTabControl tabs = CreateTabs();

        tabs.TabPages[0].ToolTipText = "Hint";

        Assert.True(tabs.ShowToolTips == false);
        Assert.Equal("Hint", tabs.TabPages[0].ToolTipText);
    }

    // ---- the strip and the page area ----

    [Theory]
    [InlineData(TabAlignment.Top)]
    [InlineData(TabAlignment.Bottom)]
    [InlineData(TabAlignment.Left)]
    [InlineData(TabAlignment.Right)]
    public void TheLayout_HoldsTogether_InEveryAlignment(TabAlignment alignment)
    {
        using WfuiTabControl tabs = CreateTabs(alignment);
        Rectangle strip = tabs.InvokePrivate<Rectangle>("GetTabStripBounds");
        Rectangle area = tabs.InvokePrivate<Rectangle>("GetPageAreaBounds");

        // The pages stay inside the control, and the page area wraps them.
        Assert.True(tabs.ClientRectangle.Contains(tabs.DisplayRectangle));
        Assert.True(area.Contains(tabs.DisplayRectangle));
        Assert.True(tabs.ClientRectangle.Contains(area));

        // The strip holds every tab and keeps off the page contents.
        for (int i = 0; i < tabs.TabCount; i++)
        {
            Assert.True(strip.IntersectsWith(tabs.GetTabRect(i)));
        }

        Assert.False(strip.IntersectsWith(Rectangle.Inflate(tabs.DisplayRectangle, -1, -1)));
    }
}

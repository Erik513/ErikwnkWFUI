using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// The themed tab control looks like the standard one: geometry and defaults.
/// </summary>
[Trait("Focus", "true")]
[Collection(FocusTestCollection.Name)]
public class TabControlParityLookTests : TabControlParityTestBase
{
    // ---- geometry ----

    [Theory]
    [InlineData(Kind.ReadOnly, TabAlignment.Top, false)]
    [InlineData(Kind.ReadOnly, TabAlignment.Bottom, false)]
    [InlineData(Kind.ReadOnly, TabAlignment.Left, false)]
    [InlineData(Kind.ReadOnly, TabAlignment.Right, false)]
    [InlineData(Kind.ReadOnly, TabAlignment.Top, true)]
    [InlineData(Kind.ReadOnly, TabAlignment.Left, true)]
    public void TheStripSitsOnTheSameSideOfThePage(Kind kind, TabAlignment alignment, bool multiline)
    {
        AssertSame(
            kind,
            (tabs, log) => log.Add(Shape(tabs)),
            tabs =>
            {
                tabs.Alignment = alignment;
                tabs.Multiline = multiline;
            },
            new[] { "Overview", "Details", "A much longer tab name", "Four", "Five", "Six" });
    }

    [Theory]
    [InlineData(Kind.ReadOnly, TabSizeMode.Normal)]
    [InlineData(Kind.ReadOnly, TabSizeMode.FillToRight)]
    [InlineData(Kind.ReadOnly, TabSizeMode.Fixed)]
    public void TheSizeModes_KeepTheShapeOfTheLayout(Kind kind, TabSizeMode mode)
    {
        AssertSame(
            kind,
            (tabs, log) => log.Add(Shape(tabs)),
            tabs =>
            {
                tabs.Multiline = true;
                tabs.SizeMode = mode;
            },
            new[] { "Overview", "Details", "A longer one", "Four", "Five", "Six" });
    }

    // FillToRight is about the rows reaching the right edge.
    [Theory]
    [InlineData(Kind.ReadOnly)]
    public void FillToRight_StretchesEveryRowToTheRightEdge_LikeTheStandardControl(Kind kind)
    {
        AssertSame(
            kind,
            (tabs, log) =>
            {
                foreach (IGrouping<int, int> row in Enumerable.Range(0, tabs.TabCount).GroupBy(i => tabs.GetTabRect(i).Top))
                {
                    int right = row.Max(i => tabs.GetTabRect(i).Right);
                    log.Add($"row reaches the edge={right >= tabs.Width - 6}");
                }
            },
            tabs =>
            {
                tabs.Multiline = true;
                tabs.SizeMode = TabSizeMode.FillToRight;
            },
            new[] { "Overview", "Details", "A longer one", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten" });
    }

    // Sizes that are given are kept exactly - these must be identical.
    [Theory]
    [InlineData(Kind.ReadOnly)]
    public void ExplicitItemSizeAndPadding_ProduceTheExactSameTabs(Kind kind)
    {
        AssertSame(
            kind,
            (tabs, log) =>
            {
                log.Add($"rows={tabs.RowCount} display={tabs.DisplayRectangle}");
                log.Add(string.Join(";", Enumerable.Range(0, tabs.TabCount).Select(i => tabs.GetTabRect(i).ToString())));
            },
            tabs =>
            {
                tabs.SizeMode = TabSizeMode.Fixed;
                tabs.ItemSize = new Size(90, 28);
                tabs.Padding = new Point(12, 6);
            });
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    public void ChangingATabsText_ResizesItInTheSameDirection(Kind kind)
    {
        AssertSame(kind, (tabs, log) =>
        {
            int before = tabs.GetTabRect(1).Width;
            tabs.TabPages[1].Text = "A much longer name now";
            int longer = tabs.GetTabRect(1).Width;
            tabs.TabPages[1].Text = "x";
            int shorter = tabs.GetTabRect(1).Width;

            log.Add("longer text is wider=" + (longer > before));
            log.Add("shorter text is narrower=" + (shorter < longer));
            log.Add("neighbour moved along=" + (tabs.GetTabRect(2).Left > tabs.GetTabRect(1).Left));
        });
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    public void TabImages_MakeATabWiderInBoth(Kind kind)
    {
        using ImageList images = new ImageList { ImageSize = new Size(16, 16) };
        using Bitmap icon = new Bitmap(16, 16);
        images.Images.Add(icon);

        AssertSame(
            kind,
            (tabs, log) =>
            {
                log.Add("tab with an image is wider=" + (tabs.GetTabRect(0).Width > tabs.GetTabRect(1).Width));
            },
            tabs =>
            {
                tabs.ImageList = images;
                tabs.TabPages[0].Text = "Two";
                tabs.TabPages[0].ImageIndex = 0;
            });
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    public void ResizingTheControl_WrapsAndUnwrapsTheRowsTheSame(Kind kind)
    {
        AssertSame(
            kind,
            (tabs, log) =>
            {
                Form host = (Form)tabs.FindForm()!;
                log.Add("wide: " + (tabs.RowCount > 1 ? "wrapped" : "one row"));
                host.ClientSize = new Size(180, 120);
                Pump();
                log.Add("narrow: " + (tabs.RowCount > 1 ? "wrapped" : "one row"));
                log.Add(Shape(tabs));
                host.ClientSize = new Size(700, 300);
                Pump();
                log.Add("wide again: " + (tabs.RowCount > 1 ? "wrapped" : "one row"));
            },
            tabs => tabs.Multiline = true,
            new[] { "Overview", "Details", "Notes", "Reports", "Archive", "Settings" });
    }

    // The one deliberate difference, written down so it cannot grow
    // unnoticed: while the native control sizes a tab itself, the themed
    // controls' tabs come out a little larger (about 6 px wider and 1 px
    // taller for a short name on .NET 8 without visual styles, about 12 and 3
    // with them on .NET Framework) than the standard control's. The tab
    // heights and the page area start follow from it.
    [Fact]
    public void TheAutomaticTabSize_IsALittleLargerThanTheStandardControls()
    {
        Size standard = default;
        Size themed = default;

        Run(null, (tabs, log) =>
        {
            standard = tabs.GetTabRect(0).Size;
            log.Add("x");
        });
        Run(Kind.ReadOnly, (tabs, log) =>
        {
            themed = tabs.GetTabRect(0).Size;
            log.Add("x");
        });

        Assert.True(themed.Width >= standard.Width);
        Assert.True(themed.Height >= standard.Height);
        Assert.True(themed.Width - standard.Width <= 14);
        Assert.True(themed.Height - standard.Height <= 4);
    }

    // ---- defaults ----

    [Theory]
    [InlineData(Kind.ReadOnly)]
    public void TheDefaultPropertiesAreTheStandardOnes(Kind kind)
    {
        using System.Windows.Forms.TabControl standard = Create(null);
        using System.Windows.Forms.TabControl themed = Create(kind);

        Assert.Equal(Defaults(standard), Defaults(themed));
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    public void TheDefaultsOfANewPage_AreTheStandardOnes(Kind kind)
    {
        using System.Windows.Forms.TabControl standard = Create(null);
        using System.Windows.Forms.TabControl themed = Create(kind);
        standard.TabPages.Add("a");
        themed.TabPages.Add("a");

        TabPage expected = standard.TabPages[0];
        TabPage actual = themed.TabPages[0];

        Assert.Equal(
            $"{expected.Text} {expected.Enabled} {expected.Visible} {expected.ImageIndex} {expected.ToolTipText} {expected.Dock} {expected.BorderStyle}",
            $"{actual.Text} {actual.Enabled} {actual.Visible} {actual.ImageIndex} {actual.ToolTipText} {actual.Dock} {actual.BorderStyle}");
        Assert.Equal(standard.SelectedIndex, themed.SelectedIndex);
    }

    // The one default that differs on purpose: a hover highlight is part of
    // the themed look, so HotTrack is on where the standard control has it off.
    [Theory]
    [InlineData(Kind.ReadOnly)]
    [InlineData(Kind.Editable)]
    public void HotTrack_IsOnByDefault_UnlikeTheStandardControl(Kind kind)
    {
        using System.Windows.Forms.TabControl standard = Create(null);
        using System.Windows.Forms.TabControl themed = Create(kind);

        Assert.False(standard.HotTrack);
        Assert.True(themed.HotTrack);
    }
}

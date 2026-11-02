using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.PropertyTableHost;
using static ErikwnkWFUI.Tests.Infrastructure.ColorContrastTestSupport;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using PropertyTable = ErikwnkWFUI.Controls.PropertyTable;

namespace ErikwnkWFUI.Tests.Controls.PropertyTables;

/// <summary>What the factories of the property table make: neutral, or with a line in the color of a set (or the accent) at the left edge of each section header.</summary>
[Collection(AccentColorTestCollection.Name)]
public class PropertyTableFactoryTests : IDisposable
{
    private readonly PropertyTableHost _host = new PropertyTableHost(new Size(400, 120));

    public void Dispose()
    {
        _host.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private PropertyTable Show(PropertyTable table)
    {
        table.AddSection("Section");
        table.AddRow("Label", new Button { Text = "x" });

        return _host.Show(table);
    }

    // The color of the header at its left edge (where the line is) and a little inside (where it never is).
    private static (Color Edge, Color Inside, Color Back) Header(PropertyTable table)
    {
        using Bitmap picture = Draw(table);
        Label header = SectionLabels(table)[0];
        int y = header.Top + header.Height / 2;

        return (picture.GetPixel(1, y), picture.GetPixel(Dpi.Scale(header, 3) + 4, header.Top + 3), header.BackColor);
    }

    private static List<Label> SectionLabels(PropertyTable table)
    {
        return table.GetPrivateField<List<Label>>("_sectionLabels")!;
    }

    [Fact]
    public void ANeutralTable_HasNoLine_AndThePrimaryOneAndAColoredOneHaveOne()
    {
        PropertyTable neutral = Show(UIStyles.PropertyTables.CreateStandard());
        PropertyTable yellow = Show(UIStyles.PropertyTables.CreateStandard(UIStyles.Colors.Sets.Yellow));
        PropertyTable primary = Show(UIStyles.PropertyTables.CreatePrimary());

        (Color edge, _, Color back) = Header(neutral);
        Assert.Equal(back.ToArgb(), edge.ToArgb());
        Assert.Null(neutral.Colors);

        (Color yellowEdge, Color yellowInside, Color yellowBack) = Header(yellow);
        Assert.NotEqual(yellowBack.ToArgb(), yellowEdge.ToArgb());
        Assert.Equal(yellowBack.ToArgb(), yellowInside.ToArgb());
        Assert.Equal(UIColors.EnsureContrast(UIStyles.Colors.Sets.Yellow.Hover, yellowBack, 3.0).ToArgb(), yellowEdge.ToArgb());

        (Color primaryEdge, _, Color primaryBack) = Header(primary);
        Assert.Equal(UIColors.EnsureContrast(UIColors.PrimarySet.Hover, primaryBack, 3.0).ToArgb(), primaryEdge.ToArgb());
    }

    [Fact]
    public void ASingleColor_IsTakenAsTheMainColorOfASet()
    {
        PropertyTable purple = Show(UIStyles.PropertyTables.CreateStandard(Color.MediumPurple));

        (Color edge, _, Color back) = Header(purple);

        Assert.NotEqual(back.ToArgb(), edge.ToArgb());
        Assert.True(edge.B > edge.G + 20, "the line is not purple");
    }

    [Fact]
    public void ThePrimaryLine_FollowsAChangeOfTheAccent_WhileTheTableIsShown()
    {
        PropertyTable primary = Show(UIStyles.PropertyTables.CreatePrimary());
        Color before = Header(primary).Edge;

        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Color after = Header(primary).Edge;
        Assert.NotEqual(before.ToArgb(), after.ToArgb());
        Assert.True(after.R > after.B + 40, "the line did not take the new accent");
    }

    [Fact]
    public void ColorsSetLater_ColorTheSectionsThatAreThereAndThoseThatCome_AndNullTakesTheLineAway()
    {
        PropertyTable table = Show(UIStyles.PropertyTables.CreateStandard());
        table.AddSection("Second");

        table.Colors = UIStyles.Colors.Sets.Green;
        Application.DoEvents();
        (Color edge, _, Color back) = Header(table);
        Assert.NotEqual(back.ToArgb(), edge.ToArgb());
        Assert.Equal(2, SectionLabels(table).Count);

        table.Colors = null;
        Application.DoEvents();
        Assert.Equal(back.ToArgb(), Header(table).Edge.ToArgb());
    }

    [Fact]
    public void ClearingTheRows_ForgetsTheSections_AndANewSectionHasTheLineAgain()
    {
        PropertyTable table = Show(UIStyles.PropertyTables.CreatePrimary());
        Assert.Single(SectionLabels(table));

        table.ClearRows();
        Assert.Empty(SectionLabels(table));

        table.AddSection("Again");
        table.PerformLayout();
        Application.DoEvents();

        Assert.Single(SectionLabels(table));
        (Color edge, _, Color back) = Header(table);
        Assert.NotEqual(back.ToArgb(), edge.ToArgb());
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheLine_IsSeenOnTheHeader_ForEveryColorSetAndPrimary_InBothThemes(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach ((string name, UIColorSet? set) in AllSets(includeDark: true))
            {
                if (set == null)
                    continue;

                PropertyTable table = Show(UIStyles.PropertyTables.CreateStandard(set));
                (Color edge, _, Color back) = Header(table);
                Check(shortfalls, "line, " + name, edge, back, 2.9);
            }

            PropertyTable primary = Show(UIStyles.PropertyTables.CreatePrimary());
            (Color primaryEdge, _, Color primaryBack) = Header(primary);
            Check(shortfalls, "line, primary", primaryEdge, primaryBack, 2.9);
        });
    }

    [Fact]
    public void ATableWithoutRowsOrSections_StillHasAColorSet_AndPaintsNothingWrong()
    {
        using PropertyTable table = UIStyles.PropertyTables.CreatePrimary();

        Assert.NotNull(table.Colors);
        Assert.Null(Record.Exception(() => table.Colors = UIStyles.Colors.Sets.Red));
        Assert.Null(Record.Exception(() => table.ClearRows()));
    }

    [Fact]
    public void ClearingTheRows_KeepsTheEditorsAlive_AndDisposesWhatTheTableBuiltAroundThem()
    {
        PropertyTable table = Show(UIStyles.PropertyTables.CreateStandard());
        Button editor = new Button { Text = "kept" };
        table.AddRow("Another", editor);
        Control wrapper = editor.Parent!;
        Label rowLabel = AllLabels(table).First(l => l.Text == "Another");

        table.ClearRows();

        Assert.False(editor.IsDisposed);
        Assert.Null(editor.Parent);
        Assert.True(wrapper.IsDisposed);
        Assert.True(rowLabel.IsDisposed);
        editor.Dispose();
    }
}

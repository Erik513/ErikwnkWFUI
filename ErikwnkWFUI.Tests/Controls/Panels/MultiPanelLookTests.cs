using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ErikwnkWFUI;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using Xunit;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Tests.Controls.Panels;

/// <summary>
/// The multi panel: header, title, colors and line.
/// </summary>
[Collection(ErikwnkWFUI.Tests.Infrastructure.AccentColorTestCollection.Name)]
public class MultiPanelLookTests : MultiPanelTestBase
{
    [Fact]
    public void ThePageCountIsOffByDefault_AndShowsTheNumberOfTheShownPage()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 12);
        Assert.False(panel.ShowPageCount);

        (int firstOff, _) = InkColumns(panel);
        panel.ShowPageCount = true;
        Application.DoEvents();
        (int firstOn, int lastOn) = InkColumns(panel);

        // The count stands at the left of the header and the title behind it.
        Assert.True(firstOff >= 0);
        Assert.True(firstOn >= 0 && firstOn <= firstOff);
        Assert.True(lastOn >= firstOff);

        using Bitmap before = Render(panel);
        panel.SelectedIndex = 5;
        using Bitmap after = Render(panel);
        Assert.NotEqual(Convert.ToBase64String(ToBytes(before)), Convert.ToBase64String(ToBytes(after)));
    }

    [Fact]
    public void TheTitle_StandsLeftByDefault_AndMovesToTheCenterAndTheRight()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 1);
        panel.Pages[0].Title = "Title";
        Application.DoEvents();

        Assert.Equal(HorizontalAlignment.Left, panel.TitleAlignment);
        (int left, _) = InkColumns(panel);

        panel.TitleAlignment = HorizontalAlignment.Center;
        Application.DoEvents();
        (int center, _) = InkColumns(panel);

        panel.TitleAlignment = HorizontalAlignment.Right;
        Application.DoEvents();
        (int right, int rightEnd) = InkColumns(panel);

        Assert.True(left < center, $"{left} < {center}");
        Assert.True(center < right, $"{center} < {right}");
        Assert.True(rightEnd < Arrows(panel)[0].Left);
    }

    [Fact]
    public void ATitleThatIsChanged_IsPaintedAgain()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 0);
        panel.AddPage();
        Application.DoEvents();
        (int none, _) = InkColumns(panel);
        Assert.Equal(-1, none);

        panel.Pages[0].Title = "Now there is a title";
        Application.DoEvents();

        Assert.True(InkColumns(panel).First >= 0);
    }

    [Fact]
    public void TheFlatOnesAreSquareWithoutALine_TheCardsAreRoundedWithOne()
    {
        using MultiPanel flat = UIStyles.MultiPanels.CreateStandard();
        using MultiPanel card = UIStyles.MultiPanels.CreateCardStandard();

        Assert.Equal(0, flat.CornerRadius);
        Assert.Equal(BorderStyle.None, flat.BorderStyle);
        Assert.Equal(DockStyle.Fill, flat.Dock);
        Assert.True(card.CornerRadius > 0);
        Assert.Equal(BorderStyle.FixedSingle, card.BorderStyle);
        Assert.Equal(DockStyle.None, card.Dock);
    }

    [Fact]
    public void TheColors_FollowTheFactoryAndTheColorSet()
    {
        UIColorSet green = UIStyles.Colors.Sets.Green;

        using MultiPanel standard = UIStyles.MultiPanels.CreateStandard();
        using MultiPanel inGreen = UIStyles.MultiPanels.CreateStandard(green);
        using MultiPanel primary = UIStyles.MultiPanels.CreatePrimary();
        using MultiPanel card = UIStyles.MultiPanels.CreateCardStandard();
        using MultiPanel cardInGreen = UIStyles.MultiPanels.CreateCardStandard(green);
        using MultiPanel cardPrimary = UIStyles.MultiPanels.CreateCardPrimary();

        Assert.Equal(UIColors.BackgroundMedium, standard.BackColor);
        Assert.Equal(green.Basic, inGreen.BackColor);
        Assert.Equal(UIColors.PrimaryDarkDark, primary.BackColor);
        Assert.Equal(UIColors.BackgroundMediumElevated, card.BackColor);
        Assert.Equal(green.Basic, cardInGreen.BackColor);
        Assert.Equal(green.Hover, cardInGreen.BorderColor);
        Assert.Equal(UIColors.PrimaryDarkDark, cardPrimary.BackColor);
        Assert.Equal(UIColors.PrimaryDark, cardPrimary.BorderColor);
    }

    [Fact]
    public void TheArrowsOfThePrimaryOnes_FollowTheAccent_TheOthersTheirColorSet()
    {
        Color accent = UIColors.Primary;

        try
        {
            MultiPanel primary = Shown(UIStyles.MultiPanels.CreatePrimary(), 2);
            MultiPanel cardPrimary = Shown(UIStyles.MultiPanels.CreateCardPrimary(), 2);
            MultiPanel green = Shown(UIStyles.MultiPanels.CreateStandard(UIStyles.Colors.Sets.Green), 2);
            MultiPanel standard = Shown(UIStyles.MultiPanels.CreateStandard(), 2);

            UIColors.SetAccent(Color.OrangeRed);
            Application.DoEvents();

            Assert.Equal(UIColors.PrimarySet.Basic, Arrows(primary)[1].BackColor);
            Assert.Equal(UIColors.PrimarySet.Basic, Arrows(cardPrimary)[1].BackColor);
            Assert.Equal(UIStyles.Colors.Sets.Green.Basic, Arrows(green)[1].BackColor);
            Assert.Equal(UIColors.BackgroundMedium, Arrows(standard)[1].BackColor);
        }
        finally
        {
            UIColors.SetAccent(accent);
        }
    }

    [Fact]
    public void ALargeFont_MakesTheHeaderHigher_SoThatTheTitleFits_AndThePagesStayBelowIt()
    {
        MultiPanel normal = Shown(UIStyles.MultiPanels.CreateStandard(), 1);
        int normalHeader = normal.DisplayRectangle.Top - normal.Padding.Top;

        MultiPanel large = Shown(UIStyles.MultiPanels.CreateStandard(), 1);
        large.Font = new Font("Segoe UI", 28f);
        Application.DoEvents();
        int largeHeader = large.DisplayRectangle.Top - large.Padding.Top;

        Assert.True(largeHeader >= large.Font.Height, $"{largeHeader} >= {large.Font.Height}");
        Assert.True(largeHeader > normalHeader);
        Assert.Equal(large.DisplayRectangle, large.Pages[0].Bounds);

        Button next = Arrows(large)[1];
        Assert.True(next.Bottom <= large.Padding.Top + largeHeader);
    }

    [Theory]
    [InlineData(40)]
    [InlineData(60)]
    [InlineData(90)]
    public void ANarrowMultiPanel_KeepsTheArrowsInside_AndTheTextAwayFromThem(int width)
    {
        MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
        panel.AddPage("A very long title that cannot fit");
        panel.AddPage("B");
        panel.ShowPageCount = true;
        panel.Dock = DockStyle.None;
        panel.Size = new Size(width, 100);
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(200, 120) };
        host.Controls.Add(panel);
        host.Show();
        Application.DoEvents();
        _forms.Add(host);

        Button[] arrows = Arrows(panel);
        Assert.True(arrows[0].Left >= panel.Padding.Left, $"{arrows[0].Left}");

        // Between the text and the arrows nothing is painted (only the background).
        using Bitmap bitmap = Render(panel);
        Color back = bitmap.GetPixel(panel.Width / 2, panel.Height - 12);
        int gap = 6;

        for (int x = Math.Max(0, arrows[0].Left - gap); x < arrows[0].Left; x++)
            for (int y = 8; y < 28; y++)
                Assert.Equal(back.ToArgb(), bitmap.GetPixel(x, y).ToArgb());
    }

    [Theory]
    [InlineData(255, 255, 255)]
    [InlineData(0, 0, 0)]
    public void OnATransparentMultiPanel_TheHeaderTextIsReadableOnTheBackgroundBehindIt(int r, int g, int b)
    {
        MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
        panel.BackColor = Color.Transparent;
        panel.AddPage("Title");
        panel.Dock = DockStyle.None;
        panel.Size = new Size(400, 200);
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(420, 220), BackColor = Color.FromArgb(r, g, b) };
        host.Controls.Add(panel);
        host.Show();
        Application.DoEvents();
        _forms.Add(host);

        if (r == 0)
            Assert.True(Brightest(panel) > 128);
        else
            Assert.True(Darkest(panel) < 128);
    }

    [Fact]
    public void TheHeaderLine_IsOffByDefault_AndSeparatesTheHeaderFromThePageInTheBorderColor()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateCardStandard(), 2);
        Assert.False(panel.ShowHeaderLine);
        int pagesTop = panel.DisplayRectangle.Top;

        using Bitmap without = Render(panel);

        panel.ShowHeaderLine = true;
        Application.DoEvents();

        Assert.True(panel.DisplayRectangle.Top > pagesTop);
        Assert.Equal(panel.DisplayRectangle, panel.Pages[0].Bounds);

        // The line runs between the end of the header and the start of the page.
        int x = panel.Width / 2;
        int found = -1;

        using (Bitmap with = Render(panel))
        {
            for (int y = pagesTop - 4; y < panel.DisplayRectangle.Top; y++)
            {
                if (with.GetPixel(x, y).ToArgb() == Color.FromArgb(255, panel.BorderColor).ToArgb())
                    found = y;

                Assert.NotEqual(Color.FromArgb(255, panel.BorderColor).ToArgb(), without.GetPixel(x, y).ToArgb());
            }
        }

        Assert.True(found >= 0, "no line in the border color");

        panel.ShowHeaderLine = false;
        Assert.Equal(pagesTop, panel.DisplayRectangle.Top);
    }

    [Fact]
    public void TheHeaderLineOfTheFlatOnes_HasTheLineColorOfTheirVariant()
    {
        UIColorSet green = UIStyles.Colors.Sets.Green;

        using MultiPanel standard = UIStyles.MultiPanels.CreateStandard();
        using MultiPanel primary = UIStyles.MultiPanels.CreatePrimary();
        using MultiPanel inGreen = UIStyles.MultiPanels.CreateStandard(green);

        Assert.Equal(UIColors.BorderMedium, standard.BorderColor);
        Assert.Equal(UIColors.PrimaryDark, primary.BorderColor);
        Assert.Equal(green.Hover, inGreen.BorderColor);
    }
}

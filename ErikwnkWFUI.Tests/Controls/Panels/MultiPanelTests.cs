using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ErikwnkWFUI;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using Xunit;

namespace ErikwnkWFUI.Tests.Controls.Panels;

[Collection(ErikwnkWFUI.Tests.Infrastructure.AccentColorTestCollection.Name)]
public class MultiPanelTests : IDisposable
{
    private readonly List<Form> _forms = new();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private MultiPanel Shown(MultiPanel panel, int pages)
    {
        for (int i = 1; i <= pages; i++)
            panel.AddPage("Page " + i);

        panel.Dock = DockStyle.None;
        panel.Size = new Size(400, 200);
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(420, 220) };
        host.Controls.Add(panel);
        host.Show();
        Application.DoEvents();
        _forms.Add(host);

        return panel;
    }

    private static Button[] Arrows(MultiPanel panel)
    {
        return panel.Controls.OfType<Button>().OrderBy(button => button.Left).ToArray();
    }

    private static Bitmap Render(Control control)
    {
        Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));

        return bitmap;
    }

    // The columns of the header (without the arrows) where something other than the background is painted.
    private static (int First, int Last) InkColumns(MultiPanel panel)
    {
        using Bitmap bitmap = Render(panel);
        Color back = bitmap.GetPixel(panel.Width / 2, panel.Height - 12);
        int arrowsLeft = Arrows(panel)[0].Left - 2;
        int first = -1, last = -1;

        for (int x = 12; x < arrowsLeft; x++)
        {
            for (int y = 6; y < 30; y++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() != back.ToArgb())
                {
                    if (first < 0)
                        first = x;

                    last = x;
                    break;
                }
            }
        }

        return (first, last);
    }

    [Fact]
    public void TheFirstPageAdded_IsShown_TheOthersAreHidden()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);

        Assert.Equal(3, panel.PageCount);
        Assert.Equal(0, panel.SelectedIndex);
        Assert.Same(panel.Pages[0], panel.SelectedPage);
        Assert.True(panel.Pages[0].Visible);
        Assert.False(panel.Pages[1].Visible);
        Assert.False(panel.Pages[2].Visible);
    }

    [Fact]
    public void ThePages_FillTheAreaBelowTheHeader_InsideThePadding()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateCardStandard(), 2);

        Assert.Equal(panel.DisplayRectangle, panel.Pages[0].Bounds);
        Assert.True(panel.DisplayRectangle.Top > panel.Padding.Top);
        Assert.Equal(panel.Height - panel.Padding.Bottom, panel.DisplayRectangle.Bottom);
    }

    [Fact]
    public void NextAndPrevious_SwitchThePages_AndStopAtTheEnds_AndRaiseTheEventOnce()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);
        int raised = 0;
        panel.SelectedIndexChanged += (sender, e) => raised++;

        panel.Previous();
        Assert.Equal(0, panel.SelectedIndex);
        Assert.Equal(0, raised);

        panel.Next();
        panel.Next();
        panel.Next();

        Assert.Equal(2, panel.SelectedIndex);
        Assert.Equal(2, raised);
        Assert.True(panel.Pages[2].Visible);
        Assert.False(panel.Pages[0].Visible);

        panel.SelectedIndex = 2;
        Assert.Equal(2, raised);
    }

    [Fact]
    public void TheArrowButtons_SwitchThePages_AndAreDisabledAtTheEnds()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);
        Button[] arrows = Arrows(panel);

        Assert.Equal(2, arrows.Length);
        Assert.False(arrows[0].Enabled);
        Assert.True(arrows[1].Enabled);

        arrows[1].PerformClick();
        Assert.Equal(1, panel.SelectedIndex);
        Assert.True(arrows[0].Enabled);
        Assert.True(arrows[1].Enabled);

        arrows[1].PerformClick();
        Assert.False(arrows[1].Enabled);

        arrows[0].PerformClick();
        Assert.Equal(1, panel.SelectedIndex);
    }

    [Fact]
    public void WithoutPagesOrWithOne_BothArrowsAreDisabled_AndTheIndexIsMinusOneUntilThereIsAPage()
    {
        MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
        using (panel)
        {
            Assert.Equal(-1, panel.SelectedIndex);
            Assert.Null(panel.SelectedPage);
            Assert.All(Arrows(panel), button => Assert.False(button.Enabled));

            panel.AddPage("Only");

            Assert.Equal(0, panel.SelectedIndex);
            Assert.All(Arrows(panel), button => Assert.False(button.Enabled));
        }
    }

    [Fact]
    public void ASelectedIndexOutOfRange_Throws()
    {
        using MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
        panel.AddPage("A");
        panel.AddPage("B");

        Assert.Throws<ArgumentOutOfRangeException>(() => panel.SelectedIndex = 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => panel.SelectedIndex = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => panel.SelectedIndex = -2);
    }

    [Fact]
    public void RemovingPages_KeepsTheShownPage_OrShowsTheOneThatTookItsPlace()
    {
        using MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
        MultiPanelPage a = panel.AddPage("A");
        MultiPanelPage b = panel.AddPage("B");
        MultiPanelPage c = panel.AddPage("C");
        panel.SelectedIndex = 1;
        int raised = 0;
        panel.SelectedIndexChanged += (sender, e) => raised++;

        Assert.True(panel.RemovePage(a));
        Assert.Equal(0, panel.SelectedIndex);
        Assert.Same(b, panel.SelectedPage);
        Assert.Equal(0, raised);

        Assert.True(panel.RemovePage(b));
        Assert.Same(c, panel.SelectedPage);
        Assert.True(c.Visible);
        Assert.Equal(1, raised);

        Assert.True(panel.RemovePage(c));
        Assert.Equal(-1, panel.SelectedIndex);
        Assert.False(panel.RemovePage(c));
        Assert.Equal(2, raised);
        Assert.False(a.IsDisposed);
    }

    [Fact]
    public void APageThatIsDisposedByItsOwner_LeavesTheMultiPanel_AndTheNextOneIsShown()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);
        int raised = 0;
        panel.SelectedIndexChanged += (sender, e) => raised++;
        MultiPanelPage first = panel.Pages[0];
        MultiPanelPage second = panel.Pages[1];

        first.Dispose();

        Assert.Equal(2, panel.PageCount);
        Assert.Same(second, panel.SelectedPage);
        Assert.True(second.Visible);
        Assert.Equal(1, raised);

        // A page that is not shown: nothing else changes.
        panel.Pages[1].Dispose();
        panel.Pages[0].Dispose();
        Assert.Equal(0, panel.PageCount);
        Assert.Equal(-1, panel.SelectedIndex);
        Assert.All(Arrows(panel), button => Assert.False(button.Enabled));
    }

    [Fact]
    public void APageThatIsPutOnAnotherControl_LeavesTheMultiPanel_AndIsShownThere()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);
        using Panel other = new Panel();
        MultiPanelPage shown = panel.Pages[0];
        MultiPanelPage hidden = panel.Pages[2];

        other.Controls.Add(hidden);

        Assert.Equal(2, panel.PageCount);
        Assert.Equal(0, panel.SelectedIndex);
        Assert.True(hidden.Visible);

        other.Controls.Add(shown);

        Assert.Equal(1, panel.PageCount);
        Assert.Equal("Page 2", panel.SelectedPage.Title);
        Assert.True(panel.SelectedPage.Visible);
    }

    [Fact]
    public void DisposingTheMultiPanel_DoesNotRaiseTheEvent_AndDoesNotThrow()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);
        int raised = 0;
        panel.SelectedIndexChanged += (sender, e) => raised++;

        panel.Dispose();

        Assert.Equal(0, raised);
    }

    [Fact]
    public void AControlAddedDirectlyToTheMultiPanel_IsRefused()
    {
        using MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
        using Label label = new Label();

        Assert.Throws<InvalidOperationException>(() => panel.Controls.Add(label));
        Assert.DoesNotContain(label, panel.Controls.Cast<Control>());

        panel.AddPage("A").Controls.Add(label);
        Assert.Same(panel.Pages[0], label.Parent);
    }

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

    private static byte[] ToBytes(Bitmap bitmap)
    {
        using System.IO.MemoryStream stream = new System.IO.MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

        return stream.ToArray();
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
        Assert.False(flat.ShowBorder);
        Assert.Equal(DockStyle.Fill, flat.Dock);
        Assert.True(card.CornerRadius > 0);
        Assert.True(card.ShowBorder);
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
    public void TheHeaderText_IsReadableOnADarkAndOnALightSurface()
    {
        MultiPanel dark = Shown(UIStyles.MultiPanels.CreatePrimary(), 1);
        dark.Pages[0].Title = "Title";
        MultiPanel light = Shown(UIStyles.MultiPanels.CreateStandard(Color.FromArgb(240, 240, 240)), 1);
        light.Pages[0].Title = "Title";
        Application.DoEvents();

        Assert.True(Brightest(dark) > 128);
        Assert.True(Darkest(light) < 128);
    }

    private static int Brightest(MultiPanel panel)
    {
        using Bitmap bitmap = Render(panel);
        int best = 0;

        for (int x = 12; x < 100; x++)
            for (int y = 6; y < 30; y++)
                best = Math.Max(best, bitmap.GetPixel(x, y).R);

        return best;
    }

    private static int Darkest(MultiPanel panel)
    {
        using Bitmap bitmap = Render(panel);
        int best = 255;

        for (int x = 12; x < 100; x++)
            for (int y = 6; y < 30; y++)
                best = Math.Min(best, bitmap.GetPixel(x, y).R);

        return best;
    }
}

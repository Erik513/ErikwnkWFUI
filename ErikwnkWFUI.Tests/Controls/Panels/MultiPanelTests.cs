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

    // PerformClick only works on buttons that can take the focus, which the arrows cannot (they are clicked with the mouse).
    private static void Click(Button button)
    {
        typeof(Button).GetMethod("OnClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(button, new object[] { EventArgs.Empty });
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

        Click(arrows[1]);
        Assert.Equal(1, panel.SelectedIndex);
        Assert.True(arrows[0].Enabled);
        Assert.True(arrows[1].Enabled);

        Click(arrows[1]);
        Assert.False(arrows[1].Enabled);

        Click(arrows[0]);
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

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static (WeakReference Panel, MultiPanelPage Page) MakeAndRemove()
    {
        MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
        MultiPanelPage page = panel.AddPage("A");
        panel.AddPage("B");
        panel.RemovePage(page);
        panel.Dispose();

        return (new WeakReference(panel), page);
    }

    [Fact]
    public void ARemovedPage_DoesNotKeepTheMultiPanelAlive()
    {
        (WeakReference panel, MultiPanelPage page) = MakeAndRemove();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(panel.IsAlive);
        GC.KeepAlive(page);
        page.Dispose();
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

    [Fact]
    public void FocusInTheLeftPage_GoesOnToTheFirstControlOfTheNewPage()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);
        TextBox first = new TextBox();
        TextBox second = new TextBox { TabIndex = 1 };
        TextBox other = new TextBox { TabIndex = 0 };
        panel.Pages[0].Controls.Add(first);
        panel.Pages[1].Controls.Add(second);
        panel.Pages[1].Controls.Add(other);
        Form host = (Form)panel.FindForm();
        first.Focus();
        Assert.Same(first, host.ActiveControl);

        panel.Next();

        Assert.Same(other, host.ActiveControl);

        // The arrows do not take the focus, so the focus is not moved by them either.
        Assert.All(Arrows(panel), arrow => Assert.False(arrow.CanSelect));
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

    private static bool PressKey(MultiPanel panel, Keys keys)
    {
        object[] arguments = { new Message(), keys };

        return (bool)typeof(Control).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(panel, arguments)!;
    }

    [Fact]
    public void CtrlPageDownAndCtrlPageUp_SwitchThePages_AndLeaveTheKeyToTheParentAtTheEnds()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);

        Assert.False(PressKey(panel, Keys.Control | Keys.PageUp));
        Assert.Equal(0, panel.SelectedIndex);

        Assert.True(PressKey(panel, Keys.Control | Keys.PageDown));
        Assert.True(PressKey(panel, Keys.Control | Keys.Tab));
        Assert.Equal(2, panel.SelectedIndex);
        Assert.False(PressKey(panel, Keys.Control | Keys.PageDown));
        Assert.False(PressKey(panel, Keys.Control | Keys.Tab));

        Assert.True(PressKey(panel, Keys.Control | Keys.PageUp));
        Assert.True(PressKey(panel, Keys.Control | Keys.Shift | Keys.Tab));
        Assert.Equal(0, panel.SelectedIndex);

        // Other keys are not taken.
        Assert.False(PressKey(panel, Keys.PageDown));
        Assert.False(PressKey(panel, Keys.Tab));
        Assert.Equal(0, panel.SelectedIndex);
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

    [Fact]
    public void TheArrows_DoNotTakeTheFocus_SoItDoesNotJumpToAPageWhenAnArrowIsDisabled()
    {
        MultiPanel panel = Shown(UIStyles.MultiPanels.CreateStandard(), 3);

        // A click with the mouse focuses a button that can be selected - and when that one is disabled by the click (the next arrow on
        // the last page), the focus jumped on to the first control of the page. So the arrows cannot be selected.
        Button[] arrows = Arrows(panel);
        Assert.All(arrows, arrow => Assert.False(arrow.CanSelect));
        Assert.All(arrows, arrow => Assert.False(arrow.TabStop));

        Click(arrows[1]);
        Click(arrows[1]);

        Assert.Equal(2, panel.SelectedIndex);
        Assert.False(arrows[1].Enabled);
        Assert.NotSame(arrows[0], panel.FindForm().ActiveControl);
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

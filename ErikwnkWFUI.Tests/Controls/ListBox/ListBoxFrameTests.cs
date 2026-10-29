using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>The frame of the list (and of the titled list): seen against the window in both themes, in the color of the look, and the area below the rows has the color of the rows.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ListBoxFrameTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private T Shown<T>(T control) where T : Control
    {
        control.SetBounds(10, 10, 160, 150);

        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            ClientSize = new Size(200, 200),
            BackColor = UIColors.BackgroundMedium
        };
        form.Controls.Add(control);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return control;
    }

    private static WfuiListBox NewList(UIColorSet? colors = null, bool primary = false)
    {
        WfuiListBox list = primary
            ? (WfuiListBox)UIStyles.ListBoxes.CreatePrimary()
            : (WfuiListBox)UIStyles.ListBoxes.CreateStandard(colors: colors);
        list.Items.AddRange(new object[] { "one", "two" });

        return list;
    }

    // The four edges of the picture of a control: the colors of the line around it.
    private static Color[] Edges(Control control)
    {
        using Bitmap picture = TestPictures.Draw(control);
        int w = picture.Width - 1;
        int h = picture.Height - 1;

        return new[] { picture.GetPixel(w / 2, 0), picture.GetPixel(0, h / 2), picture.GetPixel(w / 2, h), picture.GetPixel(w, h / 2) };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheListHasAFrame_InTheNeutralColorOfTheTheme_AndItIsSeenAgainstTheWindow(bool light)
    {
        UIColors.ApplyTheme(light ? UIThemes.Light : UIThemes.Dark);
        WfuiListBox list = Shown(NewList());

        Assert.Equal(BorderStyle.FixedSingle, list.BorderStyle);
        Assert.Equal(UIColors.BorderMedium, list.BorderColor);

        foreach (Color edge in Edges(list))
        {
            Assert.Equal(UIColors.BorderMedium.ToArgb(), edge.ToArgb());
            Assert.True(ColorContrastHelper.GetContrastRatio(edge, UIColors.BackgroundMedium) >= 1.4, "the frame is seen against the window: " + edge);
        }
    }

    [Fact]
    public void EveryVariant_HasTheNeutralGrayFrame_AlsoWithAColorSetOrPrimary_ItIsNotTheColorOfTheLook()
    {
        foreach (WfuiListBox list in new[] { NewList(), NewList(UIStyles.Colors.Sets.Green), NewList(UIStyles.Colors.Sets.Yellow), NewList(primary: true) })
        {
            Shown(list);

            Assert.Equal(UIColors.BorderMedium, list.BorderColor);
            Assert.All(Edges(list), edge => Assert.Equal(UIColors.BorderMedium.ToArgb(), edge.ToArgb()));
        }

        // (The accent does not touch it either.)
        WfuiListBox primary = Shown(NewList(primary: true));
        UIColors.SetAccent(UIAccentColors.Red);
        Application.DoEvents();

        Assert.Equal(UIColors.BorderMedium, primary.BorderColor);
        Assert.All(Edges(primary), edge => Assert.Equal(UIColors.BorderMedium.ToArgb(), edge.ToArgb()));
    }

    [Fact]
    public void TheFrameCanBeGivenTheColorOfASet_ByHand()
    {
        WfuiListBox list = Shown(NewList());

        list.BorderColor = UIStyles.Colors.Sets.Green.Hover;
        Assert.All(Edges(list), edge => Assert.Equal(UIStyles.Colors.Sets.Green.Hover.ToArgb(), edge.ToArgb()));

        // A color that was set is kept when the theme changes.
        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();
        Assert.Equal(UIStyles.Colors.Sets.Green.Hover, list.BorderColor);
    }
    [Fact]
    public void ABorderColorThatWasSet_Wins_AndNoBorderStyle_MeansNoFrame()
    {
        WfuiListBox list = Shown(NewList(UIStyles.Colors.Sets.Green));

        list.BorderColor = Color.Orange;
        Assert.All(Edges(list), edge => Assert.Equal(Color.Orange.ToArgb(), edge.ToArgb()));

        list.BorderStyle = BorderStyle.None;
        Assert.DoesNotContain(Color.Orange.ToArgb(), Edges(list).Select(edge => edge.ToArgb()));
    }

    [Fact]
    public void TheAreaBelowTheRows_HasTheColorOfTheRows_NotAnotherGray()
    {
        foreach (bool light in new[] { false, true })
        {
            UIColors.ApplyTheme(light ? UIThemes.Light : UIThemes.Dark);
            WfuiListBox list = Shown(NewList());

            Assert.Equal(list.ItemBackColor, list.BackColor);

            using Bitmap picture = TestPictures.Draw(list);
            Color below = picture.GetPixel(list.Width / 2, list.Height - 4);

            Assert.Equal(list.ItemBackColor.ToArgb(), below.ToArgb());
        }
    }

    [Fact]
    public void TheTitledList_HasOneFrameAroundTheTitleAndTheList_TheInnerListHasNone()
    {
        TitledListBox titled = (TitledListBox)UIStyles.TitledListBoxes.CreateStandard("Title");
        titled.Items.AddRange(new object[] { "one", "two" });
        Shown(titled);

        Assert.Equal(BorderStyle.None, titled.InnerListBox.BorderStyle);
        Assert.Equal(new Padding(1), titled.Padding);
        Assert.Equal(UIColors.BorderMedium, titled.BorderColor);
        Assert.Equal(titled.BorderColor, titled.BackColor);
        Assert.All(Edges(titled), edge => Assert.Equal(UIColors.BorderMedium.ToArgb(), edge.ToArgb()));
    }

    [Fact]
    public void ThePrimaryTitledList_HasTheGrayFrameToo_AndTheColorCanBeSetByHand()
    {
        TitledListBox titled = (TitledListBox)UIStyles.TitledListBoxes.CreatePrimary("Title");
        Shown(titled);

        Assert.Equal(UIColors.BorderMedium, titled.BorderColor);
        Assert.Equal(UIColors.BorderMedium, titled.BackColor);

        UIColors.SetAccent(UIAccentColors.Green);
        Application.DoEvents();
        Assert.Equal(UIColors.BorderMedium, titled.BackColor);

        titled.BorderColor = Color.Orange;
        Assert.Equal(Color.Orange, titled.BackColor);
        Assert.Equal(Color.Orange, titled.InnerListBox.BorderColor);
    }
}
using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// The factories of the list box and of the titled one: CreateStandard is gray, with a color set (a set, or just a
/// color) the selection and the drop line take its color, CreatePrimary is the Standard one with the primary set.
/// [Collection]: the primary set reads the live accent, and the themes are switched.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ListBoxFactoryTests
{
    public static IEnumerable<object[]> Sets()
    {
        yield return new object[] { UIStyles.Colors.Sets.Green };
        yield return new object[] { UIStyles.Colors.Sets.Purple };
        yield return new object[] { UIStyles.Colors.Sets.Yellow };
        yield return new object[] { UIStyles.Colors.Sets.Primary };
        yield return new object[] { UIStyles.Colors.CreateSet(Color.FromArgb(200, 80, 20)) };
    }

    [Fact]
    public void CreateStandard_WithoutASet_IsTheNeutralGrayOfTheTheme()
    {
        using WfuiListBox listBox = UIStyles.ListBoxes.CreateStandard();

        Assert.Equal(UIColors.BorderLight, listBox.SelectedBackColor);
        Assert.Equal(UIColors.BorderLight, listBox.DragIndicatorColor);
        Assert.False(listBox.AllowReorder);
        Assert.False(listBox.ShowEnumeration);
    }

    [Theory]
    [MemberData(nameof(Sets))]
    public void CreateStandard_WithASet_TakesTheHoverColorOfTheSet(UIColorSet set)
    {
        using WfuiListBox listBox = UIStyles.ListBoxes.CreateStandard(colors: set);

        Assert.Equal(set.Hover, listBox.SelectedBackColor);
        Assert.Equal(set.Hover, listBox.DragIndicatorColor);
    }

    [Fact]
    public void ASelectedForeColor_ThatIsSet_WinsOverTheWorkedOutOne_AndEmptyGivesItBack()
    {
        using WfuiListBox listBox = UIStyles.ListBoxes.CreateStandard();
        Color worked = listBox.SelectedForeColor;

        listBox.SelectedForeColor = Color.HotPink;
        Assert.Equal(Color.HotPink, listBox.SelectedForeColor);
        Assert.Equal(Color.HotPink, listBox.InvokePrivate<Color>("GetTextColor", true, false));

        listBox.SelectedForeColor = Color.Empty;
        Assert.Equal(worked, listBox.SelectedForeColor);
    }

    [Fact]
    public void CreateStandard_WithAPlainColor_WorksAsASet()
    {
        using WfuiListBox listBox = UIStyles.ListBoxes.CreateStandard(colors: Color.Teal);

        Assert.Equal(((UIColorSet)Color.Teal).Hover, listBox.SelectedBackColor);
    }

    [Fact]
    public void CreatePrimary_IsTheStandardWithThePrimarySet_OfTheAccentAtCreation()
    {
        using WfuiListBox primary = UIStyles.ListBoxes.CreatePrimary();
        using WfuiListBox standard = UIStyles.ListBoxes.CreateStandard(colors: UIStyles.Colors.Sets.Primary);

        Assert.Equal(standard.SelectedBackColor, primary.SelectedBackColor);
        Assert.Equal(standard.DragIndicatorColor, primary.DragIndicatorColor);
        Assert.Equal(UIStyles.Colors.Sets.Primary.Hover, primary.DragIndicatorColor);
    }

    [Fact]
    public void TheOptions_ReachTheListBox()
    {
        using WfuiListBox listBox = UIStyles.ListBoxes.CreateStandard("Name", allowReorder: true, showEnumeration: true);

        Assert.Equal("Name", listBox.DisplayMember);
        Assert.True(listBox.AllowReorder);
        Assert.True(listBox.ShowEnumeration);
    }

    [Theory]
    [MemberData(nameof(Sets))]
    public void TheTextOnTheSelection_ReadsOnEverySet(UIColorSet set)
    {
        using WfuiListBox listBox = UIStyles.ListBoxes.CreateStandard(colors: set);

        Color text = listBox.InvokePrivate<Color>("GetTextColor", true, false);

        Assert.True(
            ColorContrastHelper.MeetsMinimumContrast(text, listBox.InvokePrivate<Color>("GetBackColor", 0, true, false, false), ColorContrastHelper.MinimumRatioNormalText),
            "the text on the selection does not read");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ASet_IsTheSameInBothThemes_AndTheTextReadsOnTheGrayOneToo(bool lightTheme)
    {
        try
        {
            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);

            using WfuiListBox colored = UIStyles.ListBoxes.CreateStandard(colors: UIStyles.Colors.Sets.Green);
            using WfuiListBox gray = UIStyles.ListBoxes.CreateStandard();

            Assert.Equal(UIStyles.Colors.Sets.Green.Hover, colored.SelectedBackColor);

            Color text = gray.InvokePrivate<Color>("GetTextColor", true, false);
            Assert.True(ColorContrastHelper.MeetsMinimumContrast(text, gray.InvokePrivate<Color>("GetBackColor", 0, true, false, false), ColorContrastHelper.MinimumRatioNormalText));
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }

    // ---- the titled one ----

    [Fact]
    public void TheTitledListBox_FollowsTheSameRules()
    {
        using TitledListBox gray = UIStyles.TitledListBoxes.CreateStandard("Title");
        using TitledListBox green = UIStyles.TitledListBoxes.CreateStandard("Title", colors: UIStyles.Colors.Sets.Green);
        using TitledListBox primary = UIStyles.TitledListBoxes.CreatePrimary("Title");

        Assert.Equal(UIColors.BorderLight, gray.SelectedBackColor);
        Assert.Equal(UIStyles.Colors.Sets.Green.Hover, green.SelectedBackColor);
        Assert.Equal(UIStyles.Colors.Sets.Green.Hover, green.DragIndicatorColor);
        Assert.Equal(UIStyles.Colors.Sets.Primary.Hover, primary.DragIndicatorColor);
        Assert.Equal("Title", green.Title);
        Assert.False(gray.AllowReorder);
    }

    [Fact]
    public void TheTitle_StaysNeutral_WhateverTheSet()
    {
        using TitledListBox gray = UIStyles.TitledListBoxes.CreateStandard("Title");
        using TitledListBox green = UIStyles.TitledListBoxes.CreateStandard("Title", colors: UIStyles.Colors.Sets.Green);

        Assert.Equal(gray.HeaderBackColor, green.HeaderBackColor);
        Assert.Equal(gray.HeaderForeColor, green.HeaderForeColor);
    }
}

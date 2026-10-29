using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.CheckBox;

/// <summary>The colors of the box of a check box and a radio button, in one place: the fill and the mark for each state, and the border. [Collection]: the sets read the live accent.</summary>
[Collection(AccentColorTestCollection.Name)]
public class BoxLookTests
{
    private static readonly Color DarkPage = Color.FromArgb(35, 35, 35);
    private static readonly Color LightPage = Color.White;

    [Fact]
    public void ADisabledBox_IsTheFixedGray_OnAnyPage_WhateverTheState()
    {
        UIColorSet set = UIStyles.Colors.Sets.Blue;

        foreach (Color page in new[] { DarkPage, LightPage })
        {
            BoxLook.GetFill(set, page, enabled: false, isHot: true, isPressed: true, out Color fill, out Color mark);

            Assert.Equal(UIColors.DisabledGray, fill);
            Assert.Equal(UIColors.GetContrastingForeColor(UIColors.DisabledGray), mark);
            Assert.Equal(UIColors.DisabledGray, BoxLook.GetBorderColor(set, page, false, true, true, false));
        }
    }

    [Fact]
    public void OnADarkPage_TheFillIsTheSetsOwn_ForRestHoverAndPress()
    {
        UIColorSet set = UIStyles.Colors.Sets.Blue;

        BoxLook.GetFill(set, DarkPage, true, false, false, out Color rest, out Color restMark);
        BoxLook.GetFill(set, DarkPage, true, true, false, out Color hot, out Color hotMark);
        BoxLook.GetFill(set, DarkPage, true, true, true, out Color pressed, out Color pressedMark);

        Assert.Equal((set.Basic, set.BasicText), (rest, restMark));
        Assert.Equal((set.Hover, set.HoverText), (hot, hotMark));
        Assert.Equal((set.Pressed, set.PressedText), (pressed, pressedMark));
    }

    [Fact]
    public void OnALightPage_TheBoxTakesTheNextBrighterShadeOfTheSet_SoTheColorIsSeen()
    {
        UIColorSet set = UIStyles.Colors.Sets.Blue;

        BoxLook.GetFill(set, LightPage, true, false, false, out Color rest, out _);
        BoxLook.GetFill(set, LightPage, true, false, true, out Color pressed, out _);

        Assert.Equal(set.Hover, rest);
        Assert.Equal(set.Basic, pressed);
    }

    [Fact]
    public void WhereTheMarkOfTheSetCannotBeSeenOnTheBrighterShade_TheDarkOrTheLightMarkIsTaken()
    {
        UIColorSet set = UIStyles.Colors.CreateSet(Color.FromArgb(0, 40, 110), Color.FromArgb(0, 80, 180), Color.FromArgb(235, 235, 255), Color.White);

        BoxLook.GetFill(set, LightPage, true, true, false, out Color fill, out Color mark);

        Assert.Equal(set.Pressed, fill);
        Assert.True(UIColors.GetContrastRatio(mark, fill) >= 3.0, "the mark " + mark + " cannot be seen on " + fill);
    }

    [Fact]
    public void TheBorder_IsTheOutlineAtRestAndWhenFilled_AndStrongerForAnEmptyBoxThatIsHoveredOrPressed()
    {
        UIColorSet set = UIStyles.Colors.Sets.Gray;

        Color outline = UIColors.GetOutlineColor(DarkPage);

        Assert.Equal(outline, BoxLook.GetBorderColor(set, DarkPage, true, false, false, false));
        Assert.Equal(outline, BoxLook.GetBorderColor(set, DarkPage, true, true, true, true));

        Color hot = BoxLook.GetBorderColor(set, DarkPage, true, true, false, false);
        Color pressed = BoxLook.GetBorderColor(set, DarkPage, true, true, true, false);

        Assert.True(UIColors.GetContrastRatio(hot, DarkPage) >= 4.5);
        Assert.True(UIColors.GetContrastRatio(pressed, DarkPage) >= 6.0);
    }
}

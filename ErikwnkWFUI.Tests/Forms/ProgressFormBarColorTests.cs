using System.Drawing;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using ProgressBar = ErikwnkWFUI.Controls.ProgressBar;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>ProgressForm.BarColors: the color of the bar while the task runs, and what takes over once it has ended.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ProgressFormBarColorTests : IDisposable
{
    private readonly AppFormHost _host = new AppFormHost();

    public void Dispose()
    {
        _host.Dispose();
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private static Color BarColor(ProgressForm form)
    {
        return form.GetPrivateField<ProgressBar>("_bar")!.ForeColor;
    }

    [Fact]
    public void WithoutBarColors_TheBarIsInTheAccent_OrInTheColorSetOfTheWindow()
    {
        ProgressForm accent = _host.Show(UIStyles.Forms.CreateProgressPrimary("x"));
        ProgressForm yellow = _host.Show(UIStyles.Forms.CreateProgressStandard("x", colors: UIStyles.Colors.Sets.Yellow));

        Assert.Null(accent.BarColors);
        Assert.NotEqual(BarColor(accent).ToArgb(), BarColor(yellow).ToArgb());
    }

    [Fact]
    public void BarColors_ColorTheBar_AndNullGivesTheDefaultBack()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressPrimary("x"));
        Color before = BarColor(form);

        form.BarColors = UIStyles.Colors.Sets.Green;
        Color green = BarColor(form);

        Assert.NotEqual(before.ToArgb(), green.ToArgb());
        Assert.Equal(UIStyles.ProgressBars.CreateGreen().ForeColor.ToArgb(), green.ToArgb());

        form.BarColors = null;
        Assert.Equal(before.ToArgb(), BarColor(form).ToArgb());
    }

    [Fact]
    public void OnceTheTaskHasEnded_TheBarIsGreenOrRed_WhateverBarColorsSay()
    {
        ProgressForm failed = _host.Show(UIStyles.Forms.CreateProgressPrimary("x"));
        failed.BarColors = UIStyles.Colors.Sets.Green;
        failed.Fail("it did not work");
        Assert.Equal(UIStyles.Colors.Sets.Red.Basic.ToArgb(), BarColor(failed).ToArgb());

        // (A color asked for after the end changes nothing either.)
        failed.BarColors = UIStyles.Colors.Sets.Yellow;
        Assert.Equal(UIStyles.Colors.Sets.Red.Basic.ToArgb(), BarColor(failed).ToArgb());

        ProgressForm succeeded = _host.Show(UIStyles.Forms.CreateProgressPrimary("x"));
        succeeded.BarColors = UIStyles.Colors.Sets.Yellow;
        succeeded.Succeed("done");
        Assert.Equal(UIStyles.Colors.Sets.Green.Basic.ToArgb(), BarColor(succeeded).ToArgb());
    }

    [Fact]
    public void TheBarColors_FollowTheTheme_AndTheAccentWhenTheSetDoes()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressPrimary("x"));
        form.BarColors = UIStyles.Colors.Sets.Primary;
        Color blue = BarColor(form);

        UIColors.SetAccent(UIAccentColors.Red);
        Application.DoEvents();

        Assert.NotEqual(blue.ToArgb(), BarColor(form).ToArgb());

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(UIStyles.ProgressBars.CreateStandard(UIStyles.Colors.Sets.Primary).ForeColor.ToArgb(), BarColor(form).ToArgb());
    }
}
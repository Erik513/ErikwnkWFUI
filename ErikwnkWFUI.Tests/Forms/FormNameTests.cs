using System.Drawing;
using ErikwnkWFUI.Forms;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The names of the forms: AppForm, FormOptions and OptionsForm, and the old "Styled" names that stay until the projects have been changed.</summary>
public class FormNameTests
{
    [Fact]
    public void CreateSettings_FillsInTheVersion_TheOtherPresetsHaveNone()
    {
        Assert.False(string.IsNullOrEmpty(FormOptions.CreateSettings("Settings").VersionText));
        Assert.Null(FormOptions.CreateStandard("Window").VersionText);
        Assert.Null(FormOptions.CreateDialog("Dialog").VersionText);
    }

    [Fact]
    public void AnAppForm_HasAVersionStrip_OnlyWhenTheOptionsHaveAVersionText()
    {
        using AppForm settings = new AppForm(FormOptions.CreateSettings("Settings"));
        using AppForm dialog = new AppForm(FormOptions.CreateDialog("Dialog"));
        using AppForm window = new AppForm(new FormOptions { Title = "Window", VersionText = "v9.9" });

        Assert.NotNull(settings.VersionStrip);
        Assert.Null(dialog.VersionStrip);
        Assert.Equal("v9.9", window.VersionLabel.Text);
    }

    [Fact]
    public void ADialog_HasNoMinimizeOrMaximize_AndAStandardWindowHasBoth()
    {
        FormOptions dialog = FormOptions.CreateDialog("Dialog");
        FormOptions window = FormOptions.CreateStandard("Window");

        Assert.False(dialog.ShowMinimizeButton || dialog.ShowMaximizeButton || dialog.AllowWindowSnapAndMaximize || dialog.Resizable);
        Assert.True(window.ShowMinimizeButton && window.ShowMaximizeButton && window.AllowWindowSnapAndMaximize && window.Resizable);
    }

#pragma warning disable CS0618
    [Fact]
    public void TheOldNames_StillMakeTheSameForms()
    {
        using StyledForm old = new StyledForm(StyledFormOptions.CreateDialog("Old"));
        using StyledForm quick = new StyledForm("Quick", StyledFormType.Settings);
        using StyledOptionsForm options = new StyledOptionsForm("Options");

        Assert.IsAssignableFrom<AppForm>(old);
        Assert.Equal("Old", old.FormTitle);
        Assert.NotNull(quick.VersionStrip);
        Assert.IsAssignableFrom<OptionsForm>(options);
    }

    [Fact]
    public void TheOldOptions_WithTypeSettings_StillFillInTheVersion_AnExplicitTextWins()
    {
        StyledFormOptions auto = new StyledFormOptions { Title = "a", Type = StyledFormType.Settings, TitleTextAlign = ContentAlignment.MiddleLeft };
        StyledFormOptions own = new StyledFormOptions { Title = "b", VersionText = "own", Type = StyledFormType.Settings };
        StyledFormOptions standard = new StyledFormOptions { Title = "c", Type = StyledFormType.Standard };

        Assert.False(string.IsNullOrEmpty(auto.VersionText));
        Assert.Equal("own", own.VersionText);
        Assert.Null(standard.VersionText);
    }
#pragma warning restore CS0618
}

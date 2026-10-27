using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.ColorContrastTestSupport;
using TextBox = System.Windows.Forms.TextBox;

namespace ErikwnkWFUI.Tests.Controls.TextBoxes;

/// <summary>A text box that is read-only is set apart from an editable one by its background, less than a disabled one, in both themes; the text stays readable.</summary>
[Collection(AccentColorTestCollection.Name)]
public class TextBoxReadOnlyTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private TextBox Show(TextBox box)
    {
        box.TabStop = false;
        Form form = OffscreenForm.Show(box, new Size(box.Width + 20, 40), UIColors.BackgroundMedium, new Point(10, 8));
        ThemeWatcher.FollowBackColor(form, () => UIColors.BackgroundMedium); // the page, as in an application
        _forms.Add(form);

        return box;
    }

    [Fact]
    public void AReadOnlyBox_HasADimmedBackground_AnEditableOneTheField_AndItComesBack()
    {
        TextBox box = Show(UIStyles.TextBoxes.CreateStandard("abc"));
        Color field = box.BackColor;

        box.ReadOnly = true;

        Assert.Equal(InputField.GetReadOnlyBackground(field), box.BackColor);
        Assert.NotEqual(field, box.BackColor);

        box.ReadOnly = false;

        Assert.Equal(field, box.BackColor);
    }

    [Fact]
    public void AReadOnlyBox_IsLessDimmedThanADisabledOne_AndADisabledReadOnlyBoxIsTheDisabledOne()
    {
        TextBox box = Show(UIStyles.TextBoxes.CreateStandard("abc"));
        TextBox disabled = Show(UIStyles.TextBoxes.CreateStandard("abc"));
        disabled.Enabled = false;
        box.ReadOnly = true;

        double toField = ColorContrastHelper.GetContrastRatio(UIColors.BackgroundLight, box.BackColor);
        double disabledToField = ColorContrastHelper.GetContrastRatio(UIColors.BackgroundLight, disabled.BackColor);
        Assert.True(toField < disabledToField, "a read-only box is dimmed as much as a disabled one");

        disabled.ReadOnly = true;
        Assert.Equal(InputField.GetDisabledBackground(UIColors.BackgroundLight), disabled.BackColor);

        disabled.Enabled = true;
        Assert.Equal(InputField.GetReadOnlyBackground(UIColors.BackgroundLight), disabled.BackColor);
    }

    [Fact]
    public void AReadOnlyBox_FollowsTheTheme_WhileItIsShown()
    {
        TextBox box = Show(UIStyles.TextBoxes.CreateStandard("abc"));
        box.ReadOnly = true;

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(InputField.GetReadOnlyBackground(UIColors.BackgroundLight), box.BackColor);
    }

    [Fact]
    public void ABackgroundSetByHand_IsLeftAlone_WhenTheBoxBecomesReadOnly()
    {
        TextBox box = Show(UIStyles.TextBoxes.CreateStandard("abc"));
        box.BackColor = Color.Lavender;

        box.ReadOnly = true;

        Assert.Equal(Color.Lavender, box.BackColor);
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheTextOfAReadOnlyBox_CanBeReadOnItsBackground_InBothThemes(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            TextBox box = Show(UIStyles.TextBoxes.CreateStandard("abc"));
            box.ReadOnly = true;

            Check(shortfalls, "text of a read-only box", box.ForeColor, box.BackColor, 7.0);
        });
    }

    [Fact]
    public void TheBorderlessAndThePrimaryBox_DoItToo()
    {
        TextBox borderless = Show(UIStyles.TextBoxes.CreateBorderless("abc"));
        TextBox primary = Show(UIStyles.TextBoxes.CreatePrimary("abc"));

        borderless.ReadOnly = true;
        primary.ReadOnly = true;

        Assert.Equal(InputField.GetReadOnlyBackground(UIColors.BackgroundLight), borderless.BackColor);
        Assert.Equal(InputField.GetReadOnlyBackground(UIColors.BackgroundLight), primary.BackColor);
    }

    [Fact]
    public void AnEditableBox_IsNotChangedByAnyOfThis()
    {
        TextBox box = Show(UIStyles.TextBoxes.CreateStandard("abc"));

        Assert.False(box.ReadOnly);
        Assert.Equal(UIColors.BackgroundLight, box.BackColor);
    }
}

using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// What the three input fields (text box, combo box, numeric up/down) have in common: one background color, a
/// dimmed one when disabled, and the frame around the field. [Collection]: the theme is switched.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class InputFieldTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private T Show<T>(T control) where T : Control
    {
        control.Location = new Point(10, 10);
        control.Width = 110;
        control.TabStop = false;
        Form form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-3000, 0), FormBorderStyle = FormBorderStyle.None, ClientSize = new Size(140, 60), BackColor = UIColors.BackgroundMedium };
        form.Controls.Add(control);
        ThemeWatcher.FollowBackColor(form, () => UIColors.BackgroundMedium); // the page, as in an application
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return control;
    }

    public static IEnumerable<object[]> Fields()
    {
        foreach (var entry in new (string, Func<Control>)[]
        {
            ("TextBox", () => UIStyles.TextBoxes.CreateStandard("x")),
            ("TextBox without border", () => UIStyles.TextBoxes.CreateBorderstyleNone("x")),
            ("ComboBox", () => UIStyles.ComboBoxes.CreateStandard()),
            ("NumericUpDown", () => UIStyles.NumericUpDowns.CreateStandard()),
            ("NumericUpDown primary", () => UIStyles.NumericUpDowns.CreatePrimary()),
        })
            yield return new object[] { entry.Item1, entry.Item2 };
    }

    [Theory]
    [MemberData(nameof(Fields))]
    public void EveryField_HasTheOneBackground_InBothThemes(string name, Func<Control> make)
    {
        using Control dark = make();
        Assert.Equal(UIColors.BackgroundLight, dark.BackColor);

        UIColors.ApplyTheme(UIThemes.Light);
        using Control light = make();

        Assert.Equal(UIColors.BackgroundLight, light.BackColor);
        Assert.NotEqual(dark.BackColor, light.BackColor);
    }

    [Theory]
    [MemberData(nameof(Fields))]
    public void ADisabledField_IsDimmed_AndTheOldBackgroundComesBackWhenEnabled(string name, Func<Control> make)
    {
        Control field = Show(make());
        Color enabled = field.BackColor;

        field.Enabled = false;
        Assert.Equal(InputField.GetDisabledBackground(enabled), field.BackColor);
        Assert.NotEqual(enabled, field.BackColor);

        field.Enabled = true;
        Assert.Equal(enabled, field.BackColor);
    }

    [Theory]
    [MemberData(nameof(Fields))]
    public void ADisabledShownField_FollowsTheTheme(string name, Func<Control> make)
    {
        Control field = Show(make());
        field.Enabled = false;

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(InputField.GetDisabledBackground(UIColors.BackgroundLight), field.BackColor);

        field.Enabled = true;

        Assert.Equal(UIColors.BackgroundLight, field.BackColor);
    }

    [Fact]
    public void ABackgroundSetByHand_StaysWhenDisabledAndEnabled()
    {
        TextBox box = Show(UIStyles.TextBoxes.CreateStandard("x"));
        box.BackColor = Color.Purple;

        box.Enabled = false;
        box.Enabled = true;
        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(Color.Purple, box.BackColor);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheDisabledBackground_IsDarker_LessOnALightTheme(bool light)
    {
        Color enabled = light ? UIThemes.Light.BackgroundLight : UIThemes.Dark.BackgroundLight;
        Color disabled = InputField.GetDisabledBackground(enabled);

        Assert.True(disabled.GetBrightness() < enabled.GetBrightness());
        Assert.True(UIColors.GetContrastRatio(disabled, enabled) < 2.0, "still the same kind of surface");
    }

    // ---- the frame ----

    private static Color Edge(Control control)
    {
        using Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));

        return bitmap.GetPixel(0, control.Height / 2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheTextBoxFrame_IsTheQuietOutlineOfThePage_InBothThemes(bool light)
    {
        UIColors.ApplyTheme(light ? UIThemes.Light : UIThemes.Dark);
        TextBox box = Show(UIStyles.TextBoxes.CreateStandard("x"));

        Assert.Equal(UIColors.GetOutlineColor(UIColors.BackgroundMedium).ToArgb(), Edge(box).ToArgb());
    }

    [Fact]
    public void WithTheFocus_TheTextBoxFrame_IsSeenAgainstTheField()
    {
        TextBox box = Show(UIStyles.TextBoxes.CreateStandard("x"));
        Color rest = Edge(box);

        box.Focus();
        box.Select(0, 0);
        Application.DoEvents();
        Color focused = Edge(box);

        Assert.NotEqual(rest.ToArgb(), focused.ToArgb());
        Assert.True(UIColors.GetContrastRatio(focused, box.BackColor) >= 4.5);
    }

    [Fact]
    public void ATextBoxWithoutABorder_HasNoFrame()
    {
        TextBox box = Show(UIStyles.TextBoxes.CreateBorderstyleNone("x"));

        Assert.Equal(box.BackColor.ToArgb(), Edge(box).ToArgb());
    }

    [Fact]
    public void TheNumericUpDownFrame_IsTheQuietOutline_AndWithTheFocusTheColorOfTheSet()
    {
        ErikwnkWFUI.Controls.NumericUpDown box = (ErikwnkWFUI.Controls.NumericUpDown)Show(UIStyles.NumericUpDowns.CreatePrimary());
        Assert.Equal(UIColors.GetOutlineColor(UIColors.BackgroundMedium).ToArgb(), Edge(box).ToArgb());

        box.Focus();
        Application.DoEvents();

        Assert.Equal(UIColors.EnsureContrast(UIColors.PrimarySet.Hover, box.BackColor, 3.0).ToArgb(), Edge(box).ToArgb());
    }

    [Fact]
    public void ANumericUpDownWithoutABorder_HasNoFrame()
    {
        ErikwnkWFUI.Controls.NumericUpDown box = (ErikwnkWFUI.Controls.NumericUpDown)Show(UIStyles.NumericUpDowns.CreateStandard());
        box.BorderStyle = BorderStyle.None;
        Application.DoEvents();

        Assert.Equal(box.BackColor.ToArgb(), Edge(box).ToArgb());
    }

    [Fact]
    public void TheFrame_FollowsTheTheme()
    {
        TextBox box = Show(UIStyles.TextBoxes.CreateStandard("x"));
        Color dark = Edge(box);

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.NotEqual(dark.ToArgb(), Edge(box).ToArgb());
        Assert.Equal(UIColors.GetOutlineColor(UIColors.BackgroundMedium).ToArgb(), Edge(box).ToArgb());
    }
}

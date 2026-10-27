using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using TextBox = System.Windows.Forms.TextBox;

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
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private T Show<T>(T control) where T : Control
    {
        control.Location = new Point(10, 10);
        control.Width = 110;
        control.TabStop = false;
        Form form = OffscreenForm.Create(new Size(140, 60), UIColors.BackgroundMedium);
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
            ("TextBox without border", () => UIStyles.TextBoxes.CreateBorderless("x")),
            ("TextBox primary", () => UIStyles.TextBoxes.CreatePrimary("x")),
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

    // ---- the text in the middle ----

    private static (int Above, int Below) DigitSpace(Control box) => InkMeasure.SpaceAboveAndBelow(box, 6);

    [Theory]
    [InlineData(8f)]
    [InlineData(9f)]
    [InlineData(10f)]
    [InlineData(11f)]
    [InlineData(12f)]
    [InlineData(14f)]
    [InlineData(18f)]
    public void TheTextOfATextBox_IsInTheMiddle_ForEveryFontSize(float points)
    {
        TextBox box = Show(UIStyles.TextBoxes.CreateStandard("8"));

        using Font font = new Font("Segoe UI", points);
        box.Font = font;
        Application.DoEvents();
        var space = DigitSpace(box);

        Assert.True(Math.Abs(space.Above - space.Below) <= 1, $"{points} pt: {space.Above} above, {space.Below} below");
    }

    [Fact]
    public void TheTextBox_StillShowsItsText_AndKeepsItsFullHeight()
    {
        TextBox box = Show(UIStyles.TextBoxes.CreateStandard("Hello"));

        Assert.Equal("Hello", box.Text);
        Assert.True(box.Height >= box.Font.Height + 6);
        Assert.True(box.ClientSize.Height >= box.Font.Height);
    }

    [Fact]
    public void TheCenteringFollowsTheFont_AfterAChangeAndBack()
    {
        TextBox box = Show(UIStyles.TextBoxes.CreateStandard("8"));

        using (Font big = new Font("Segoe UI", 20))
            box.Font = big;

        Application.DoEvents();
        using Font normal = new Font("Segoe UI", 9);
        box.Font = normal;
        Application.DoEvents();
        var space = DigitSpace(box);

        Assert.True(Math.Abs(space.Above - space.Below) <= 1, $"{space.Above} above, {space.Below} below");
    }

    [Fact]
    public void AMultilineBox_AndABorderlessOne_KeepTheNativeTextPlace()
    {
        TextBox multiline = Show(UIStyles.TextBoxes.CreateStandard("a"));
        multiline.Multiline = true;
        TextBox borderless = Show(UIStyles.TextBoxes.CreateBorderless("a"));

        Assert.Equal(0, InputField.GetTextTopInset(multiline));
        Assert.Equal(0, InputField.GetTextTopInset(borderless));
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

    [Trait("Focus", "true")]
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

    [Trait("Focus", "true")]
    [Fact]
    public void WithTheFocus_TheTextBoxFrame_HasTheColorOfTheSet_Primary_TheAccent_AndWithoutASetTheTextColor()
    {
        TextBox yellow = Show(UIStyles.TextBoxes.CreateStandard("x", colors: UIStyles.Colors.Sets.Yellow));
        yellow.Focus();
        Application.DoEvents();
        Assert.Equal(UIColors.EnsureContrast(UIStyles.Colors.Sets.Yellow.Hover, yellow.BackColor, 3.0).ToArgb(), Edge(yellow).ToArgb());

        TextBox primary = Show(UIStyles.TextBoxes.CreatePrimary("x"));
        primary.Focus();
        Application.DoEvents();
        Assert.Equal(UIColors.EnsureContrast(UIColors.PrimarySet.Hover, primary.BackColor, 3.0).ToArgb(), Edge(primary).ToArgb());

        TextBox plain = Show(UIStyles.TextBoxes.CreateStandard("x"));
        plain.Focus();
        Application.DoEvents();
        Assert.Equal(UIColors.EnsureContrast(plain.ForeColor, plain.BackColor, 4.5).ToArgb(), Edge(plain).ToArgb());
    }

    [Trait("Focus", "true")]
    [Fact]
    public void ThePrimaryTextBoxFrame_FollowsAChangeOfTheAccent_WhileTheBoxHasTheFocus()
    {
        TextBox primary = Show(UIStyles.TextBoxes.CreatePrimary("x"));
        primary.Focus();
        Application.DoEvents();
        Color before = Edge(primary);

        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Assert.NotEqual(before.ToArgb(), Edge(primary).ToArgb());
        Assert.Equal(UIColors.EnsureContrast(UIColors.PrimarySet.Hover, primary.BackColor, 3.0).ToArgb(), Edge(primary).ToArgb());
    }

    [Fact]
    public void WithoutTheFocus_TheColorsOfTheSet_DoNotChangeTheFrame()
    {
        TextBox plain = Show(UIStyles.TextBoxes.CreateStandard("x"));
        TextBox colored = Show(UIStyles.TextBoxes.CreateStandard("x", colors: UIStyles.Colors.Sets.Green));
        TextBox primary = Show(UIStyles.TextBoxes.CreatePrimary("x"));

        Assert.Equal(Edge(plain).ToArgb(), Edge(colored).ToArgb());
        Assert.Equal(Edge(plain).ToArgb(), Edge(primary).ToArgb());
    }

    [Fact]
    public void ATextBoxWithoutABorder_HasNoFrame()
    {
        TextBox box = Show(UIStyles.TextBoxes.CreateBorderless("x"));

        Assert.Equal(box.BackColor.ToArgb(), Edge(box).ToArgb());
    }

    [Trait("Focus", "true")]
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

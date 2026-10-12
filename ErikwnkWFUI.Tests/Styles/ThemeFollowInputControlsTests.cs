using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// Buttons, check boxes and combo boxes follow a change of the theme or the accent while they are shown: afterwards
/// they look as one made after the change - enabled and disabled. A color set by hand stays.
/// [Collection]: the theme and the accent are switched.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ThemeFollowInputControlsTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();
    private readonly Color _accentAtStart = UIColors.Primary;

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(_accentAtStart);
    }

    private T Show<T>(T control) where T : Control
    {
        control.Location = new Point(5, 5);
        control.TabStop = false;
        Form form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-3000, 0), FormBorderStyle = FormBorderStyle.None, ClientSize = new Size(260, 80), BackColor = Color.FromArgb(40, 90, 140) };
        form.Controls.Add(control);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return control;
    }

    private static string Look(Control control)
    {
        string look = $"{control.BackColor.ToArgb()} {control.ForeColor.ToArgb()}";

        if (control is Button button)
            look += $" {button.FlatAppearance.BorderColor.ToArgb()} {button.FlatAppearance.MouseOverBackColor.ToArgb()} {button.FlatAppearance.MouseDownBackColor.ToArgb()}";

        // The check box and the combo box paint themselves from the theme: the picture shows what they read.
        if (control is ErikwnkWFUI.Controls.CheckBox || control is ErikwnkWFUI.Controls.ComboBox || control is ErikwnkWFUI.Controls.NumericUpDown)
        {
            using Bitmap bitmap = new Bitmap(control.Width, control.Height);
            control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
            long sum = 0;

            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++)
                    sum = sum * 31 + bitmap.GetPixel(x, y).ToArgb();

            look += " " + sum;
        }

        return look;
    }

    public static IEnumerable<object[]> Controls()
    {
        foreach (var entry in new (string, Func<Control>)[]
        {
            ("Buttons.Standard", () => UIStyles.Buttons.CreateStandard("Text")),
            ("Buttons.Primary", () => UIStyles.Buttons.CreatePrimary("Text")),
            ("Buttons.Green", () => UIStyles.Buttons.CreateGreen("Text")),
            ("Buttons.Red", () => UIStyles.Buttons.CreateRed("Text")),
            ("Buttons.StandardSymbol", () => UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Add)),
            ("Buttons.ColoredSymbol", () => UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Add, "tip", null, UIStyles.Colors.Sets.Green)),
            ("CheckBoxes.Standard", () => UIStyles.CheckBoxes.CreateStandard("Text")),
            ("CheckBoxes.Primary", () => UIStyles.CheckBoxes.CreatePrimary("Text")),
            ("CheckBoxes.PrimaryCompact", () => UIStyles.CheckBoxes.CreatePrimaryCompact()),
            ("ComboBoxes.Standard", () => UIStyles.ComboBoxes.CreateStandard()),
            ("ComboBoxes.Primary", () => UIStyles.ComboBoxes.CreatePrimary()),
            ("TextBoxes.Standard", () => UIStyles.TextBoxes.CreateStandard("Text")),
            ("TextBoxes.Placeholder", () => UIStyles.TextBoxes.CreateStandard("", "Hint")),
            ("TextBoxes.None", () => UIStyles.TextBoxes.CreateBorderstyleNone("Text")),
            ("NumericUpDowns.Standard", () => UIStyles.NumericUpDowns.CreateStandard()),
            ("NumericUpDowns.Primary", () => UIStyles.NumericUpDowns.CreatePrimary()),
            ("NumericUpDowns.Green", () => UIStyles.NumericUpDowns.CreateStandard(colors: UIStyles.Colors.Sets.Green)),
        })
            yield return new object[] { entry.Item1, entry.Item2 };
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void AShownControl_LooksAsOneMadeAfterTheChange_InBothDirections(string name, Func<Control> make)
    {
        Control shown = Show(make());
        string dark = Look(shown);

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();
        Control madeInLight = Show(make());

        Assert.Equal(Look(madeInLight), Look(shown));

        UIColors.ApplyTheme(UIThemes.Dark);
        Application.DoEvents();
        Control madeInDark = Show(make());

        Assert.Equal(Look(madeInDark), Look(shown));
        Assert.Equal(dark, Look(shown));
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void AShownControl_LooksAsOneMadeAfterAChangeOfTheAccent(string name, Func<Control> make)
    {
        Control shown = Show(make());

        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Assert.Equal(Look(Show(make())), Look(shown));
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void ADisabledControl_FollowsToo_AndIsRightAgainWhenEnabled(string name, Func<Control> make)
    {
        Control shown = Show(make());
        shown.Enabled = false;

        UIColors.ApplyTheme(UIThemes.Light);
        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Control made = Show(make());
        made.Enabled = false;
        Assert.Equal(Look(made), Look(shown));

        shown.Enabled = true;
        made.Enabled = true;
        Assert.Equal(Look(made), Look(shown));
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void AShownControl_IsToldToPaintAgain(string name, Func<Control> make)
    {
        Control shown = Show(make());
        int invalidated = 0;
        shown.Invalidated += (s, e) => invalidated++;

        UIColors.ApplyTheme(UIThemes.Light);
        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Assert.True(invalidated > 0);
    }

    [Fact]
    public void AColorSetByHand_Stays()
    {
        Button button = Show(UIStyles.Buttons.CreateStandard("Text"));
        button.BackColor = Color.Red;
        button.FlatAppearance.BorderColor = Color.Yellow;
        ComboBox combo = Show(UIStyles.ComboBoxes.CreateStandard());
        combo.BackColor = Color.Purple;

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(Color.Red, button.BackColor);
        Assert.Equal(Color.Yellow, button.FlatAppearance.BorderColor);
        Assert.Equal(Color.Purple, combo.BackColor);
    }

    [Fact]
    public void AHoveredButton_GetsTheTextOfTheNewLook()
    {
        Button button = Show(UIStyles.Buttons.CreatePrimary("Text"));
        button.GetType().GetMethod("OnMouseEnter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(button, new object[] { EventArgs.Empty });

        UIColors.SetAccent(Color.Gold);
        Application.DoEvents();

        Assert.Equal(UIColors.PrimarySet.HoverText, button.ForeColor);
    }
}

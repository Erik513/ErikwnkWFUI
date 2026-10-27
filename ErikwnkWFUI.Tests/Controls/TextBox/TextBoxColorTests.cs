using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.ColorContrastTestSupport;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using TextBox = System.Windows.Forms.TextBox;

namespace ErikwnkWFUI.Tests.Controls.TextBoxes;

/// <summary>The colors of the text box in both themes: the placeholder and the button in the box (at rest, with the mouse over it, switched on, disabled) can be read on the field, and the frame while the box has the focus, in every variant.</summary>
[Collection(AccentColorTestCollection.Name)]
public class TextBoxColorTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private T Show<T>(T box, int width = 160) where T : TextBox
    {
        box.TabStop = false;
        box.Width = width;
        _forms.Add(OffscreenForm.Show(box, new Size(width + 20, 40), UIColors.BackgroundMedium, new Point(10, 8)));

        return box;
    }

    // The most different color of the area from the field: what the text or the symbol is painted in.
    private static double Strongest(TextBox box, Rectangle area)
    {
        using Bitmap picture = Draw(box);
        double strongest = 1.0;

        for (int x = Math.Max(area.Left, 0); x < Math.Min(area.Right, picture.Width); x++)
        {
            for (int y = Math.Max(area.Top, 0); y < Math.Min(area.Bottom, picture.Height); y++)
                strongest = Math.Max(strongest, ColorContrastHelper.GetContrastRatio(picture.GetPixel(x, y), box.BackColor));
        }

        return strongest;
    }

    private static Rectangle Inner(TextBox box)
    {
        Rectangle bounds = (Rectangle)box.GetType().GetProperty("InnerBounds", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(box)!;

        // (The picture of the control starts with the frame: the client area is one pixel inside it.)
        return Rectangle.Inflate(new Rectangle(bounds.X + 1, bounds.Y + 1, bounds.Width, bounds.Height), -3, -4);
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void ThePlaceholder_CanBeReadOnTheField_InBothThemes(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            TextBox box = Show(UIStyles.TextBoxes.CreateStandard("", "Placeholder"));
            double strongest = Strongest(box, new Rectangle(3, 3, 100, 14));

            if (strongest < 4.0)
                shortfalls.Add("placeholder: " + strongest.ToString("F2") + ":1, needs 4.0:1");
        });
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheButtonInTheBox_CanBeSeen_AtRest_OverAndSwitchedOn_AndDisabledIsFainter(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            LockableTextBox rest = Show(UIStyles.TextBoxes.CreateLockable("x"));
            LockableTextBox hot = Show(UIStyles.TextBoxes.CreateLockable("x"));
            LockableTextBox active = Show(UIStyles.TextBoxes.CreateLockable("x", locked: false));
            LockableTextBox disabled = Show(UIStyles.TextBoxes.CreateLockable("x"));
            hot.SetPrivateField("_innerHot", true);
            disabled.Enabled = false;

            double atRest = Strongest(rest, Inner(rest));
            double over = Strongest(hot, Inner(hot));
            double on = Strongest(active, Inner(active));
            double off = Strongest(disabled, Inner(disabled));

            if (atRest < 3.0)
                shortfalls.Add("button at rest: " + atRest.ToString("F2") + ":1, needs 3:1");

            if (over < 4.5)
                shortfalls.Add("button with the mouse over it: " + over.ToString("F2") + ":1, needs 4.5:1");

            if (on < 3.0)
                shortfalls.Add("button switched on: " + on.ToString("F2") + ":1, needs 3:1");

            if (off < 1.4)
                shortfalls.Add("disabled button: " + off.ToString("F2") + ":1, needs 1.4:1");

            if (over <= atRest)
                shortfalls.Add("the button is not stronger with the mouse over it (" + over.ToString("F2") + " against " + atRest.ToString("F2") + ")");

            if (off >= atRest)
                shortfalls.Add("a disabled button (" + off.ToString("F2") + ":1) is not fainter than one at rest (" + atRest.ToString("F2") + ":1)");
        });
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheFrameWithTheFocus_IsSeenOnTheField_ForEveryColorSetAndPrimary(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach ((string name, UIColorSet? set) in AllSets())
            {
                TextBox box = UIStyles.TextBoxes.CreateStandard("x", colors: set);
                Show(box);
                Color frame = InputField.GetFrameColor(box, set, true);

                Check(shortfalls, "frame with the focus, " + name, frame, box.BackColor, set == null ? 4.5 : 3.0);
            }

            TextBox primary = UIStyles.TextBoxes.CreatePrimary("x");
            Show(primary);
            Check(shortfalls, "frame with the focus, primary", InputField.GetFrameColor(primary, ((ThemedTextBox)primary).Colors, true), primary.BackColor, 3.0);
        });
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheEyeOfAShownPassword_AndThePencilOfALockedBox_TakeTheColorOfTheSet(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            LockableTextBox gray = Show(UIStyles.TextBoxes.CreateLockable("x", locked: false));
            LockableTextBox yellow = Show(UIStyles.TextBoxes.CreateLockable("x", locked: false, colors: UIStyles.Colors.Sets.Yellow));
            using Bitmap first = Draw(gray);
            using Bitmap second = Draw(yellow);

            if (SamePixels(first, second))
                shortfalls.Add("the check mark of a box with the colors of a set looks like the gray one");
        });
    }
}

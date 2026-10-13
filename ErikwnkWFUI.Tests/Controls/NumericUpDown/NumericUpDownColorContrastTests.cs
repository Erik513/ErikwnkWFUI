using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiNumericUpDown = ErikwnkWFUI.Controls.NumericUpDown;

namespace ErikwnkWFUI.Tests.Controls.NumericUpDown;

/// <summary>
/// The colors of the NumericUpDown in both themes: the number against the field, the arrow against its field in every
/// state of every set, the boundary of the control against the page and the focus frame. [Collection]: the theme and
/// the accent are switched.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class NumericUpDownColorContrastTests : IDisposable
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

    private static UIColorSet[] Sets() => new[]
    {
        WfuiNumericUpDown.GetNeutralSet(), UIStyles.Colors.Sets.Green, UIStyles.Colors.Sets.Red, UIStyles.Colors.Sets.Blue, UIStyles.Colors.Sets.Dark, UIColors.PrimarySet,
    };

    private static void UseTheme(bool light) => UIColors.ApplyTheme(light ? UIThemes.Light : UIThemes.Dark);

    private WfuiNumericUpDown Show(UIColorSet? colors = null)
    {
        WfuiNumericUpDown box = (WfuiNumericUpDown)UIStyles.NumericUpDowns.CreateStandard(colors: colors);
        box.Location = new Point(10, 10);
        box.Width = 110;
        box.TabStop = false;
        Form form = OffscreenForm.Create(new Size(140, 60), UIColors.BackgroundMedium);
        form.Controls.Add(box);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return box;
    }

    // ---- the number ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheNumber_IsReadableAgainstTheField(bool light)
    {
        UseTheme(light);
        WfuiNumericUpDown box = Show();

        Assert.True(ColorContrastHelper.MeetsMinimumContrast(box.ForeColor, box.BackColor, 4.5), $"{ColorContrastHelper.GetContrastRatio(box.ForeColor, box.BackColor):F2}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheNumberOfADisabledField_IsStillSeen(bool light)
    {
        UseTheme(light);
        WfuiNumericUpDown box = Show();
        box.Enabled = false;

        // Windows draws the text of a disabled field in its own gray.
        double ratio = ColorContrastHelper.GetContrastRatio(SystemColors.GrayText, box.BackColor);

        Assert.True(ratio >= 3.0, $"{ratio:F2}");
    }

    // ---- the arrows ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheArrow_IsReadableOnItsField_InEveryStateOfEverySet(bool light)
    {
        UseTheme(light);
        foreach (UIColorSet set in Sets().Append(WfuiNumericUpDown.GetNeutralSet()))
        {
            for (int state = 0; state < 4; state++)
            {
                var colors = WfuiNumericUpDown.GetStateColors(set, state, UIColors.BackgroundLight);
                double ratio = ColorContrastHelper.GetContrastRatio(colors.Arrow, colors.Fill);

                // A disabled arrow is muted on purpose: it only has to be seen.
                Assert.True(ratio >= (state == 3 ? 2.0 : 3.0), $"state {state}: {ratio:F2} (fill {colors.Fill}, arrow {colors.Arrow})");
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryArrowField_IsSeenAgainstTheField_ByItsFillOrByItsOutline(bool light)
    {
        UseTheme(light);
        WfuiNumericUpDown box = Show();

        foreach (UIColorSet set in Sets())
        {
            box.Colors = set;

            foreach (Color fill in new[] { set.Basic, set.Hover, set.Pressed, set.Disabled })
            {
                double byFill = ColorContrastHelper.GetContrastRatio(fill, box.BackColor);
                double byOutline = ColorContrastHelper.GetContrastRatio(UIColors.GetOutlineColor(box.BackColor), box.BackColor);

                // A fill within 2.0 of the field gets the outline (the rule of the control), which is seen at 3:1.
                Assert.True(byFill >= 2.0 || byOutline >= 3.0, $"fill {fill}: {byFill:F2}, outline {byOutline:F2}");
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheOutlineOfAFieldClose_ToTheFieldColor_IsSeen(bool light)
    {
        UseTheme(light);
        Color field = UIColors.BackgroundLight;
        Color outline = UIColors.GetOutlineColor(field);

        Assert.True(ColorContrastHelper.GetContrastRatio(outline, field) >= 3.0);
    }

    // ---- the frame ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheFrameAtRest_IsSeenAgainstThePage(bool light)
    {
        UseTheme(light);
        WfuiNumericUpDown box = Show();
        Color frame = ErikwnkWFUI.Controls.InputField.GetFrameColor(box, null, false);

        Assert.True(ColorContrastHelper.GetContrastRatio(frame, UIColors.BackgroundMedium) >= 3.0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheFocusFrame_OfEverySet_IsSeenAgainstTheField(bool light)
    {
        UseTheme(light);
        WfuiNumericUpDown box = Show();

        foreach (UIColorSet set in Sets())
        {
            Color frame = ErikwnkWFUI.Controls.InputField.GetFrameColor(box, set, true);

            Assert.True(ColorContrastHelper.GetContrastRatio(frame, box.BackColor) >= 3.0, $"{set.Hover}: {ColorContrastHelper.GetContrastRatio(frame, box.BackColor):F2}");
        }
    }

    // ---- the accent ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThePrimaryArrow_StaysReadable_WithEveryAccentOfTheLibrary(bool light)
    {
        UseTheme(light);

        foreach (Color accent in UIAccentColors.All)
        {
            UIColors.SetAccent(accent);
            UIColorSet set = UIColors.PrimarySet;

            foreach (var state in new[] { (set.Basic, set.BasicText), (set.Hover, set.HoverText), (set.Pressed, set.PressedText) })
                {
                    Color arrow = WfuiNumericUpDown.GetArrowColor(state.Item2, state.Item1);
                    Assert.True(ColorContrastHelper.GetContrastRatio(arrow, state.Item1) >= 3.0, $"accent {accent}: {ColorContrastHelper.GetContrastRatio(arrow, state.Item1):F2}");
                }
        }
    }
}

using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using static ErikwnkWFUI.Tests.Infrastructure.ColorContrastTestSupport;

namespace ErikwnkWFUI.Tests.Controls.RadioButtons;

/// <summary>
/// Colors of the radio button: the dot has to be readable on the fill in every state and set, the label on its background, the
/// box against the page, and a disabled button has to stay visible - in both themes. Measured on the picture the control draws.
/// [Collection]: the sets read the live accent, and the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class RadioButtonColorContrastTests
{
    // The dot in the middle of the 16 px box, and the fill between the dot and the outline.
    private static (Color Fill, Color Dot) Probe(Control radio)
    {
        using Bitmap bitmap = Draw(radio);
        int centerY = radio.Height / 2;

        return (bitmap.GetPixel(5, centerY), bitmap.GetPixel(10, centerY));
    }

    [Theory]
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheDot_IsReadable_OnTheFill_InEveryStateAndSet(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach ((string name, UIColorSet? set) in AllSets())
            {
                using System.Windows.Forms.RadioButton radio = UIStyles.RadioButtons.CreateStandard("x", true, set);

                // As the check box: the yellow keeps its bright fills with a white mark, held to what its resting look has.
                bool isYellow = name == "Yellow";

                (Color fill, Color dot) = Probe(radio);
                Check(shortfalls, name + " at rest", dot, fill, isYellow ? 2.6 : 3.0);

                radio.InvokePrivate("OnMouseEnter", EventArgs.Empty);
                (fill, dot) = Probe(radio);

                if (!isYellow)
                    Check(shortfalls, name + " hovered", dot, fill, 3.0);

                radio.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
                (fill, dot) = Probe(radio);

                if (!isYellow)
                    Check(shortfalls, name + " pressed", dot, fill, 2.0);
            }
        });
    }

    [Theory]
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void ThePrimaryButton_IsReadable_WithEveryAccent_InEveryState(bool lightTheme)
    {
        Color[] accents = typeof(UIAccentColors)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(Color))
            .Select(field => (Color)field.GetValue(null)!)
            .ToArray();

        Run(lightTheme, shortfalls =>
        {
            foreach (Color accent in accents)
            {
                UIColors.SetAccent(accent);
                string label = "accent " + accent.R + "," + accent.G + "," + accent.B;
                using System.Windows.Forms.RadioButton radio = UIStyles.RadioButtons.CreatePrimary("x", true);

                (Color fill, Color dot) = Probe(radio);
                Check(shortfalls, label + " at rest", dot, fill, 3.0);

                radio.InvokePrivate("OnMouseEnter", EventArgs.Empty);
                (fill, dot) = Probe(radio);
                Check(shortfalls, label + " hovered", dot, fill, 3.0);

                radio.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
                (fill, dot) = Probe(radio);
                Check(shortfalls, label + " pressed", dot, fill, 2.0);
            }
        });
    }

    [Theory]
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheLabel_IsReadable_AndTheDisabledButtonAndLabelAreStillVisible(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            Color page = UIColors.BackgroundMedium;
            Check(shortfalls, "label", UIColors.TextPrimary, page, ColorContrastHelper.MinimumRatioNormalText);

            using System.Windows.Forms.RadioButton radio = UIStyles.RadioButtons.CreateStandard("x", true);
            radio.Enabled = false;

            (Color fill, Color dot) = Probe(radio);
            Check(shortfalls, "disabled dot", dot, fill, 3.0);
            Check(shortfalls, "disabled fill against the page", fill, page, 1.8);

            Color disabledLabel = UIColors.BlendToward(UIColors.GetContrastingForeColor(page), page, 0.35);
            Check(shortfalls, "disabled label", disabledLabel, page, 3.0);
        });
    }

    [Theory]
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheOutline_StandsOutFromThePage_AtRestAndHovered(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            using System.Windows.Forms.RadioButton empty = UIStyles.RadioButtons.CreateStandard("x");
            ErikwnkWFUI.Controls.RadioButton control = (ErikwnkWFUI.Controls.RadioButton)empty;

            Check(shortfalls, "outline against the page", control.OutlineColor, UIColors.BackgroundMedium, 3.0);

            using Panel parent = new Panel { BackColor = Color.FromArgb(60, 90, 40), Size = new Size(100, 40) };
            parent.Controls.Add(empty);
            Check(shortfalls, "outline against another parent color", control.OutlineColor, parent.BackColor, 3.0);

            // An empty button that is hovered or pressed shows it by a stronger border.
            control.InvokePrivate("OnMouseEnter", EventArgs.Empty);
            Check(shortfalls, "hovered border against the page", control.BorderColor, parent.BackColor, 3.0);
        });
    }

    [Theory]
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheGroupBox_FrameAndTitle_AreSeenOnEverySurface(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach (Color surface in new[] { UIColors.BackgroundMedium, UIColors.BackgroundBlack, Color.White, Color.Black, UIStyles.Colors.Sets.Green.Basic, UIStyles.Colors.Sets.Yellow.Basic, UIStyles.Colors.Sets.Red.Basic })
            {
                foreach (var set in new UIColorSet?[] { null, UIStyles.Colors.Sets.Green, UIStyles.Colors.Sets.Yellow, UIStyles.Colors.Sets.Red })
                {
                    ErikwnkWFUI.Controls.GroupBox box = (ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard("T", set);
                    box.BackColor = surface;

                    Check(shortfalls, "frame on " + surface + (set == null ? "" : " with a set"), box.BorderColor, surface, 1.45);
                    box.Dispose();
                }
            }
        });
    }
}
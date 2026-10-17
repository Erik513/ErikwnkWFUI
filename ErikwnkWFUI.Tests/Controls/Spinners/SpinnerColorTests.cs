using System.Drawing;
using System.Windows.Forms;
using Panel = System.Windows.Forms.Panel;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.ColorContrastTestSupport;

namespace ErikwnkWFUI.Tests.Controls.Spinners;

/// <summary>Whether the ring, the arc and the text can be seen on what the spinner lies on: both themes, every color set, light and dark and colored surfaces.</summary>
[Collection(AccentColorTestCollection.Name)]
public class SpinnerColorTests
{
    // The surfaces a spinner may lie on: the one of the theme (null) and a few that are not.
    private static readonly Color?[] Surfaces =
    {
        null, Color.White, Color.Black, Color.FromArgb(240, 240, 240), Color.FromArgb(255, 235, 59), Color.FromArgb(20, 30, 60), Color.FromArgb(120, 120, 120)
    };

    private static string Name(Color? surface) => surface.HasValue ? surface.Value.ToString() : "theme surface";

    private static (Panel Panel, Spinner Spinner) Make(Color? surface, Func<Spinner> factory)
    {
        Panel panel = new Panel { BackColor = surface ?? UIColors.BackgroundMedium, Size = new Size(100, 100) };
        Spinner spinner = factory();
        panel.Controls.Add(spinner);

        return (panel, spinner);
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheTrack_IsSeenOnEverySurface_AndTheArcOnTheTrack_ForEveryColorSet(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach (Color? surface in Surfaces)
            {
                foreach ((string name, UIColorSet? set) in AllSets(includeDark: true))
                {
                    foreach (bool enabled in new[] { true, false })
                    {
                        (Panel panel, Spinner spinner) = Make(surface, () => UIStyles.Spinners.CreateStandard(40, 4, set));
                        spinner.Enabled = enabled;
                        Color track = spinner.GetTrackColor();
                        Color arc = spinner.InvokePrivate<Color>("ResolveArcColor");
                        string where = name + " on " + Name(surface) + (enabled ? "" : " (disabled)");

                        Check(shortfalls, "track of " + where, track, panel.BackColor, 1.5);
                        Check(shortfalls, "arc of " + where, arc, track, 3.0);
                        panel.Dispose();
                    }
                }
            }
        });
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheStatusArc_IsSeenOnTheTrack_AtEveryValue_AndThePrimaryArcForEveryAccent(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach (Color? surface in Surfaces)
            {
                for (int value = 0; value <= 100; value += 5)
                {
                    (Panel panel, Spinner spinner) = Make(surface, () => UIStyles.Spinners.CreateStatus(40, 4));
                    spinner.Value = value;
                    Check(shortfalls, "status arc at " + value + " on " + Name(surface), spinner.InvokePrivate<Color>("ResolveArcColor"), spinner.GetTrackColor(), 3.0);
                    panel.Dispose();
                }

                foreach (Color accent in new[] { UIAccentColors.Blue, Color.Yellow, Color.Black, Color.White, Color.OrangeRed })
                {
                    UIColors.SetAccent(accent);
                    (Panel panel, Spinner spinner) = Make(surface, () => UIStyles.Spinners.CreatePrimary(40, 4));
                    Check(shortfalls, "primary arc, accent " + accent + " on " + Name(surface), spinner.InvokePrivate<Color>("ResolveArcColor"), spinner.GetTrackColor(), 3.0);
                    panel.Dispose();
                }
            }
        });
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheDigitsAndTheText_AreReadableOnEverySurface(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach (Color? surface in Surfaces)
            {
                (Panel panel, Spinner spinner) = Make(surface, () => UIStyles.Spinners.CreateProgressPrimary());
                Check(shortfalls, "text on " + Name(surface), spinner.InvokePrivate<Color>("GetTextColor"), panel.BackColor, 4.5);

                spinner.Enabled = false;
                Check(shortfalls, "disabled text on " + Name(surface), spinner.InvokePrivate<Color>("GetTextColor"), panel.BackColor, 2.0);
                panel.Dispose();
            }
        });
    }

    [Fact]
    public void ASpinnerWithAColorOfItsOwn_LiesOnThatColor_NotOnTheParent()
    {
        using Panel panel = new Panel { BackColor = Color.White };
        using Spinner spinner = UIStyles.Spinners.CreateStandard();
        panel.Controls.Add(spinner);
        Color onWhite = spinner.GetTrackColor();

        // A surface close to the line color: the track has to move away from it.
        Color own = Color.FromArgb(75, 75, 75);
        spinner.BackColor = own;

        Assert.NotEqual(onWhite, spinner.GetTrackColor());
        Assert.Equal(UIColors.GetLineColor(own), spinner.GetTrackColor());
    }

    [Fact]
    public void AColorSetByHand_IsKeptAsItIs_EvenWhereItIsFaint()
    {
        using Panel panel = new Panel { BackColor = Color.White };
        using Spinner spinner = UIStyles.Spinners.CreateStandard();
        panel.Controls.Add(spinner);
        spinner.ArcColor = Color.FromArgb(250, 250, 250);

        Assert.Equal(Color.FromArgb(250, 250, 250), spinner.InvokePrivate<Color>("ResolveArcColor"));
    }
}

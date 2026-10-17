using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.ColorContrastTestSupport;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using Panel = System.Windows.Forms.Panel;
using TrackBar = ErikwnkWFUI.Controls.TrackBar;

namespace ErikwnkWFUI.Tests.Controls.TrackBars;

/// <summary>Whether the track, the filled part and the thumb can be seen on what the slider lies on (both themes, every color set, light, dark and colored surfaces), and that the popup has the colors of the slider.</summary>
[Collection(AccentColorTestCollection.Name)]
public class TrackBarColorTests : IDisposable
{
    private static readonly Color?[] Surfaces =
    {
        null, Color.White, Color.Black, Color.FromArgb(240, 240, 240), Color.FromArgb(255, 235, 59), Color.FromArgb(20, 30, 60), Color.FromArgb(120, 120, 120)
    };

    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private static string Name(Color? surface) => surface.HasValue ? surface.Value.ToString() : "theme surface";

    private static (Panel Panel, TrackBar Bar) Make(Color? surface, Func<TrackBar> factory)
    {
        Panel panel = new Panel { BackColor = surface ?? UIColors.BackgroundMedium, Size = new Size(200, 40) };
        TrackBar bar = factory();
        panel.Controls.Add(bar);

        return (panel, bar);
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheTrackIsSeenOnEverySurface_AndTheFillAndTheThumbOnTheSurfaceAndTheTrack_ForEveryColorSet(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach (Color? surface in Surfaces)
            {
                foreach ((string name, UIColorSet? set) in AllSets(includeDark: true))
                {
                    foreach (bool enabled in new[] { true, false })
                    {
                        (Panel panel, TrackBar bar) = Make(surface, () => UIStyles.TrackBars.CreateStandard(0.5, 1.0, set));
                        bar.Enabled = enabled;
                        Color track = bar.GetTrackColor();
                        Color fill = bar.GetFillColor();
                        string where = name + " on " + Name(surface) + (enabled ? "" : " (disabled)");

                        Check(shortfalls, "track of " + where, track, panel.BackColor, 1.5);
                        Check(shortfalls, "fill of " + where + " on the surface", fill, panel.BackColor, 2.5);
                        Check(shortfalls, "fill of " + where + " on the track", fill, track, 1.5);
                        panel.Dispose();
                    }
                }
            }
        });
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void ThePrimaryFill_IsSeenForEveryAccent_OnEverySurface(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach (Color? surface in Surfaces)
            {
                foreach (Color accent in new[] { UIAccentColors.Blue, Color.Yellow, Color.Black, Color.White, Color.OrangeRed })
                {
                    UIColors.SetAccent(accent);
                    (Panel panel, TrackBar bar) = Make(surface, () => UIStyles.TrackBars.CreatePrimary(0.5));
                    string where = "primary, accent " + accent + " on " + Name(surface);

                    Check(shortfalls, "fill of " + where + " on the surface", bar.GetFillColor(), panel.BackColor, 2.5);
                    Check(shortfalls, "fill of " + where + " on the track", bar.GetFillColor(), bar.GetTrackColor(), 1.5);
                    panel.Dispose();
                }
            }
        });
    }

    [Fact]
    public void AColorByHand_IsKeptAsItIs_AndALazyColorIsMovedAwayFromTheSurface()
    {
        using Panel panel = new Panel { BackColor = Color.White };
        using TrackBar bar = UIStyles.TrackBars.CreateStandard(0.5, 1.0, UIStyles.Colors.Sets.Yellow);
        panel.Controls.Add(bar);

        Color raw = UIStyles.Colors.Sets.Yellow.Hover;
        Assert.NotEqual(raw, bar.GetFillColor());   // the yellow of the set is too pale on white

        bar.FillColor = Color.FromArgb(250, 250, 250);
        Assert.Equal(Color.FromArgb(250, 250, 250), bar.GetFillColor());
    }

    [Fact]
    public void ASliderWithAColorOfItsOwn_LiesOnThatColor_NotOnTheParent()
    {
        using Panel panel = new Panel { BackColor = Color.White };
        using TrackBar bar = UIStyles.TrackBars.CreateStandard();
        panel.Controls.Add(bar);
        Color onWhite = bar.GetTrackColor();

        // A surface close to the line color: the track has to move away from it.
        Color own = Color.FromArgb(75, 75, 75);
        bar.BackColor = own;

        Assert.NotEqual(onWhite, bar.GetTrackColor());
        Assert.Equal(UIColors.GetQuietTrackColor(own), bar.GetTrackColor());
    }

    [Fact]
    public void ThePaintedTrackFillAndThumb_AreWhatIsWorkedOut_InTheLightTheme()
    {
        UIColors.ApplyTheme(UIThemes.Light);
        Color surface = Color.FromArgb(243, 243, 243);

        foreach (Func<TrackBar> make in new Func<TrackBar>[]
        {
            () => UIStyles.TrackBars.CreateStandard(0.5),
            () => UIStyles.TrackBars.CreatePrimary(0.5),
            () => UIStyles.TrackBars.CreateStandard(0.5, 1.0, UIStyles.Colors.Sets.Yellow)
        })
        {
            using TrackBar bar = make();
            bar.SetBounds(0, 0, 100, 20);
            using Form host = OffscreenForm.Create(new Size(100, 20), surface);
            host.Controls.Add(bar);
            host.Show();
            Application.DoEvents();
            using Bitmap picture = Draw(bar);

            Assert.Equal(bar.GetFillColor().ToArgb(), picture.GetPixel(50, 10).ToArgb());     // the thumb
            Assert.Equal(bar.GetFillColor().ToArgb(), picture.GetPixel(25, 10).ToArgb());     // the filled part
            Assert.Equal(bar.GetTrackColor().ToArgb(), picture.GetPixel(85, 10).ToArgb());    // the rest of the track
            Assert.True(ColorContrastHelper.GetContrastRatio(picture.GetPixel(50, 10), surface) >= 2.4, "the thumb is lost on the light surface");
            Assert.True(ColorContrastHelper.GetContrastRatio(picture.GetPixel(25, 10), picture.GetPixel(85, 10)) >= 1.4, "the filled part does not stand out from the track");
        }
    }

    // ---- the popup has the colors of the slider ----

    [Fact]
    public void ThePopup_HasTheColorsOfTheSlider_AndFollowsThemWhenTheyChange()
    {
        Func<Form, bool> before = PopupSupport.OwnerIsActive;
        PopupSupport.OwnerIsActive = _ => true;

        try
        {
            using TrackBar bar = UIStyles.TrackBars.CreatePrimary(0.5, 1.0, showValuePopup: true);
            using Form host = OffscreenForm.Create(new Size(100, 20), Color.Black);
            bar.SetBounds(0, 0, 100, 20);
            host.Controls.Add(bar);
            host.Show();
            Application.DoEvents();
            bar.InvokePrivate("OnMouseEnter", EventArgs.Empty);
            InfoPopupForm popup = bar.GetPrivateField<InfoPopupForm>("_popup")!;

            // The accent: the popup has the primary set, and follows the accent.
            Assert.Equal(UIColors.PrimarySet.Hover, popup.Colors!.Hover);
            UIColors.SetAccent(Color.OrangeRed);
            Assert.Equal(UIColors.PrimarySet.Hover, popup.Colors!.Hover);

            // Another set of the slider: the popup has that one.
            bar.Colors = UIStyles.Colors.Sets.Teal;
            Assert.Equal(UIStyles.Colors.Sets.Teal.Hover, popup.Colors!.Hover);

            // Gray: no set, the popup is gray as well.
            bar.Colors = null;
            Assert.Null(popup.Colors);
        }
        finally
        {
            PopupSupport.OwnerIsActive = before;
        }
    }

    [Fact]
    public void ADisabledSlider_ShowsNoPopup_AndTheOneThatWasShownGoes()
    {
        Func<Form, bool> before = PopupSupport.OwnerIsActive;
        PopupSupport.OwnerIsActive = _ => true;

        try
        {
            using TrackBar bar = new TrackBar { ShowValuePopup = true, Value = 0.5 };
            using Form host = OffscreenForm.Create(new Size(100, 20), Color.Black);
            bar.SetBounds(0, 0, 100, 20);
            host.Controls.Add(bar);
            host.Show();
            Application.DoEvents();

            bar.InvokePrivate("OnMouseEnter", EventArgs.Empty);
            InfoPopupForm popup = bar.GetPrivateField<InfoPopupForm>("_popup")!;
            Assert.True(popup.Visible);

            bar.Enabled = false;
            Assert.False(popup.Visible, "the popup stays on a disabled slider");

            bar.InvokePrivate("OnMouseEnter", EventArgs.Empty);
            Assert.False(popup.Visible, "a disabled slider shows its popup");
        }
        finally
        {
            PopupSupport.OwnerIsActive = before;
        }
    }
}

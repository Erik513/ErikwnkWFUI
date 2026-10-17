using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.Spinners;

/// <summary>When the spinner turns, what it shows for a known load, and what a screen reader is told.</summary>
[Collection(LanguageTestCollection.Name)]
public class SpinnerBehaviorTests : IDisposable
{
    private readonly Func<bool> _animations = AnimationSettings.Enabled;
    private readonly UILanguage _language = UIStrings.Language;

    public void Dispose()
    {
        AnimationSettings.Enabled = _animations;
        UIStrings.Language = _language;
    }

    private static Form Host(Spinner spinner)
    {
        Form host = OffscreenForm.Create(new Size(80, 80), Color.Black);
        spinner.SetBounds(10, 10, 60, 60);
        host.Controls.Add(spinner);
        host.Show();
        Application.DoEvents();

        return host;
    }

    private static bool Runs(Spinner spinner)
    {
        System.Windows.Forms.Timer timer = spinner.GetPrivateField<System.Windows.Forms.Timer>("_timer")!;

        return timer.Enabled && timer.Interval < 100;
    }

    [Fact]
    public void ItTurnsWhenShown_AndStopsWhenDisabledStoppedOrHidden()
    {
        using Spinner spinner = UIStyles.Spinners.CreatePrimary();
        using Form host = Host(spinner);

        Assert.True(Runs(spinner));

        spinner.IsSpinning = false;
        Assert.False(Runs(spinner));
        spinner.IsSpinning = true;
        Assert.True(Runs(spinner));

        spinner.Enabled = false;
        Assert.False(Runs(spinner));
        spinner.Enabled = true;
        Assert.True(Runs(spinner));

        spinner.Visible = false;
        Assert.False(Runs(spinner));
        spinner.Visible = true;
        Assert.True(Runs(spinner));
    }

    [Fact]
    public void WithTheAnimationsSwitchedOffInWindows_ItStandsStill_AndAKnownLoadNeverTurns()
    {
        AnimationSettings.Enabled = () => false;

        using Spinner off = UIStyles.Spinners.CreatePrimary();
        using Form host = Host(off);
        Assert.False(Runs(off));

        AnimationSettings.Enabled = _animations;
        using Spinner known = UIStyles.Spinners.CreateProgressPrimary();
        known.IsIndeterminate = false;
        using Form host2 = Host(known);
        Assert.False(Runs(known));

        known.IsIndeterminate = true;
        Assert.True(Runs(known));
    }

    [Fact]
    public void AKnownLoad_DrawsAnArcAsLongAsTheValue_FromTheTop_AndItDoesNotTurn()
    {
        using Spinner spinner = UIStyles.Spinners.CreateStandard(60, 8, UIStyles.Colors.Sets.Green);
        spinner.IsIndeterminate = false;
        using Form host = Host(spinner);
        Color arc = spinner.InvokePrivate<Color>("ResolveArcColor");

        // Cut so close to the ring that the pixel is on the stroke (12 o'clock, 3, 6, 9 o'clock).
        Point Top() => new Point(30, 5);
        Point Right() => new Point(55, 30);
        Point Bottom() => new Point(30, 55);
        Point Left() => new Point(5, 30);

        spinner.Value = 0;
        using (Bitmap none = Draw(spinner))
        {
            Assert.NotEqual(arc.ToArgb(), none.GetPixel(Top().X, Top().Y).ToArgb());
        }

        spinner.Value = 50;
        using (Bitmap half = Draw(spinner))
        {
            Assert.Equal(arc.ToArgb(), half.GetPixel(Right().X, Right().Y).ToArgb());
            Assert.Equal(arc.ToArgb(), half.GetPixel(Bottom().X + 2, Bottom().Y).ToArgb());
            Assert.NotEqual(arc.ToArgb(), half.GetPixel(Left().X, Left().Y).ToArgb());
        }

        spinner.Value = 100;
        using (Bitmap full = Draw(spinner))
        {
            Assert.Equal(arc.ToArgb(), full.GetPixel(Left().X, Left().Y).ToArgb());
        }

        // It stands still: the picture is the same later.
        spinner.Value = 30;
        using Bitmap first = Draw(spinner);
        Pump(100);
        using Bitmap second = Draw(spinner);
        Assert.True(SamePixels(first, second));
    }

    [Fact]
    public void ATurningArc_ChangesItsPicture_AndAStoppedOneDoesNot()
    {
        using Spinner spinner = UIStyles.Spinners.CreatePrimary(60, 8);
        using Form host = Host(spinner);

        using Bitmap before = Draw(spinner);
        Pump(150);
        using Bitmap after = Draw(spinner);
        Assert.False(SamePixels(before, after), "the arc did not turn");

        spinner.IsSpinning = false;
        using Bitmap stopped = Draw(spinner);
        Pump(150);
        using Bitmap later = Draw(spinner);
        Assert.True(SamePixels(stopped, later), "the stopped arc moved");
    }

    [Fact]
    public void AScreenReader_IsToldItIsAProgressBar_WithThePercentageOnlyForAKnownLoad()
    {
        UIStrings.Language = UILanguage.English;

        using Spinner spinner = UIStyles.Spinners.CreateProgressStandard();
        spinner.Value = 42;

        Assert.Equal(AccessibleRole.ProgressBar, spinner.AccessibilityObject.Role);
        Assert.Null(spinner.AccessibilityObject.Value);

        spinner.IsIndeterminate = false;
        Assert.Equal("42%", spinner.AccessibilityObject.Value);

        spinner.Text = "Loading";
        Assert.Equal("Loading", spinner.AccessibilityObject.Name);
    }
}

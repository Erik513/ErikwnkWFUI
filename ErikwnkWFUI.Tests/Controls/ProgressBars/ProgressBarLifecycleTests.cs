using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.ProgressBars;

/// <summary>What the bars do over their life that the other tests do not look at: the mouse that is gone, the designer, the questions to Windows.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ProgressBarLifecycleTests : IDisposable
{
    private const string LongText = "Connecting to the server, checking the certificate and loading the first part of the data";

    private readonly Func<bool> _animations = AnimationSettings.Enabled;

    public void Dispose()
    {
        AnimationSettings.Enabled = _animations;
        UIColors.ApplyTheme(UIThemes.Dark);
    }

    public static IEnumerable<object[]> BothBars()
    {
        yield return new object[] { "ProgressBar" };
        yield return new object[] { "SlimProgressBar" };
    }

    private static Control Make(string which, LabelScroll scroll)
    {
        if (which == "ProgressBar")
        {
            ErikwnkWFUI.Controls.ProgressBar thick = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
            thick.Text = LongText;
            thick.TextScroll = scroll;

            return thick;
        }

        SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();
        slim.Text = LongText;
        slim.TextScroll = scroll;

        return slim;
    }

    private static Form Host(Control bar)
    {
        Form host = OffscreenForm.Create(new Size(300, 60), Color.Black);
        bar.SetBounds(0, 5, 300, 26);
        host.Controls.Add(bar);
        host.Show();
        Application.DoEvents();
        using Bitmap picture = Draw(bar);

        return host;
    }

    private static ProgressTextScroller Scroller(Control bar) => bar.GetPrivateField<ProgressTextScroller>("_textScroller")!;

    // --- A mouse that is gone without a MouseLeave ---

    [Theory]
    [MemberData(nameof(BothBars))]
    public void ABarThatIsDisabledOrHiddenUnderTheMouse_DoesNotKeepItsTextStill(string which)
    {
        LabelScroll scroll = LabelScroll.BackAndForth(LabelScrollSpeed.Fast, LabelScrollPause.Short, pauseOnHover: true);
        Control bar = Make(which, scroll);
        using Form host = Host(bar);
        ProgressTextScroller scroller = Scroller(bar);

        // Disabled while the mouse is on it: it gets no MouseLeave.
        scroller.SetHovered(true);
        bar.Enabled = false;
        bar.Enabled = true;
        using (Bitmap picture = Draw(bar)) { }
        scroller.Advance(2500);
        Assert.True(scroller.Offset > 0, "the text is still held by a mouse that is gone (disabled)");

        // The same for a bar that was hidden.
        bar.Text = LongText + " (again)";
        using (Bitmap picture = Draw(bar)) { }
        scroller.SetHovered(true);
        bar.Visible = false;
        bar.Visible = true;
        using (Bitmap picture = Draw(bar)) { }
        scroller.Advance(2500);
        Assert.True(scroller.Offset > 0, "the text is still held by a mouse that is gone (hidden)");
    }

    // --- A window that is resized ---

    [Theory]
    [MemberData(nameof(BothBars))]
    public void AMovingText_KeepsItsPlace_WhenTheBarIsResized_AndBeginsAgainForAnotherText(string which)
    {
        // (Back and forth: how far the text has to go depends on the room. Endless: it does not, but the place stays all the same.)
        foreach (LabelScroll scroll in new[]
        {
            LabelScroll.BackAndForth(LabelScrollSpeed.Fast, LabelScrollPause.Short),
            LabelScroll.Endless(LabelScrollDirection.Left, LabelScrollSeparator.Dot, LabelScrollSpeed.Fast)
        })
        {
            Control bar = Make(which, scroll);
            using Form host = Host(bar);
            ProgressTextScroller scroller = Scroller(bar);

            scroller.Advance(2000);
            Assert.True(scroller.Offset > 0, "the text did not move: " + scroll.Mode);

            // The window grows by a few pixels (again and again while it is dragged): the text goes on from where it is.
            bar.Width = 280;
            using (Bitmap picture = Draw(bar)) { }
            Assert.True(scroller.IsMoving);
            Assert.True(scroller.Offset > 0, "the text jumped back to its start when the bar was resized: " + scroll.Mode);

            // Another text begins again at the start.
            bar.Text = LongText + " - and another one";
            using (Bitmap picture = Draw(bar)) { }
            Assert.Equal(0, scroller.Offset);
        }
    }

    // --- Right to left ---

    [Fact]
    public void TheBar_IsMirroredByWindows_WithARightToLeftLayout_AndThePlainRightToLeftIsIgnored()
    {
        ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
        bar.Value = 30;
        using Form host = Host(bar);
        Color fill = bar.ForeColor;

        bar.RightToLeft = RightToLeft.Yes;
        using (Bitmap plain = Draw(bar))
        {
            Assert.Equal(fill.ToArgb(), plain.GetPixel(5, 11).ToArgb());
            Assert.NotEqual(fill.ToArgb(), plain.GetPixel(bar.Width - 5, 11).ToArgb());
        }

        bar.RightToLeftLayout = true;
        Application.DoEvents();
        using (Bitmap mirrored = Draw(bar))
        {
            Assert.Equal(fill.ToArgb(), mirrored.GetPixel(bar.Width - 5, 11).ToArgb());
            Assert.NotEqual(fill.ToArgb(), mirrored.GetPixel(5, 11).ToArgb());
        }
    }

    // --- The designer ---

    [Fact]
    public void TheDesigner_WritesTheColorsOfTheBars_OnlyWhereTheyWereSet_AndCanResetThem()
    {
        SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();
        ErikwnkWFUI.Controls.ProgressBar thick = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();

        foreach ((Control bar, string property, Color color) in new (Control, string, Color)[]
        {
            (slim, "BackColor", Color.Purple), (slim, "ForeColor", Color.Orange), (thick, "BackColor", Color.Teal)
        })
        {
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(bar)[property]!;

            Assert.False(descriptor.ShouldSerializeValue(bar), property + " of " + bar.GetType().Name + " is written without being set");

            descriptor.SetValue(bar, color);
            Assert.True(descriptor.ShouldSerializeValue(bar), property + " of " + bar.GetType().Name + " is not written after it was set");
            Assert.True(descriptor.CanResetValue(bar));

            descriptor.ResetValue(bar);
            Assert.False(descriptor.ShouldSerializeValue(bar), property + " of " + bar.GetType().Name + " is still written after the reset");
        }
    }

    // --- What Windows is asked ---

    [Theory]
    [MemberData(nameof(BothBars))]
    public void WhetherWindowsLetsWindowsAnimate_IsNotAskedOnEveryPicture(string which)
    {
        int asked = 0;
        AnimationSettings.Enabled = () =>
        {
            asked++;

            return true;
        };

        Control bar = Make(which, LabelScroll.Endless(LabelScrollDirection.Left, LabelScrollSeparator.Dot, LabelScrollSpeed.Fast));
        using Form host = Host(bar);
        ProgressTextScroller scroller = Scroller(bar);
        Assert.True(scroller.IsMoving);

        asked = 0;

        for (int picture = 0; picture < 60; picture++)
        {
            scroller.Advance(16);
            using Bitmap drawn = Draw(bar);
        }

        Assert.True(asked <= 2, "Windows was asked " + asked + " times for 60 pictures");
    }

    [Theory]
    [MemberData(nameof(BothBars))]
    public void ATextThatStoodStill_BecauseWindowsHadItsAnimationsOff_MovesWhenTheyAreSwitchedOn(string which)
    {
        AnimationSettings.Enabled = () => false;

        Control bar = Make(which, LabelScroll.Endless(LabelScrollDirection.Left, LabelScrollSeparator.Dot, LabelScrollSpeed.Fast));
        using Form host = Host(bar);
        ProgressTextScroller scroller = Scroller(bar);
        Assert.False(scroller.IsMoving);

        // Nothing paints a bar that stands still: the system has to tell it (the bar is told to paint; a window far off the screen is
        // not painted at all, so the picture is made here).
        int paints = 0;
        bar.Invalidated += (sender, e) => paints++;

        AnimationSettings.Enabled = _animations;
        AnimationSettings.Raise();
        Pump(100);
        Assert.True(paints > 0, "the bar is not told to paint again");
        using (Bitmap picture = Draw(bar)) { }

        Assert.True(scroller.IsMoving, "the text does not move although Windows lets windows animate again");

        // And it stops again when they are switched off.
        AnimationSettings.Enabled = () => false;
        AnimationSettings.Raise();
        Pump(100);
        using (Bitmap picture = Draw(bar)) { }

        Assert.False(scroller.IsMoving);
    }

    [Theory]
    [MemberData(nameof(BothBars))]
    public void ABarListensForTheSettingsOfWindows_OnlyWhileItHasATextThatMoves(string which)
    {
        int before = AnimationSettings.Count;
        Control bar = Make(which, LabelScroll.None);
        Assert.Equal(before, AnimationSettings.Count);

        bar.GetType().GetProperty("TextScroll")!.SetValue(bar, LabelScroll.BackAndForth());
        Assert.Equal(before + 1, AnimationSettings.Count);

        bar.GetType().GetProperty("TextScroll")!.SetValue(bar, LabelScroll.None);
        Assert.Equal(before, AnimationSettings.Count);

        bar.GetType().GetProperty("TextScroll")!.SetValue(bar, LabelScroll.BackAndForth());
        bar.Dispose();
        Assert.Equal(before, AnimationSettings.Count);
    }

    // --- The tooltip helper ---

    [Fact]
    public void TheFullTextToolTip_HasTheTextOnlyWhileTheControlCutsIt_AndItIsEnabled()
    {
        using Control owner = new Control();
        FullTextToolTip tip = new FullTextToolTip(owner);

        Assert.True(tip.Enabled);

        tip.Update("whole text", cut: false);
        Assert.Equal("", tip.Text);

        tip.Update("whole text", cut: true);
        Assert.Equal("whole text", tip.Text);

        tip.Enabled = false;
        Assert.Equal("", tip.Text);

        tip.Update("whole text", cut: true);
        Assert.Equal("", tip.Text);

        tip.Enabled = true;
        tip.Update("whole text", cut: true);
        Assert.Equal("whole text", tip.Text);

        tip.Dispose();
        Assert.Equal("", tip.Text);
    }
}

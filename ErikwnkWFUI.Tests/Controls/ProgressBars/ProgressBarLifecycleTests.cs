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

    private readonly Func<bool> _animations = ErikwnkWFUI.Controls.ThemedLabel.AnimationsEnabled;

    public void Dispose()
    {
        ErikwnkWFUI.Controls.ThemedLabel.AnimationsEnabled = _animations;
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
        ErikwnkWFUI.Controls.ThemedLabel.AnimationsEnabled = () =>
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

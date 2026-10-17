using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using TrackBar = ErikwnkWFUI.Controls.TrackBar;

namespace ErikwnkWFUI.Tests.Controls.TrackBars;

/// <summary>The range, the steps, the keys and the wheel, the grip on the thumb, the way a value is written, and the popup switch.</summary>
public class TrackBarFeatureTests
{
    private static Form Show(TrackBar bar, int width = 120)
    {
        Form host = OffscreenForm.Create(new Size(width, 20), Color.Black);
        bar.SetBounds(0, 0, width, 20);
        host.Controls.Add(bar);
        host.Show();
        Application.DoEvents();

        return host;
    }

    private static void Key(TrackBar bar, Keys key) => bar.InvokePrivate("OnKeyDown", new KeyEventArgs(key));

    private static void Wheel(TrackBar bar, int notches) => bar.InvokePrivate("OnMouseWheel", new HandledMouseEventArgs(MouseButtons.None, 0, 50, 10, notches * 120));

    private static MouseEventArgs Mouse(int x) => new MouseEventArgs(MouseButtons.Left, 1, x, 10, 0);

    // ---- the range ----

    [Fact]
    public void TheRange_IsAnyRange_AndAValueOutsideIsLimitedToIt()
    {
        using TrackBar bar = new TrackBar { Minimum = -12, Maximum = 12 };

        bar.Value = 30;
        Assert.Equal(12, bar.Value);
        bar.Value = -30;
        Assert.Equal(-12, bar.Value);
        bar.Value = 3;
        Assert.Equal(3, bar.Value);

        // A range that shrinks takes the value with it, from either side.
        bar.Maximum = 2;
        Assert.Equal(2, bar.Value);
        bar.Minimum = 5;
        Assert.True(bar.Maximum > bar.Minimum, "an empty range");
        Assert.Equal(bar.Value, Math.Min(Math.Max(bar.Value, bar.Minimum), bar.Maximum));
    }

    [Fact]
    public void TheTopIsAlwaysAboveTheBottom()
    {
        using TrackBar bar = new TrackBar();

        bar.Maximum = -5;
        Assert.True(bar.Maximum > bar.Minimum);

        bar.Maximum = 10;
        bar.Minimum = 20;
        Assert.True(bar.Minimum < bar.Maximum);
    }

    [Fact]
    public void TheThumbAndTheClick_FollowTheRange()
    {
        using TrackBar bar = new TrackBar { Minimum = -10, Maximum = 10 };
        using Form host = Show(bar);
        List<double> values = new List<double>();
        bar.ValueChanged += (s, e) => values.Add(bar.Value);

        // The track is 106 px wide from x = 7: its left end is the minimum, its right end the maximum.
        bar.InvokePrivate("OnMouseDown", Mouse(7));
        bar.InvokePrivate("OnMouseUp", Mouse(7));
        bar.InvokePrivate("OnMouseDown", Mouse(113));
        bar.InvokePrivate("OnMouseUp", Mouse(113));
        bar.InvokePrivate("OnMouseDown", Mouse(60));
        bar.InvokePrivate("OnMouseUp", Mouse(60));

        Assert.Equal(3, values.Count);
        Assert.Equal(-10, values[0], 3);
        Assert.Equal(10, values[1], 3);
        Assert.Equal(-10 + 53 / 106.0 * 20, values[2], 2);
    }

    // ---- the keys ----

    [Fact]
    public void TheKeys_MoveByTheSmallAndTheLargeChange_AndHomeAndEndGoToTheEnds()
    {
        using TrackBar bar = new TrackBar { Minimum = 0, Maximum = 100, SmallChange = 2, LargeChange = 20 };
        using Form host = Show(bar);
        bar.Value = 50;

        Key(bar, Keys.Right);
        Assert.Equal(52, bar.Value);
        Key(bar, Keys.Up);
        Assert.Equal(54, bar.Value);
        Key(bar, Keys.Left);
        Key(bar, Keys.Down);
        Assert.Equal(50, bar.Value);
        Key(bar, Keys.PageUp);
        Assert.Equal(70, bar.Value);
        Key(bar, Keys.PageDown);
        Key(bar, Keys.PageDown);
        Assert.Equal(30, bar.Value);
        Key(bar, Keys.End);
        Assert.Equal(100, bar.Value);
        Key(bar, Keys.Home);
        Assert.Equal(0, bar.Value);
    }

    [Fact]
    public void TheChanges_AreATwentiethAndATenthOfTheRangeByDefault_AndZeroGivesTheDefaultBack()
    {
        using TrackBar bar = new TrackBar { Minimum = 0, Maximum = 200 };

        Assert.Equal(10, bar.SmallChange);
        Assert.Equal(20, bar.LargeChange);

        bar.SmallChange = 3;
        bar.LargeChange = 40;
        Assert.Equal(3, bar.SmallChange);
        Assert.Equal(40, bar.LargeChange);

        bar.SmallChange = 0;
        bar.LargeChange = -1;
        Assert.Equal(10, bar.SmallChange);
        Assert.Equal(20, bar.LargeChange);

        bar.Maximum = 100;
        Assert.Equal(5, bar.SmallChange);
    }

    [Fact]
    public void TheKeys_RaiseValueChangedOnce_AndNothingMovesOnADisabledBar_OrAtTheEnd()
    {
        using TrackBar bar = new TrackBar { Maximum = 100, Value = 100 };
        using Form host = Show(bar);
        int changed = 0;
        bar.ValueChanged += (s, e) => changed++;

        Key(bar, Keys.Right);
        Assert.Equal(0, changed);

        Key(bar, Keys.Left);
        Assert.Equal(1, changed);

        bar.Enabled = false;
        Key(bar, Keys.Left);
        Assert.Equal(1, changed);
    }

    // ---- the wheel ----

    [Fact]
    public void TheWheel_MovesBySmallChange_UpIsMore_AndTheParentDoesNotScroll()
    {
        using TrackBar bar = new TrackBar { Maximum = 100, SmallChange = 5, Value = 50 };
        using Form host = Show(bar);
        int changed = 0;
        bar.ValueChanged += (s, e) => changed++;

        Wheel(bar, 1);
        Assert.Equal(55, bar.Value);
        Wheel(bar, -2);
        Assert.Equal(45, bar.Value);
        Assert.Equal(2, changed);

        HandledMouseEventArgs args = new HandledMouseEventArgs(MouseButtons.None, 0, 50, 10, 120);
        bar.InvokePrivate("OnMouseWheel", args);
        Assert.True(args.Handled);

        bar.Enabled = false;
        Wheel(bar, 3);
        Assert.Equal(50, bar.Value);
    }

    // ---- the grip on the thumb ----

    [Fact]
    public void APressOnTheThumb_TakesHoldOfItWhereItWasPressed_AndAPressOnTheTrackPutsTheThumbThere()
    {
        using TrackBar bar = new TrackBar { Maximum = 100, Value = 50 };
        using Form host = Show(bar);

        // The thumb stands at 60; pressed 4 px to its right, it does not jump.
        bar.InvokePrivate("OnMouseDown", Mouse(64));
        Assert.Equal(50, bar.Value, 3);

        // Moved by 20 px, the thumb goes by 20 px (the track is 106 px wide).
        bar.InvokePrivate("OnMouseMove", Mouse(84));
        Assert.Equal(50 + 20 / 106.0 * 100, bar.Value, 1);
        bar.InvokePrivate("OnMouseUp", Mouse(84));

        // Pressed on the track, away from the thumb, it goes under the mouse.
        bar.InvokePrivate("OnMouseDown", Mouse(20));
        Assert.Equal((20 - 7) / 106.0 * 100, bar.Value, 1);
        bar.InvokePrivate("OnMouseUp", Mouse(20));
    }

    // ---- how a value is written ----

    [Fact]
    public void AValueIsWrittenAsANumber_OrAsPercent_AndTheFormatCanBeSet()
    {
        using TrackBar bar = new TrackBar { Minimum = -12, Maximum = 12 };

        CultureInfo culture = UIStrings.NumberCulture;
        Assert.Equal((3.5).ToString("0.##", culture), bar.FormatValue(3.5));
        Assert.Equal((0.1).ToString("P0", culture), TrackBar.FormatPercent(0.1));
        Assert.Equal((1.0).ToString("P0", culture), TrackBar.FormatPercent(1.0));
        Assert.Equal((0.0).ToString("P0", culture), TrackBar.FormatPercent(0));

        bar.FormatValue = value => value + " dB";
        Assert.Equal("3 dB", bar.FormatValue(3));

        bar.FormatValue = null;
        Assert.Equal((3.5).ToString("0.##", culture), bar.FormatValue(3.5));

        bar.FormatValue = TrackBar.FormatPercent;
        Assert.Equal((0.1).ToString("P0", culture), bar.FormatValue(0.1));
    }

    [Fact]
    public void AScreenReader_IsToldItIsASlider_WithTheValueAsWritten()
    {
        using TrackBar bar = new TrackBar { Minimum = -12, Maximum = 12, Value = 3 };
        bar.FormatValue = value => Math.Round(value) + " dB";

        Assert.Equal(AccessibleRole.Slider, bar.AccessibilityObject.Role);
        Assert.Equal("3 dB", bar.AccessibilityObject.Value);

        bar.Value = -6;
        Assert.Equal("-6 dB", bar.AccessibilityObject.Value);

        using TrackBar volume = new TrackBar { Value = 0.42, FormatValue = TrackBar.FormatPercent };
        Assert.Equal((0.42).ToString("P0", UIStrings.NumberCulture), volume.AccessibilityObject.Value);
    }

    // ---- the popup switch ----

    [Fact]
    public void TheValuePopup_IsOffByDefault_AndTheDesignerWritesItOnlyWhenItIsOn()
    {
        using TrackBar bar = new TrackBar();

        Assert.False(bar.ShowValuePopup);

        PropertyDescriptor property = TypeDescriptor.GetProperties(bar)["ShowValuePopup"]!;
        Assert.False(property.ShouldSerializeValue(bar));

        bar.ShowValuePopup = true;
        Assert.True(bar.ShowValuePopup);
        Assert.True(property.ShouldSerializeValue(bar));

        property.ResetValue(bar);
        Assert.False(bar.ShowValuePopup);
    }

    [Fact]
    public void ThePopup_ShowsTheValueWhenTheUserChangesIt_AndOnlyWhenItIsSwitchedOn()
    {
        using TrackBar off = new TrackBar { Maximum = 100 };
        using TrackBar on = new TrackBar { Maximum = 100, ShowValuePopup = true };
        using Form hostOff = Show(off);
        using Form hostOn = Show(on);

        Key(off, Keys.Right);
        Key(on, Keys.Right);

        Assert.Null(off.GetPrivateField<object>("_popup"));
        Assert.NotNull(on.GetPrivateField<object>("_popup"));
    }

    [Fact]
    public void ThePopup_IsAsWideAsTheWiderEndOfTheRange_AndWorkedOutAgainWhenTheRangeOrTheFormatChanges()
    {
        using TrackBar bar = new TrackBar { Maximum = 9, ShowValuePopup = true };
        using Form host = Show(bar);

        Key(bar, Keys.Right);
        InfoPopupForm popup = bar.GetPrivateField<InfoPopupForm>("_popup")!;
        int narrow = popup.FixedSize.Width;

        // (The popup is not made again within a few milliseconds of the last time.)
        TestPictures.Pump(80);
        bar.Maximum = 100000;
        Key(bar, Keys.Right);
        int wide = popup.FixedSize.Width;

        Assert.True(wide > narrow, "the popup keeps the width of the old range (" + narrow + " px, then " + wide + " px)");

        TestPictures.Pump(80);
        bar.FormatValue = value => "volume level " + value;
        Key(bar, Keys.Right);
        Assert.True(popup.FixedSize.Width > wide);
    }

    private static int TextWidth(string text) =>
        TextRenderer.MeasureText(text, ErikwnkWFUI.Styles.UIFonts.Normal, new Size(short.MaxValue, short.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;

    [Fact]
    public void ThePopup_HasRoomForANumberWithDecimals_EvenThoughTheEndsOfTheRangeAreShort()
    {
        // A slider from 0 to 1 in the default format: the ends are "0" and "1", but a value in between is "0.37" or "0.88".
        using TrackBar bar = new TrackBar { ShowValuePopup = true };
        using Form host = Show(bar);

        // The popup is made while the value is 0 (the mouse comes over the slider), before any wider value was seen.
        bar.InvokePrivate("OnMouseEnter", EventArgs.Empty);

        InfoPopupForm popup = bar.GetPrivateField<InfoPopupForm>("_popup")!;
        string text = bar.FormatValue(0.85);
        Assert.True(popup.FixedSize.Width >= TextWidth(text) + 8, "the popup of " + popup.FixedSize.Width + " px cuts \"" + text + "\" (" + TextWidth(text) + " px)");
    }

    [Fact]
    public void ThePopup_GrowsToAValueWiderThanAllThatWereMeasured_AndKeepsTheSize()
    {
        // The range is measured in steps of a twentieth (0, 5, 10 ... 100): 37 is none of them.
        using TrackBar bar = new TrackBar { Maximum = 100, SmallChange = 37, ShowValuePopup = true };
        bar.FormatValue = value => Math.Abs(value - 37) < 0.001 ? "a value that is much wider than all the others" : "1";
        using Form host = Show(bar);

        // The popup is made while the value is 0 (the mouse comes over the slider); then the user moves it to 37.
        bar.InvokePrivate("OnMouseEnter", EventArgs.Empty);
        InfoPopupForm popup = bar.GetPrivateField<InfoPopupForm>("_popup")!;
        TestPictures.Pump(80);
        Key(bar, Keys.Right);

        int wide = TextWidth("a value that is much wider than all the others") + 8;
        Assert.True(popup.FixedSize.Width >= wide, "the popup of " + popup.FixedSize.Width + " px cuts the text (" + wide + " px)");

        TestPictures.Pump(80);
        Key(bar, Keys.Right);
        Assert.True(popup.FixedSize.Width >= wide, "the popup got narrower again");
    }
}

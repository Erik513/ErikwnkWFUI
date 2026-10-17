using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ProgressBars;

/// <summary>The tooltip with the whole text of a progress bar whose text is cut or moves.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ProgressBarToolTipTests : IDisposable
{
    private const string LongText = "Connecting to the server, checking the certificate and loading the first part of the data";

    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
    }

    public static IEnumerable<object[]> BothBars()
    {
        yield return new object[] { "ProgressBar" };
        yield return new object[] { "SlimProgressBar" };
    }

    private static Control Make(string which, string text, LabelScroll? scroll = null)
    {
        if (which == "ProgressBar")
        {
            ErikwnkWFUI.Controls.ProgressBar thick = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
            thick.Text = text;
            thick.ShowPercentage = true;
            thick.TextScroll = scroll!;

            return thick;
        }

        SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();
        slim.Text = text;
        slim.ShowPercentage = true;
        slim.TextScroll = scroll!;

        return slim;
    }

    private static Form Host(Control bar)
    {
        Form host = OffscreenForm.Create(new Size(300, 60), Color.Black);
        bar.SetBounds(0, 0, 300, 26);
        host.Controls.Add(bar);
        host.Show();
        Application.DoEvents();
        Repaint(bar);

        return host;
    }

    // The bar tells the tooltip while it paints.
    private static void Repaint(Control bar)
    {
        Application.DoEvents();
        using Bitmap picture = new Bitmap(bar.Width, bar.Height);
        bar.DrawToBitmap(picture, new Rectangle(Point.Empty, bar.Size));
    }

    private static FullTextToolTip Tip(Control bar) => bar.GetPrivateField<FullTextToolTip>("_fullTextToolTip")!;

    private static string Real(Control bar)
    {
        ToolTip? toolTip = Tip(bar).GetPrivateField<ToolTip>("_toolTip");

        return toolTip == null ? "" : toolTip.GetToolTip(bar) ?? "";
    }

    private static void SetShow(Control bar, bool value)
    {
        bar.GetType().GetProperty("ShowFullTextToolTip")!.SetValue(bar, value);
    }

    [Theory]
    [MemberData(nameof(BothBars))]
    public void ATextThatIsCut_HasATooltipWithAllOfIt_AndThePercentageIsNotInIt(string which)
    {
        Control bar = Make(which, LongText);
        using Form host = Host(bar);

        Assert.Equal(LongText, Tip(bar).Text);
        Assert.Equal(LongText, Real(bar));
    }

    [Theory]
    [MemberData(nameof(BothBars))]
    public void ATextThatMoves_HasATooltipWithAllOfIt(string which)
    {
        foreach (LabelScroll scroll in new[] { LabelScroll.BackAndForth(), LabelScroll.Endless() })
        {
            Control bar = Make(which, LongText, scroll);
            using Form host = Host(bar);

            Assert.Equal(LongText, Tip(bar).Text);
        }
    }

    [Theory]
    [MemberData(nameof(BothBars))]
    public void ATextThatIsShownWhole_OrNoText_HasNone(string which)
    {
        Control fits = Make(which, "Short");
        using Form fitsHost = Host(fits);
        Assert.Equal("", Tip(fits).Text);
        Assert.Equal("", Real(fits));

        Control none = Make(which, "");
        using Form noneHost = Host(none);
        Assert.Equal("", Tip(none).Text);
    }

    [Theory]
    [MemberData(nameof(BothBars))]
    public void TheTooltip_FollowsTheText_AndTheSwitch(string which)
    {
        Control bar = Make(which, LongText);
        using Form host = Host(bar);
        Assert.Equal(LongText, Tip(bar).Text);

        bar.Text = "Short";
        Repaint(bar);
        Assert.Equal("", Tip(bar).Text);

        bar.Text = LongText;
        Repaint(bar);
        Assert.Equal(LongText, Tip(bar).Text);

        SetShow(bar, false);
        Assert.Equal("", Tip(bar).Text);
        Repaint(bar);
        Assert.Equal("", Tip(bar).Text);
        Assert.Equal("", Real(bar));

        SetShow(bar, true);
        Repaint(bar);
        Assert.Equal(LongText, Tip(bar).Text);
    }

    [Theory]
    [MemberData(nameof(BothBars))]
    public void ALongText_IsWrappedInTheTooltipWindow_AtTheFixedWidth(string which)
    {
        string veryLong = string.Join(" ", Enumerable.Repeat("a long text for a tooltip", 30));
        Control bar = Make(which, veryLong);
        using Form host = Host(bar);
        ToolTip toolTip = Tip(bar).GetPrivateField<ToolTip>("_toolTip")!;
        System.Reflection.PropertyInfo handle = typeof(ToolTip).GetProperty("Handle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!;

        toolTip.Show(veryLong, bar, 5, 30, 1000);
        Application.DoEvents();
        IntPtr window = (IntPtr)handle.GetValue(toolTip)!;
        int width = (int)ErikwnkWFUI.Native.NativeMethods.SendMessage(window, ErikwnkWFUI.Native.NativeMethods.TTM_GETMAXTIPWIDTH, IntPtr.Zero, IntPtr.Zero);
        toolTip.Hide(bar);

        Assert.Equal((int)Math.Round(ToolTipWidth.MaxWidthAt96Dpi * bar.DeviceDpi / 96.0), width);
    }

    [Theory]
    [MemberData(nameof(BothBars))]
    public void TheTooltip_IsOnByDefault(string which)
    {
        Control bar = Make(which, "x");

        Assert.True((bool)bar.GetType().GetProperty("ShowFullTextToolTip")!.GetValue(bar)!);
    }
}

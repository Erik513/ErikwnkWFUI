using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Tests.Controls.Labels;

/// <summary>The tooltip with the whole text of a label whose text moves because it does not fit.</summary>
[Collection(AccentColorTestCollection.Name)]
public class LabelFullTextToolTipTests : IDisposable
{
    private const string LongText = "A text that is much longer than the label it is shown in, so it has to move";

    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private ThemedLabel Create(string text, LabelScroll scroll)
    {
        ThemedLabel label = UIStyles.Labels.CreateStandard(text);
        label.Size = new Size(120, 24);
        label.Scroll = scroll;

        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            ClientSize = new Size(200, 60)
        };
        label.Location = new Point(5, 5);
        form.Controls.Add(label);
        form.Show();
        _forms.Add(form);
        Application.DoEvents();

        return label;
    }

    private static string Shown(ThemedLabel label)
    {
        FullTextToolTip tip = label.GetPrivateField<FullTextToolTip>("_fullTextToolTip")!;

        return tip.Text;
    }

    // What the tooltip window of the label really has for it (not only what the label told it).
    private static string Real(ThemedLabel label)
    {
        ToolTip? toolTip = label.GetPrivateField<FullTextToolTip>("_fullTextToolTip")!.GetPrivateField<ToolTip>("_toolTip");

        return toolTip == null ? "" : toolTip.GetToolTip(label) ?? "";
    }

    [Fact]
    public void AMovingTextThatDoesNotFit_HasATooltipWithAllOfIt()
    {
        foreach (LabelScroll scroll in new[] { LabelScroll.BackAndForth(), LabelScroll.Endless() })
        {
            ThemedLabel label = Create(LongText, scroll);

            Assert.Equal(LongText, Shown(label));
            Assert.Equal(LongText, Real(label));
        }
    }

    [Fact]
    public void ATextThatFits_OrThatDoesNotMove_HasNone()
    {
        Assert.Equal("", Shown(Create("Short", LabelScroll.BackAndForth())));
        Assert.Equal("", Shown(Create("Short", LabelScroll.Endless())));

        // Not moving: a standard label with an ellipsis has a tooltip of its own.
        Assert.Equal("", Shown(Create(LongText, LabelScroll.None)));
    }

    [Fact]
    public void TheTooltip_FollowsTheTextAndTheSize()
    {
        ThemedLabel label = Create(LongText, LabelScroll.BackAndForth());
        Assert.Equal(LongText, Shown(label));

        label.Text = "Short";
        Assert.Equal("", Shown(label));
        Assert.Equal("", Real(label));

        label.Text = LongText + " and more";
        Assert.Equal(LongText + " and more", Shown(label));

        label.Width = 600;
        Application.DoEvents();
        Assert.Equal("", Shown(label));
    }

    [Fact]
    public void ADisabledLabel_HasNone_AndTheTooltipComesBackWhenItIsEnabled()
    {
        ThemedLabel label = Create(LongText, LabelScroll.BackAndForth());

        label.Enabled = false;
        Assert.Equal("", Shown(label));

        label.Enabled = true;
        Assert.Equal(LongText, Shown(label));
    }

    [Fact]
    public void TheTooltip_CanBeSwitchedOff_AndOnAgain()
    {
        ThemedLabel label = Create(LongText, LabelScroll.BackAndForth());
        Assert.True(label.ShowFullTextToolTip);

        label.ShowFullTextToolTip = false;
        Assert.Equal("", Shown(label));
        Assert.Equal("", Real(label));

        label.ShowFullTextToolTip = true;
        Assert.Equal(LongText, Shown(label));
    }

    [Fact]
    public void SeveralTexts_HaveATooltipWithAllOfThem()
    {
        ThemedLabel label = Create("", LabelScroll.Endless(new[] { "First message of the banner", "Second message of the banner", "Third message" }));

        Assert.Equal(label.Text, Shown(label));
        Assert.Contains("Second message", Shown(label));
    }

    [Fact]
    public void ALongTooltipText_IsWrappedAtAFixedWidth_EachTimeItIsShown()
    {
        string veryLong = string.Join(" ", Enumerable.Repeat("a long text for a tooltip", 30));
        ThemedLabel label = Create(veryLong, LabelScroll.BackAndForth());
        ToolTip toolTip = label.GetPrivateField<FullTextToolTip>("_fullTextToolTip")!.GetPrivateField<ToolTip>("_toolTip")!;
        System.Reflection.PropertyInfo handleProperty = typeof(ToolTip).GetProperty("Handle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!;
        int expected = (int)Math.Round(ToolTipWidth.MaxWidthAt96Dpi * label.DeviceDpi / 96.0);

        for (int round = 0; round < 2; round++)
        {
            toolTip.Show(veryLong, label, 5, 30, 1000);
            Application.DoEvents();

            IntPtr window = (IntPtr)handleProperty.GetValue(toolTip)!;
            int width = (int)SendMessage(window, TTM_GETMAXTIPWIDTH, IntPtr.Zero, IntPtr.Zero);
            toolTip.Hide(label);

            Assert.Equal(expected, width);

            // And the window really is that narrow (with its border), the first time too.
            System.Drawing.Rectangle bounds = GetWindowBounds(window);
            Assert.True(bounds.Width <= expected + 20, "the window is " + bounds.Width + " px wide in round " + round);
            Assert.True(bounds.Height > 30, "the text is not wrapped: height " + bounds.Height);

            // The window is made again when the form is activated (the library switches the tooltip off and on then).
            Form form = label.FindForm()!;
            form.GetType().GetMethod("OnActivated", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(form, new object[] { EventArgs.Empty });
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out RectStruct rect);

    private struct RectStruct
    {
        public int Left, Top, Right, Bottom;
    }

    private static System.Drawing.Rectangle GetWindowBounds(IntPtr window)
    {
        GetWindowRect(window, out RectStruct r);

        return new System.Drawing.Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
    }

    [Fact]
    public void ATooltipThatIsMadeAfterTheFormWasShown_IsLimitedFromTheStart()
    {
        // The form has been activated before the label has a tooltip: nothing sets the width but the tooltip itself.
        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            ClientSize = new Size(200, 60)
        };
        _forms.Add(form);
        form.Show();
        Application.DoEvents();

        string veryLong = string.Join(" ", Enumerable.Repeat("a long text for a tooltip", 30));
        ThemedLabel label = UIStyles.Labels.CreateStandard("x");
        label.Size = new Size(120, 24);
        label.Location = new Point(5, 5);
        form.Controls.Add(label);
        label.Text = veryLong;
        label.Scroll = LabelScroll.BackAndForth();
        Application.DoEvents();

        ToolTip toolTip = label.GetPrivateField<FullTextToolTip>("_fullTextToolTip")!.GetPrivateField<ToolTip>("_toolTip")!;
        System.Reflection.PropertyInfo handleProperty = typeof(ToolTip).GetProperty("Handle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!;

        toolTip.Show(veryLong, label, 5, 30, 1000);
        Application.DoEvents();
        System.Drawing.Rectangle bounds = GetWindowBounds((IntPtr)handleProperty.GetValue(toolTip)!);
        toolTip.Hide(label);

        Assert.True(bounds.Width <= (int)Math.Round(ToolTipWidth.MaxWidthAt96Dpi * label.DeviceDpi / 96.0) + 20, "the window is " + bounds.Width + " px wide");
    }

    [Fact]
    public void ALabelThatIsDisposed_TakesItsTooltipWithIt()
    {
        ThemedLabel label = Create(LongText, LabelScroll.BackAndForth());

        label.Dispose();

        Assert.Equal("", Shown(label));
    }
}

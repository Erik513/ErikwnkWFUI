using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.CheckBox;

/// <summary>
/// What the checkbox does: toggling, the events and their order, AutoCheck, the third state,
/// the keyboard, a handler that interferes, text and size edge cases, and the standard
/// properties the library draws its own way and deliberately ignores (CheckAlign, TextAlign,
/// RightToLeft, Appearance). The states of the look are in CheckBoxStateTests.
/// [Collection]: the default colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class CheckBoxBehaviorTests : IDisposable
{
    // The windows the boxes live in (a real click needs a shown window).
    private readonly List<Form> _hosts = new List<Form>();

    public void Dispose()
    {
        foreach (Form host in _hosts)
            host.Dispose();
    }

    private void Show(System.Windows.Forms.CheckBox box)
    {
        Form host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-4000, -4000),
            ShowInTaskbar = false,
            ClientSize = new Size(300, 60),
            BackColor = UIColors.BackgroundMedium
        };

        box.Location = new Point(10, 10);
        host.Controls.Add(box);
        host.Show();
        _hosts.Add(host);

        for (int i = 0; i < 3; i++)
        {
            Application.DoEvents();
            Thread.Sleep(30);
        }
    }

    private System.Windows.Forms.CheckBox Create(bool isChecked, string text = "Label")
    {
        System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard(text, isChecked);
        Show(box);

        return box;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    // A real click as Windows delivers it (a disabled control gets none of it).
    private static void Click(Control control)
    {
        IntPtr at = (IntPtr)(8 | (8 << 16));
        SendMessage(control.Handle, 0x201, (IntPtr)1, at);
        SendMessage(control.Handle, 0x202, IntPtr.Zero, at);
        Application.DoEvents();
    }

    private static void PressSpace(Control control)
    {
        SendMessage(control.Handle, 0x100, (IntPtr)0x20, IntPtr.Zero);
        SendMessage(control.Handle, 0x101, (IntPtr)0x20, IntPtr.Zero);
    }

    private static Bitmap Draw(Control control)
    {
        _ = control.Handle;
        Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));

        return bitmap;
    }

    private static bool SamePixels(Bitmap first, Bitmap second)
    {
        if (first.Size != second.Size)
            return false;

        for (int y = 0; y < first.Height; y++)
        {
            for (int x = 0; x < first.Width; x++)
            {
                if (first.GetPixel(x, y) != second.GetPixel(x, y))
                    return false;
            }
        }

        return true;
    }

    // ---- toggling and the events ----

    [Fact]
    public void AClick_TogglesTheBox_AndRaisesTheEventsOnceEach_TheToggleBeforeTheClick()
    {
        using System.Windows.Forms.CheckBox box = Create(false);
        List<string> events = new List<string>();
        box.CheckedChanged += delegate { events.Add("CheckedChanged:" + box.Checked); };
        box.CheckStateChanged += delegate { events.Add("CheckStateChanged:" + box.CheckState); };
        box.Click += delegate { events.Add("Click:" + box.Checked); };

        Click(box);

        Assert.True(box.Checked);
        Assert.Equal(
            new[] { "CheckedChanged:True", "CheckStateChanged:Checked", "Click:True" },
            events.ToArray());
    }

    [Fact]
    public void ClickingTwice_BringsItBack()
    {
        using System.Windows.Forms.CheckBox box = Create(false);

        Click(box);
        Click(box);

        Assert.False(box.Checked);
    }

    [Fact]
    public void WithoutAutoCheck_AClickDoesNotToggle_ButStillRaisesClick()
    {
        using System.Windows.Forms.CheckBox box = Create(false);
        box.AutoCheck = false;
        int clicks = 0;
        int changes = 0;
        box.Click += delegate { clicks++; };
        box.CheckedChanged += delegate { changes++; };

        Click(box);

        Assert.False(box.Checked);
        Assert.Equal(1, clicks);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void ADisabledBox_DoesNotToggle()
    {
        using System.Windows.Forms.CheckBox box = Create(false);
        box.Enabled = false;
        int clicks = 0;
        box.Click += delegate { clicks++; };

        Click(box);

        Assert.False(box.Checked);
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void SettingTheSameValue_RaisesNothing()
    {
        using System.Windows.Forms.CheckBox box = Create(true);
        int changes = 0;
        box.CheckedChanged += delegate { changes++; };
        box.CheckStateChanged += delegate { changes++; };

        box.Checked = true;
        box.CheckState = CheckState.Checked;

        Assert.Equal(0, changes);
    }

    [Fact]
    public void TheThirdState_IsPartOfTheCycle_OnlyWithThreeState()
    {
        using System.Windows.Forms.CheckBox two = Create(false);
        List<CheckState> twoStates = new List<CheckState>();

        for (int i = 0; i < 4; i++)
        {
            Click(two);
            twoStates.Add(two.CheckState);
        }

        Assert.Equal(new[] { CheckState.Checked, CheckState.Unchecked, CheckState.Checked, CheckState.Unchecked }, twoStates.ToArray());

        using System.Windows.Forms.CheckBox three = Create(false);
        three.ThreeState = true;
        List<CheckState> threeStates = new List<CheckState>();

        for (int i = 0; i < 4; i++)
        {
            Click(three);
            threeStates.Add(three.CheckState);
        }

        Assert.Equal(new[] { CheckState.Checked, CheckState.Indeterminate, CheckState.Unchecked, CheckState.Checked }, threeStates.ToArray());
    }

    [Fact]
    public void AHandlerThatTakesTheChangeBack_EndsInAStableState_WithoutLooping()
    {
        using System.Windows.Forms.CheckBox box = Create(false);
        int changes = 0;

        box.CheckedChanged += delegate
        {
            changes++;

            if (box.Checked)
                box.Checked = false;
        };

        Click(box);

        Assert.False(box.Checked);
        Assert.Equal(2, changes);
    }

    // ---- the keyboard ----

    [Fact]
    public void TheSpaceKey_Toggles_OtherKeysDoNot()
    {
        using System.Windows.Forms.CheckBox box = Create(false);

        SendMessage(box.Handle, 0x100, (IntPtr)0x0D, IntPtr.Zero);
        SendMessage(box.Handle, 0x101, (IntPtr)0x0D, IntPtr.Zero);
        Assert.False(box.Checked);

        PressSpace(box);
        Assert.True(box.Checked);

        // The mnemonic (Alt + letter) is the other way to toggle by keyboard.
        using System.Windows.Forms.CheckBox mnemonic = Create(false, "&Remember");
        bool handled = mnemonic.InvokePrivate<bool>("ProcessMnemonic", 'r');

        Assert.True(handled);
        Assert.True(mnemonic.Checked);
    }

    // ---- the compact ones ----

    [Fact]
    public void TheCompactOnes_AreTheSameBoxWithoutALabel_StandardGrayAndPrimaryAccent_AndTakeASet()
    {
        using System.Windows.Forms.CheckBox standard = UIStyles.CheckBoxes.CreateStandardCompact(true);
        using System.Windows.Forms.CheckBox primary = UIStyles.CheckBoxes.CreatePrimaryCompact(true);
        using System.Windows.Forms.CheckBox colored = UIStyles.CheckBoxes.CreateStandardCompact(false, UIStyles.Colors.Sets.Green);

        Assert.Equal("", standard.Text);
        Assert.Equal(new Size(25, 25), standard.Size);
        Assert.Equal(new Size(25, 25), primary.Size);
        Assert.True(standard.Checked);
        Assert.False(colored.Checked);

        ErikwnkWFUI.Controls.CheckBox standardBox = (ErikwnkWFUI.Controls.CheckBox)standard;
        ErikwnkWFUI.Controls.CheckBox primaryBox = (ErikwnkWFUI.Controls.CheckBox)primary;
        ErikwnkWFUI.Controls.CheckBox coloredBox = (ErikwnkWFUI.Controls.CheckBox)colored;

        Assert.Null(standardBox.Colors);
        Assert.Equal(UIStyles.Colors.Sets.Primary.Basic, primaryBox.Colors!.Basic);
        Assert.Same(UIStyles.Colors.Sets.Green, coloredBox.Colors);
    }

    // ---- text and size ----

    [Fact]
    public void ATextThatIsNullOrEmpty_GivesJustTheBox()
    {
        using System.Windows.Forms.CheckBox empty = Create(true, "");
        using System.Windows.Forms.CheckBox missing = UIStyles.CheckBoxes.CreateStandard(null!, true);

        Assert.Equal("", missing.Text);
        Assert.Equal(empty.Size, missing.Size);
        Assert.True(empty.Width <= 24, "wider than a box: " + empty.Width);

        using Bitmap picture = Draw(empty);
        Assert.Equal(empty.Size, picture.Size);
    }

    [Fact]
    public void ThePreferredSize_GrowsWithTheTextAndTheFont()
    {
        System.Drawing.Font normal = UIFonts.Normal;
        using System.Drawing.Font large = new System.Drawing.Font(normal.FontFamily, normal.Size * 2);

        Size none = ErikwnkWFUI.Controls.CheckBox.ComputePreferredSize("", normal);
        Size shortText = ErikwnkWFUI.Controls.CheckBox.ComputePreferredSize("Ok", normal);
        Size longText = ErikwnkWFUI.Controls.CheckBox.ComputePreferredSize("A much longer label", normal);
        Size largeFont = ErikwnkWFUI.Controls.CheckBox.ComputePreferredSize("Ok", large);

        Assert.True(none.Width < shortText.Width);
        Assert.True(shortText.Width < longText.Width);
        Assert.True(largeFont.Height > shortText.Height);
        Assert.True(none.Height >= 24);
    }

    [Fact]
    public void ALabelLongerThanTheBox_AndATinyOrEmptyControl_DrawWithoutAFuss()
    {
        using System.Windows.Forms.CheckBox clipped = Create(true, "A label that does not fit into the width at all");
        clipped.Width = 40;
        using Bitmap clippedPicture = Draw(clipped);
        Assert.Equal(new Size(40, clipped.Height), clippedPicture.Size);

        using System.Windows.Forms.CheckBox tiny = Create(true);
        tiny.Size = new Size(6, 6);
        using Bitmap tinyPicture = Draw(tiny);
        Assert.Equal(new Size(6, 6), tinyPicture.Size);

        using System.Windows.Forms.CheckBox none = Create(true);
        none.Size = Size.Empty;
        Exception? thrown = Record.Exception(() => none.Invalidate());
        Assert.Null(thrown);
    }

    [Fact]
    public void TheText_StartsJustAfterTheBox_NeverOverIt_AndNeverFarFromIt()
    {
        // The box takes x 2 to 18; the text follows after a gap, left-aligned.
        using System.Windows.Forms.CheckBox box = Create(true, "Label");
        using Bitmap picture = Draw(box);
        Color background = picture.GetPixel(picture.Width - 1, 2);
        int firstTextColumn = -1;

        for (int x = 20; x < picture.Width && firstTextColumn < 0; x++)
        {
            for (int y = 0; y < picture.Height; y++)
            {
                if (picture.GetPixel(x, y) != background)
                {
                    firstTextColumn = x;
                    break;
                }
            }
        }

        Assert.True(firstTextColumn >= 24, "the text starts too close to the box, at " + firstTextColumn);
        Assert.True(firstTextColumn <= 30, "the text starts too far from the box, at " + firstTextColumn);
    }

    [Fact]
    public void ATransparentParent_ShowsTheColorBehindIt_NotAHole()
    {
        using Form form = new Form { BackColor = Color.FromArgb(10, 40, 70), Size = new Size(300, 100) };
        Panel transparent = new Panel { BackColor = Color.Transparent, Size = new Size(200, 40) };
        System.Windows.Forms.CheckBox box = Create(false);
        form.Controls.Add(transparent);
        transparent.Controls.Add(box);

        using Bitmap picture = Draw(box);

        Assert.Equal(form.BackColor, picture.GetPixel(picture.Width - 1, 1));
    }

    [Fact]
    public void TheSizesGrowWithTheScreensScaling_AndStayTheOldOnesAtNormalScaling()
    {
        ErikwnkWFUI.Controls.CheckBox.Metrics normal = new ErikwnkWFUI.Controls.CheckBox.Metrics(96);
        ErikwnkWFUI.Controls.CheckBox.Metrics large = new ErikwnkWFUI.Controls.CheckBox.Metrics(144);
        ErikwnkWFUI.Controls.CheckBox.Metrics huge = new ErikwnkWFUI.Controls.CheckBox.Metrics(192);
        ErikwnkWFUI.Controls.CheckBox.Metrics tooLow = new ErikwnkWFUI.Controls.CheckBox.Metrics(0);

        Assert.Equal(16, normal.BoxSize);
        Assert.Equal(2, normal.BoxLeftMargin);
        Assert.Equal(24, normal.GlyphColumnWidth);
        Assert.Equal(24, large.BoxSize);
        Assert.Equal(32, huge.BoxSize);
        Assert.True(large.GlyphColumnWidth > normal.GlyphColumnWidth && huge.GlyphColumnWidth > large.GlyphColumnWidth);
        Assert.Equal(normal.BoxSize, tooLow.BoxSize);

        System.Drawing.Font font = UIFonts.Normal;
        Assert.True(ErikwnkWFUI.Controls.CheckBox.ComputePreferredSize("Ok", font, 192).Width
            > ErikwnkWFUI.Controls.CheckBox.ComputePreferredSize("Ok", font, 96).Width);
        Assert.True(ErikwnkWFUI.Controls.CheckBox.ComputePreferredSize("Ok", font, 192).Height
            > ErikwnkWFUI.Controls.CheckBox.ComputePreferredSize("Ok", font, 96).Height);
    }

    // ---- what the library draws its own way: these properties are ignored ----

    [Fact]
    public void CheckAlign_TextAlign_RightToLeft_AndAppearance_AreIgnored_TheBoxIsAlwaysOnTheLeft()
    {
        using System.Windows.Forms.CheckBox box = Create(true);
        using Bitmap before = Draw(box);

        box.CheckAlign = ContentAlignment.MiddleRight;
        box.TextAlign = ContentAlignment.BottomRight;
        box.RightToLeft = RightToLeft.Yes;
        box.Appearance = Appearance.Button;

        using Bitmap after = Draw(box);

        Assert.True(SamePixels(before, after), "one of the ignored properties changed the picture");
    }

    [Fact]
    public void TheLook_SurvivesTheHandleBeingRecreated()
    {
        using System.Windows.Forms.CheckBox box = Create(true);
        using Bitmap before = Draw(box);

        box.InvokePrivate("RecreateHandle");

        using Bitmap after = Draw(box);
        Assert.True(SamePixels(before, after));
        Assert.True(box.Checked);
    }

    [Fact]
    public void ABoxInsideAParent_IsEmbeddedInTheParentsBackColor()
    {
        using Panel parent = new Panel { Size = new Size(200, 40), BackColor = Color.FromArgb(10, 40, 70) };
        System.Windows.Forms.CheckBox box = Create(false);
        parent.Controls.Add(box);

        using Bitmap picture = Draw(box);

        Assert.Equal(parent.BackColor, picture.GetPixel(picture.Width - 1, 1));
    }
}

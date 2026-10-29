using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.RadioButtons;

/// <summary>What the radio button does: selecting, the events, AutoCheck, the keyboard, the group of the parent, handlers that interfere, edge cases of text and size.</summary>
[Collection(AccentColorTestCollection.Name)]
public class RadioButtonBehaviorTests : IDisposable
{
    private readonly List<Form> _hosts = new List<Form>();

    public void Dispose()
    {
        foreach (Form host in _hosts)
            host.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private Form NewHost()
    {
        Form host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-4000, -4000),
            ShowInTaskbar = false,
            ClientSize = new Size(300, 120),
            BackColor = UIColors.BackgroundMedium
        };
        _hosts.Add(host);

        return host;
    }

    private System.Windows.Forms.RadioButton[] Shown(params System.Windows.Forms.RadioButton[] buttons)
    {
        Form host = NewHost();

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].Location = new Point(10, 10 + i * 30);
            host.Controls.Add(buttons[i]);
        }

        host.Show();
        Application.DoEvents();

        return buttons;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    private static void Click(Control control)
    {
        IntPtr at = (IntPtr)(8 | (8 << 16));
        SendMessage(control.Handle, 0x201, (IntPtr)1, at);
        SendMessage(control.Handle, 0x202, IntPtr.Zero, at);
        Application.DoEvents();
    }

    private static System.Windows.Forms.RadioButton Make(string text, bool isChecked = false)
    {
        return UIStyles.RadioButtons.CreateStandard(text, isChecked);
    }

    // ---- selecting and the events ----

    [Fact]
    public void AClick_SelectsTheButton_AndClearsTheOthersOfTheParent()
    {
        var radios = Shown(Make("A", true), Make("B"), Make("C"));

        Click(radios[1]);

        Assert.False(radios[0].Checked);
        Assert.True(radios[1].Checked);
        Assert.False(radios[2].Checked);
    }

    [Fact]
    public void AClickOnTheSelectedButton_ChangesNothing_AndRaisesNoCheckedChanged()
    {
        var radios = Shown(Make("A", true), Make("B"));
        int changed = 0, clicked = 0;
        radios[0].CheckedChanged += (s, e) => changed++;
        radios[0].Click += (s, e) => clicked++;

        Click(radios[0]);

        Assert.True(radios[0].Checked);
        Assert.Equal(0, changed);
        Assert.Equal(1, clicked);
    }

    [Fact]
    public void Selecting_RaisesCheckedChangedOnBoth_TheOldOneFirstOrNot_ButOnceEach()
    {
        var radios = Shown(Make("A", true), Make("B"));
        int a = 0, b = 0;
        radios[0].CheckedChanged += (s, e) => a++;
        radios[1].CheckedChanged += (s, e) => b++;

        Click(radios[1]);

        Assert.Equal(1, a);
        Assert.Equal(1, b);
    }

    [Fact]
    public void WithAutoCheckOff_AClickDoesNotSelect_ButIsStillAClick()
    {
        // (The standard button is the measure: whatever it does with a click while AutoCheck is off, this one does as well.)
        var radios = Shown(Make("A", true), Make("B"), new System.Windows.Forms.RadioButton { Text = "Standard", AutoSize = true });
        radios[1].AutoCheck = false;
        radios[2].AutoCheck = false;
        int clicked = 0, standardClicked = 0;
        radios[1].Click += (s, e) => clicked++;
        radios[2].Click += (s, e) => standardClicked++;

        Click(radios[1]);
        Click(radios[2]);

        Assert.False(radios[1].Checked);
        Assert.True(radios[0].Checked);
        Assert.True(clicked >= 1);
        Assert.Equal(standardClicked, clicked);

        radios[1].Checked = true;
        Assert.True(radios[1].Checked);
    }

    [Fact]
    public void ADisabledButton_GetsNoClick()
    {
        var radios = Shown(Make("A", true), Make("B"));
        radios[1].Enabled = false;

        Click(radios[1]);

        Assert.False(radios[1].Checked);
    }

    [Fact]
    public void TheMnemonic_SelectsTheButton()
    {
        var radios = Shown(Make("&Alpha", true), Make("&Beta"));

        radios[1].InvokePrivate("ProcessMnemonic", 'b');

        Assert.True(radios[1].Checked);
        Assert.False(radios[0].Checked);
    }

    [Fact]
    public void OnlyTheSelectedButtonOfAParentIsATabStop()
    {
        var radios = Shown(Make("A"), Make("B", true), Make("C"));

        Assert.False(radios[0].TabStop);
        Assert.True(radios[1].TabStop);
        Assert.False(radios[2].TabStop);

        Click(radios[2]);

        Assert.False(radios[1].TabStop);
        Assert.True(radios[2].TabStop);
    }

    [Fact]
    public void TheFactory_SelectsOnlyWhenAskedTo_AndStartsEmptyByDefault()
    {
        using System.Windows.Forms.RadioButton plain = UIStyles.RadioButtons.CreateStandard("A");
        using System.Windows.Forms.RadioButton on = UIStyles.RadioButtons.CreatePrimary("A", true);

        Assert.False(plain.Checked);
        Assert.True(on.Checked);
    }

    // ---- a handler that interferes ----

    [Fact]
    public void AHandlerThatSelectsAnotherButton_EndsWithOneSelected()
    {
        var radios = Shown(Make("A", true), Make("B"), Make("C"));
        radios[1].CheckedChanged += (s, e) =>
        {
            if (radios[1].Checked)
                radios[2].Checked = true;
        };

        Click(radios[1]);

        Assert.Single(radios, radio => radio.Checked);
    }

    [Fact]
    public void AHandlerThatDisablesTheButton_LeavesItNotLookingPressedOrHovered()
    {
        var radios = Shown(Make("A", true), Make("B"));
        radios[1].CheckedChanged += (s, e) => radios[1].Enabled = false;

        Click(radios[1]);

        Assert.True(radios[1].Checked);
        Assert.False(radios[1].GetPrivateField<bool>("_isPressed"));
        Assert.False(radios[1].GetPrivateField<bool>("_isHot"));
    }

    [Fact]
    public void AHandlerThatRemovesTheButton_DoesNotThrow()
    {
        var radios = Shown(Make("A", true), Make("B"));
        radios[1].CheckedChanged += (s, e) => radios[1].Parent?.Controls.Remove(radios[1]);

        Click(radios[1]);

        Assert.True(radios[1].Checked);
    }

    // ---- states that must not stick ----

    [Fact]
    public void Hover_IsClearedWhenTheButtonIsHidden_OrLosesTheMouse()
    {
        var radios = Shown(Make("A"));
        radios[0].InvokePrivate("OnMouseEnter", EventArgs.Empty);
        Assert.True(radios[0].GetPrivateField<bool>("_isHot"));

        radios[0].Visible = false;
        Assert.False(radios[0].GetPrivateField<bool>("_isHot"));

        radios[0].Visible = true;
        radios[0].InvokePrivate("OnMouseEnter", EventArgs.Empty);
        radios[0].InvokePrivate("OnMouseLeave", EventArgs.Empty);
        Assert.False(radios[0].GetPrivateField<bool>("_isHot"));
    }

    [Fact]
    public void ThePress_IsClearedWhenTheMouseIsTakenAway()
    {
        var radios = Shown(Make("A"));
        radios[0].InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        Assert.True(radios[0].GetPrivateField<bool>("_isPressed"));

        radios[0].InvokePrivate("OnMouseCaptureChanged", EventArgs.Empty);

        Assert.False(radios[0].GetPrivateField<bool>("_isPressed"));
    }

    [Fact]
    public void ARightClick_DoesNotLookPressed()
    {
        var radios = Shown(Make("A"));

        radios[0].InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Right, 1, 5, 5, 0));

        Assert.False(radios[0].GetPrivateField<bool>("_isPressed"));
    }

    // ---- text and size edge cases ----

    [Fact]
    public void NoText_ANullText_AVeryLongText_AndNoSize_AllPaint()
    {
        var radios = Shown(Make(""), Make("x"), Make(new string('W', 400)));
        radios[1].Text = null;

        foreach (var radio in radios)
        {
            using Bitmap picture = Draw(radio);
            Assert.Equal(radio.Size, picture.Size);
        }

        radios[0].Size = new Size(0, 0);
        radios[0].Invalidate();
        radios[0].Size = new Size(1, 1);
        using Bitmap tiny = Draw(radios[0]);
    }

    [Fact]
    public void ANewButton_PaintsWithoutAParent()
    {
        using ErikwnkWFUI.Controls.RadioButton radio = new ErikwnkWFUI.Controls.RadioButton { Text = "A", Size = new Size(100, 24) };
        using Bitmap picture = new Bitmap(100, 24);

        radio.DrawToBitmap(picture, new Rectangle(0, 0, 100, 24));
    }

    [Fact]
    public void DisposingTwice_AndPaintingAfterwards_DoNotThrow()
    {
        var radios = Shown(Make("A", true));
        radios[0].Dispose();
        radios[0].Dispose();
    }

    [Fact]
    public void TheIgnoredProperties_DoNotChangeThePicture()
    {
        var radios = Shown(Make("Label", true));
        using Bitmap before = Draw(radios[0]);
        using Bitmap image = new Bitmap(10, 10);

        radios[0].TextAlign = ContentAlignment.BottomRight;
        radios[0].RightToLeft = RightToLeft.Yes;
        radios[0].Appearance = Appearance.Button;
        radios[0].Image = image;
        radios[0].FlatStyle = FlatStyle.Standard;
        radios[0].AutoEllipsis = true;
        radios[0].Padding = new Padding(10);
        radios[0].UseVisualStyleBackColor = true;

        using Bitmap after = Draw(radios[0]);
        Assert.True(SamePixels(before, after));
    }

    [Fact]
    public void AThemeChange_RepaintsTheButton()
    {
        var radios = Shown(Make("Label", true));
        UIColors.ApplyTheme(UIThemes.Dark);
        using Bitmap dark = Draw(radios[0]);

        UIColors.ApplyTheme(UIThemes.Light);
        using Bitmap light = Draw(radios[0]);

        Assert.False(SamePixels(dark, light));
    }

    [Fact]
    public void ASetGivenLater_ColorsTheSelectedBox_AndNullGoesBackToGray()
    {
        var radios = Shown(Make("A", true));
        using Bitmap gray = Draw(radios[0]);

        ((ErikwnkWFUI.Controls.RadioButton)radios[0]).Colors = UIStyles.Colors.Sets.Green;
        using Bitmap green = Draw(radios[0]);
        Assert.False(SamePixels(gray, green));

        ((ErikwnkWFUI.Controls.RadioButton)radios[0]).Colors = null;
        using Bitmap back = Draw(radios[0]);
        Assert.True(SamePixels(gray, back));
    }

    [Fact]
    public void ThePrimaryButton_FollowsTheAccent()
    {
        var radios = Shown(UIStyles.RadioButtons.CreatePrimary("A", true));

        UIColors.SetAccent(UIAccentColors.Red);
        try
        {
            using Bitmap red = Draw(radios[0]);
            UIColors.SetAccent(UIAccentColors.Blue);
            using Bitmap blue = Draw(radios[0]);

            Assert.False(SamePixels(red, blue));
        }
        finally
        {
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }
}
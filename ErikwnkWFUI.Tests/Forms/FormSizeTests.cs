using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;
using MessageBoxButtons = ErikwnkWFUI.Forms.MessageBoxButtons;
using MessageBoxIcon = ErikwnkWFUI.Forms.MessageBoxIcon;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The three sizes (or an own size) that every window and dialog of the library can have.</summary>
[Collection(AccentColorTestCollection.Name)]
public class FormSizeTests : IDisposable
{
    private readonly AppFormHost _host = new AppFormHost();

    public void Dispose()
    {
        _host.Dispose();
    }

    private static Size Scaled(Control control, Size size96)
    {
        return new Size(Dpi.Scale(size96.Width, control.DeviceDpi), Dpi.Scale(size96.Height, control.DeviceDpi));
    }

    // ---- the option

    [Fact]
    public void EachKindOfWindow_HasThreeSizes_ThatGrow()
    {
        foreach (FormKind kind in Enum.GetValues(typeof(FormKind)))
        {
            Size small = ((FormSizeOption)FormSize.Small).Resolve(kind);
            Size medium = ((FormSizeOption)FormSize.Medium).Resolve(kind);
            Size large = ((FormSizeOption)FormSize.Large).Resolve(kind);

            Assert.True(small.Width < medium.Width && medium.Width < large.Width, kind + ": the widths grow");

            // (A dialog whose height follows its content has none in the table.)
            if (small.Height > 0)
                Assert.True(small.Height < medium.Height && medium.Height < large.Height, kind + ": the heights grow");
        }
    }

    [Fact]
    public void ADialogIsSmallerThanAWindow_AndAProgressDialogIsWiderThanAnInputBox()
    {
        foreach (FormSize size in new[] { FormSize.Small, FormSize.Medium, FormSize.Large })
        {
            FormSizeOption option = size;

            Assert.True(option.Resolve(FormKind.Dialog).Width < option.Resolve(FormKind.Window).Width);
            Assert.True(option.Resolve(FormKind.Dialog).Height < option.Resolve(FormKind.Window).Height);
            Assert.True(option.Resolve(FormKind.Progress).Width > option.Resolve(FormKind.Input).Width);
        }
    }

    [Fact]
    public void AnOptionConvertsFromASizeOrAnOwnSize_AndKnowsWhichItIs()
    {
        FormSizeOption preset = FormSize.Large;
        FormSizeOption own = new Size(500, 300);

        Assert.Null(preset.Custom);
        Assert.Equal(FormSize.Large, preset.Preset);
        Assert.Equal(new Size(500, 300), own.Custom);
        Assert.Equal(new Size(500, 300), own.Resolve(FormKind.Window));
        Assert.Equal(new Size(500, 300), own.Resolve(FormKind.MessageBox));
        Assert.Equal(FormSize.Medium, own.Preset);
        Assert.Equal("Large", preset.ToString());
        Assert.Equal("500 x 300", own.ToString());
    }

    [Fact]
    public void TwoOptionsAreEqual_WhenTheyAreTheSameSizeOrTheSameOwnSize()
    {
        Assert.Equal((FormSizeOption)FormSize.Small, (FormSizeOption)FormSize.Small);
        Assert.NotEqual((FormSizeOption)FormSize.Small, (FormSizeOption)FormSize.Large);
        Assert.Equal((FormSizeOption)new Size(1, 2), (FormSizeOption)new Size(1, 2));
        Assert.NotEqual((FormSizeOption)new Size(1, 2), (FormSizeOption)new Size(2, 1));
        Assert.NotEqual((FormSizeOption)FormSize.Medium, (FormSizeOption)new Size(800, 560));
        Assert.Equal(((FormSizeOption)new Size(1, 2)).GetHashCode(), ((FormSizeOption)new Size(1, 2)).GetHashCode());
        Assert.False(((FormSizeOption)FormSize.Small).Equals("Small"));
    }

    // ---- the windows of UIStyles.Forms

    [Fact]
    public void TheWindowFactories_GiveTheSizeOfTheirKind_ScaledToTheScreen()
    {
        (Func<FormSizeOption, AppForm> Make, FormKind Kind)[] factories =
        {
            (size => UIStyles.Forms.CreateStandard("t", size: size), FormKind.Window),
            (size => UIStyles.Forms.CreatePrimary("t", size: size), FormKind.Window),
            (size => UIStyles.Forms.CreateDialogStandard("t", size: size), FormKind.Dialog),
            (size => UIStyles.Forms.CreateDialogPrimary("t", size: size), FormKind.Dialog),
            (size => UIStyles.Forms.CreateSettingsStandard("t", size: size), FormKind.Settings),
            (size => UIStyles.Forms.CreateSettingsPrimary("t", size: size), FormKind.Settings)
        };

        foreach ((Func<FormSizeOption, AppForm> make, FormKind kind) in factories)
        {
            foreach (FormSize size in new[] { FormSize.Small, FormSize.Medium, FormSize.Large })
            {
                AppForm form = _host.Add(make(size));

                Assert.Equal(Scaled(form, ((FormSizeOption)size).Resolve(kind)), form.Size);
            }
        }
    }

    [Fact]
    public void AnOwnSize_IsTheSizeOfTheWindow_AndWithoutASizeTheWindowKeepsItsOwn()
    {
        AppForm own = _host.Add(UIStyles.Forms.CreateStandard("t", size: new Size(640, 400)));
        AppForm plain = _host.Add(UIStyles.Forms.CreateStandard("t"));
        AppForm sized = _host.Add(UIStyles.Forms.CreateStandard("t", size: FormSize.Small));

        Assert.Equal(Scaled(own, new Size(640, 400)), own.Size);
        Assert.NotEqual(own.Size, plain.Size);
        Assert.NotEqual(sized.Size, plain.Size);
        Assert.Equal(new Form().Size, plain.Size);
    }

    [Fact]
    public void ASizeSmallerThanTheLeastSize_NeverMakesTheWindowSmallerThanThat()
    {
        AppForm form = _host.Show(UIStyles.Forms.CreateStandard("t", size: new Size(50, 50)));

        Assert.True(form.Width >= form.MinimumSize.Width);
        Assert.True(form.Height >= form.MinimumSize.Height);
    }

    // ---- the message box

    [Fact]
    public void TheMessageBox_HasTheSizesOfItsKind_OrAnOwnOne_InPixelsAsTheyAre()
    {
        foreach (FormSize size in new[] { FormSize.Small, FormSize.Medium, FormSize.Large })
        {
            MessageBoxForm form = _host.Add(new MessageBoxForm("Text", "Title", MessageBoxButtons.OK, MessageBoxIcon.Info, size));

            Assert.Equal(((FormSizeOption)size).Resolve(FormKind.MessageBox), form.Size);
        }

        MessageBoxForm own = _host.Add(new MessageBoxForm("Text", "Title", MessageBoxButtons.OK, MessageBoxIcon.Info, new Size(555, 222)));
        MessageBoxForm medium = _host.Add(new MessageBoxForm("Text", "Title", MessageBoxButtons.OK, MessageBoxIcon.Info));

        Assert.Equal(new Size(555, 222), own.Size);
        Assert.Equal(((FormSizeOption)FormSize.Medium).Resolve(FormKind.MessageBox), medium.Size);
        Assert.Equal(new Size(500, 220), medium.Size);
    }

    [Fact]
    public void TheOldMessageBoxSize_StillWorks_AsTheSameThreeSizes()
    {
#pragma warning disable CS0618
        MessageBoxForm small = _host.Add(new MessageBoxForm("Text", "Title", MessageBoxButtons.OK, MessageBoxIcon.Info, MessageBoxSize.Small));
        MessageBoxForm large = _host.Add(new MessageBoxForm("Text", "Title", MessageBoxButtons.OK, MessageBoxIcon.Info, MessageBoxSize.Large));
#pragma warning restore CS0618

        Assert.Equal(new Size(380, 160), small.Size);
        Assert.Equal(new Size(700, 320), large.Size);
    }

    // ---- the input box

    [Fact]
    public void TheInputBox_HasThreeWidths_AndTheHeightOfItsContent()
    {
        InputBoxForm small = _host.Show(UIStyles.Forms.CreateInputStandard("Name?", size: FormSize.Small));
        InputBoxForm medium = _host.Show(UIStyles.Forms.CreateInputStandard("Name?", size: FormSize.Medium));
        InputBoxForm large = _host.Show(UIStyles.Forms.CreateInputPrimary("Name?", size: FormSize.Large));
        InputBoxForm plain = _host.Show(UIStyles.Forms.CreateInputStandard("Name?"));

        Assert.True(small.Width < medium.Width && medium.Width < large.Width);
        Assert.Equal(medium.Size, plain.Size);
        Assert.Equal(Scaled(small, new Size(340, 0)).Width, small.ClientSize.Width);
        Assert.Equal(small.Height, medium.Height);
    }

    [Fact]
    public void AnOwnSizeOfTheInputBox_IsTheWidth_AndTheLeastHeight_WithTheButtonsAtTheBottom()
    {
        InputBoxForm content = _host.Show(UIStyles.Forms.CreateInputStandard("Name?"));
        InputBoxForm tall = _host.Show(UIStyles.Forms.CreateInputStandard("Name?", size: new Size(450, 400)));
        InputBoxForm low = _host.Show(UIStyles.Forms.CreateInputStandard("Name?", size: new Size(450, 20)));

        Assert.Equal(Scaled(tall, new Size(450, 0)).Width, tall.ClientSize.Width);
        Assert.Equal(Scaled(tall, new Size(450, 400)).Height, tall.ClientSize.Height);
        Assert.True(tall.ContentPanel.ClientRectangle.Contains(tall.GetPrivateField<Button>("_okButton")!.Bounds));
        Assert.True(tall.GetPrivateField<Button>("_okButton")!.Bottom > tall.ContentPanel.ClientSize.Height - 40);
        Assert.Equal(content.ClientSize.Height, low.ClientSize.Height);
    }

    [Fact]
    public void InputBoxShow_TakesTheSize()
    {
        Rectangle? seen = null;
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 30 };
        timer.Tick += (sender, e) =>
        {
            InputBoxForm? open = Application.OpenForms.OfType<InputBoxForm>().FirstOrDefault();

            if (open == null)
                return;

            timer.Stop();
            seen = open.Bounds;
            open.Close();
        };
        timer.Start();

        InputBox.Show("Name?", size: FormSize.Large);
        timer.Dispose();

        Assert.NotNull(seen);
        Assert.Equal(Scaled(_host.Add(new Form()), new Size(520, 0)).Width, seen!.Value.Width);
    }

    // ---- the options window

    [Fact]
    public void TheOptionsWindow_HasThreeWidthsOfItsCard_AndAnOwnSizeGivesTheLeastHeight()
    {
        OptionsForm small = _host.Show(UIStyles.Forms.CreateOptionsStandard(size: FormSize.Small));
        OptionsForm medium = _host.Show(UIStyles.Forms.CreateOptionsStandard(size: FormSize.Medium));
        OptionsForm large = _host.Show(UIStyles.Forms.CreateOptionsPrimary(size: FormSize.Large));
        OptionsForm tall = _host.Show(UIStyles.Forms.CreateOptionsStandard(size: new Size(520, 900)));

        foreach (OptionsForm form in new[] { small, medium, large, tall })
            form.FitToContent();

        Assert.True(small.Width < medium.Width && medium.Width < large.Width);
        Assert.Equal(medium.Width, tall.Width);
        Assert.True(tall.Height > medium.Height);
        Assert.Equal(small.Height, medium.Height);
    }

    [Fact]
    public void TheCardOfTheOptionsWindow_IsAsWideAsItsSize_InTheMiddle()
    {
        OptionsForm large = _host.Show(UIStyles.Forms.CreateOptionsStandard(size: FormSize.Large));
        large.FitToContent();
        Application.DoEvents();

        Panel holder = (Panel)large.PropertyTable.Parent!;
        int cardWidth = holder.ClientSize.Width - holder.Padding.Horizontal;

        Assert.True(Math.Abs(Scaled(large, new Size(680, 0)).Width - cardWidth) <= 2, "the card is " + cardWidth + " wide");
        Assert.Equal(holder.Padding.Left, holder.Padding.Right);
    }
}

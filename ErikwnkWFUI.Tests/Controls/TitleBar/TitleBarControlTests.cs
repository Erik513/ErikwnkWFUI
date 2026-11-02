using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.TitleBar;

/// <summary>The title bar of a window of the library: what its properties do, where the title stands, the three buttons, and a bar that is not in a window (yet).</summary>
[Collection(AccentColorTestCollection.Name)]
public class TitleBarControlTests
{
    private static Button Field(TitleBarControl bar, string name)
    {
        return (Button)typeof(TitleBarControl).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(bar)!;
    }

    private static Form HostWith(TitleBarControl bar, int width = 400)
    {
        Form form = OffscreenForm.Create(new Size(width, 120), Color.Black);
        form.Controls.Add(bar);
        form.Show();
        Application.DoEvents();

        return form;
    }

    [Fact]
    public void TheProperties_ShowAndHideWhatTheyAreFor_AndTheTitleIsNeverNull()
    {
        using TitleBarControl bar = new TitleBarControl(title: "Window");
        using Form host = HostWith(bar);
        using Bitmap logo = new Bitmap(16, 16);

        bar.Title = null!;
        Assert.Equal("", bar.Title);
        bar.Title = "Other";
        Assert.Equal("Other", bar.Title);

        bar.IconImage = logo;
        Assert.Same(logo, bar.IconImage);
        bar.IconImage = null;
        Assert.Null(bar.IconImage);

        bar.TitleForeColor = Color.Magenta;
        Assert.Equal(Color.Magenta, bar.TitleForeColor);

        bar.ShowMinimizeButton = false;
        bar.ShowMaximizeButton = false;
        bar.ShowCloseButton = false;
        Assert.False(Field(bar, "_minimizeButton").Visible);
        Assert.False(Field(bar, "_maximizeButton").Visible);
        Assert.False(Field(bar, "_closeButton").Visible);
        Assert.False(bar.ShowMinimizeButton || bar.ShowMaximizeButton || bar.ShowCloseButton);

        bar.ShowCloseButton = true;
        Assert.True(Field(bar, "_closeButton").Visible);

        bar.CloseButtonEnabled = false;
        Assert.False(Field(bar, "_closeButton").Enabled);
    }

    [Theory]
    [InlineData(ContentAlignment.MiddleLeft)]
    [InlineData(ContentAlignment.MiddleCenter)]
    [InlineData(ContentAlignment.MiddleRight)]
    public void TheTitle_StandsWhereItIsToldTo_AndStaysClearOfTheButtons(ContentAlignment alignment)
    {
        using TitleBarControl bar = new TitleBarControl(title: "Window", titleTextAlign: alignment);
        using Form host = HostWith(bar);
        Control label = bar.Controls.OfType<Label>().First();

        bar.TitleTextAlign = alignment;
        Assert.Equal(alignment, bar.TitleTextAlign);

        Assert.True(label.Right <= Field(bar, "_minimizeButton").Left || alignment == ContentAlignment.MiddleRight && label.Right <= bar.Width);
        using Bitmap picture = Draw(bar);
        Assert.Equal(bar.Width, picture.Width);
    }

    [Fact]
    public void TheButtons_MinimizeMaximizeRestoreAndCloseTheWindow()
    {
        using TitleBarControl bar = new TitleBarControl(title: "Window");
        using Form host = HostWith(bar);

        Field(bar, "_maximizeButton").InvokePrivate("OnClick", EventArgs.Empty);
        Assert.Equal(FormWindowState.Maximized, host.WindowState);

        Field(bar, "_maximizeButton").InvokePrivate("OnClick", EventArgs.Empty);
        Assert.Equal(FormWindowState.Normal, host.WindowState);

        Field(bar, "_minimizeButton").InvokePrivate("OnClick", EventArgs.Empty);
        Assert.Equal(FormWindowState.Minimized, host.WindowState);

        host.WindowState = FormWindowState.Normal;
        Field(bar, "_closeButton").InvokePrivate("OnClick", EventArgs.Empty);
        Assert.True(host.IsDisposed);
    }

    [Fact]
    public void ABarThatMayNotMaximize_LeavesTheWindowAsItIs()
    {
        using TitleBarControl bar = new TitleBarControl(title: "Window", allowWindowSnapAndMaximize: false);
        using Form host = HostWith(bar);

        Field(bar, "_maximizeButton").InvokePrivate("OnClick", EventArgs.Empty);

        Assert.Equal(FormWindowState.Normal, host.WindowState);
    }

    [Fact]
    public void ABarWithAWindowButNoForm_DoesNotKeepAskingForOne_AndFindsTheFormWhenThereIsOne()
    {
        using TitleBarControl bar = new TitleBarControl(title: "Window");
        using System.Windows.Forms.Panel holder = new System.Windows.Forms.Panel();
        holder.Controls.Add(bar);
        _ = bar.Handle;

        // (A bar that asked again for ever would never let the message queue run empty: DoEvents would not come back.)
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 5; i++)
            Application.DoEvents();
        Assert.True(watch.ElapsedMilliseconds < 3000);
        Assert.Null(typeof(TitleBarControl).GetField("_parentForm", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(bar));

        using Form host = OffscreenForm.Create(new Size(300, 100), Color.Black);
        host.Controls.Add(holder);
        host.Show();
        bar.Visible = false;
        bar.Visible = true;
        Application.DoEvents();

        Assert.Same(host, typeof(TitleBarControl).GetField("_parentForm", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(bar));
    }

    [Fact]
    public void TheSystemMenu_IsAskedForByARightClick_OnTheBarTheTitleAndTheLogo()
    {
        using TitleBarControl bar = new TitleBarControl(title: "Window");
        using Form host = HostWith(bar);
        int requests = 0;
        bar.SystemMenuRequested += (sender, position) => requests++;
        Control label = bar.Controls.OfType<Label>().First();

        foreach (Control control in new Control[] { bar, label })
        {
            control.InvokePrivate("OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
            control.InvokePrivate("OnMouseUp", new MouseEventArgs(MouseButtons.Right, 1, 5, 5, 0));
        }

        Assert.Equal(2, requests);
    }
}

using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// A change of the theme or of the accent while the lists are shown: they follow it by themselves (what was not set
/// by hand), look as a list that is made after the change looks, and stop listening when their window is gone.
/// [Collection]: the theme and the accent are switched.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ListBoxThemeSwitchTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();
    private readonly Color _accentAtStart = UIColors.Primary;

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(_accentAtStart);
    }

    private T Show<T>(T control) where T : Control
    {
        control.Bounds = new Rectangle(5, 5, 220, 150);
        Form form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-3000, 0), FormBorderStyle = FormBorderStyle.None, ClientSize = new Size(240, 170) };
        form.Controls.Add(control);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return control;
    }

    private static Color Row(WfuiListBox list, int index, bool selected = false, bool hovered = false, bool disabled = false) =>
        list.InvokePrivate<Color>("GetBackColor", index, selected, hovered, disabled);

    // Every color the list shows, in one line: what a list made now looks like.
    private static string Look(WfuiListBox list) => string.Join(
        " ",
        list.BackColor.ToArgb(), list.ItemBackColor.ToArgb(), list.ItemForeColor.ToArgb(), list.ForeColor.ToArgb(),
        list.DisabledBackColor.ToArgb(), list.DisabledForeColor.ToArgb(), list.HoverBackColor.ToArgb(),
        list.SelectedBackColor.ToArgb(), list.SelectedForeColor.ToArgb(), list.DragIndicatorColor.ToArgb(),
        Row(list, 0).ToArgb(), Row(list, 1).ToArgb(), Row(list, 0, selected: true).ToArgb(), Row(list, 1, hovered: true).ToArgb(),
        Row(list, 0, disabled: true).ToArgb());

    [Fact]
    public void AShownList_LooksAsAListMadeAfterTheChange_InBothDirections()
    {
        WfuiListBox shown = Show(UIStyles.ListBoxes.CreateStandard(allowReorder: true));
        shown.Items.Add("one");
        string dark = Look(shown);

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        using WfuiListBox madeInLight = UIStyles.ListBoxes.CreateStandard(allowReorder: true);
        Assert.NotEqual(dark, Look(shown));
        Assert.Equal(Look(madeInLight), Look(shown));

        UIColors.ApplyTheme(UIThemes.Dark);
        Application.DoEvents();

        using WfuiListBox madeInDark = UIStyles.ListBoxes.CreateStandard(allowReorder: true);
        Assert.Equal(dark, Look(shown));
        Assert.Equal(Look(madeInDark), Look(shown));
    }

    [Fact]
    public void TheTitleBar_FollowsToo()
    {
        TitledListBox shown = Show(UIStyles.TitledListBoxes.CreateStandard("Title"));
        Color darkBar = shown.HeaderBackColor;
        Color darkText = shown.HeaderForeColor;

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        using TitledListBox madeInLight = UIStyles.TitledListBoxes.CreateStandard("Title");
        Assert.NotEqual(darkBar, shown.HeaderBackColor);
        Assert.Equal(madeInLight.HeaderBackColor, shown.HeaderBackColor);
        Assert.Equal(madeInLight.HeaderForeColor, shown.HeaderForeColor);
        Assert.NotEqual(darkText, shown.HeaderForeColor);
        Assert.Equal(Look(madeInLight.InnerListBox), Look(shown.InnerListBox));
    }

    [Fact]
    public void WhatWasSetByHand_StaysAsItWasSet()
    {
        WfuiListBox shown = Show(new WfuiListBox());
        shown.BackColor = Color.Red;
        shown.ItemBackColor = Color.Green;
        shown.ItemForeColor = Color.Yellow;
        shown.DisabledBackColor = Color.Purple;
        shown.SelectedBackColor = Color.Orange;
        shown.HoverBackColor = Color.Pink;

        TitledListBox titled = Show(new TitledListBox(null, false, false, "Title"));
        titled.HeaderBackColor = Color.Navy;
        titled.HeaderForeColor = Color.Gold;

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(Color.Red, shown.BackColor);
        Assert.Equal(Color.Green, shown.ItemBackColor);
        Assert.Equal(Color.Yellow, shown.ItemForeColor);
        Assert.Equal(Color.Purple, shown.DisabledBackColor);
        Assert.Equal(Color.Orange, shown.SelectedBackColor);
        Assert.Equal(Color.Pink, shown.HoverBackColor);
        Assert.Equal(Color.Navy, titled.HeaderBackColor);
        Assert.Equal(Color.Gold, titled.HeaderForeColor);
    }

    [Fact]
    public void ThePrimaryList_FollowsAChangeOfTheAccent_TheGrayOneAndTheOneOfASetDoNot()
    {
        WfuiListBox primary = Show(UIStyles.ListBoxes.CreatePrimary());
        WfuiListBox gray = Show(UIStyles.ListBoxes.CreateStandard());
        WfuiListBox green = Show(UIStyles.ListBoxes.CreateStandard(colors: UIStyles.Colors.Sets.Green));
        TitledListBox titledPrimary = Show(UIStyles.TitledListBoxes.CreatePrimary("Title"));
        Color before = primary.SelectedBackColor;
        Color grayBefore = gray.SelectedBackColor;
        Color greenBefore = green.SelectedBackColor;

        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Assert.NotEqual(before, primary.SelectedBackColor);
        Assert.Equal(UIColors.PrimarySet.Hover, primary.SelectedBackColor);
        Assert.Equal(primary.SelectedBackColor, primary.DragIndicatorColor);
        Assert.Equal(UIColors.PrimarySet.Hover, titledPrimary.SelectedBackColor);
        Assert.Equal(grayBefore, gray.SelectedBackColor);
        Assert.Equal(greenBefore, green.SelectedBackColor);
    }

    private static int Listeners()
    {
        FieldInfo field = typeof(UIColors).GetField("ThemeChanged", BindingFlags.NonPublic | BindingFlags.Static)!;
        Delegate? handlers = (Delegate?)field.GetValue(null);

        return handlers == null ? 0 : handlers.GetInvocationList().Length;
    }

    [Fact]
    public void AListListensOnlyWhileItHasAWindow()
    {
        int before = Listeners();

        WfuiListBox shown = Show(UIStyles.ListBoxes.CreateStandard());
        TitledListBox titled = Show(UIStyles.TitledListBoxes.CreateStandard("Title"));
        Assert.Equal(before + 5, Listeners()); // each list: itself and its scroll bars; the titled one: itself

        foreach (Form form in _forms)
            form.Dispose();

        Assert.Equal(before, Listeners());

        // A list that never had a window listens to nothing.
        using WfuiListBox unshown = UIStyles.ListBoxes.CreateStandard();
        Assert.Equal(before, Listeners());
    }

    [Fact]
    public void AChangeFromAnotherThread_IsFollowedOnTheThreadOfTheList()
    {
        WfuiListBox shown = Show(UIStyles.ListBoxes.CreateStandard());
        int threadOfTheUpdate = -1;
        shown.BackColorChanged += (s, e) => threadOfTheUpdate = Environment.CurrentManagedThreadId;

        Thread other = new Thread(() => UIColors.ApplyTheme(UIThemes.Light));
        other.Start();
        other.Join();

        for (int i = 0; i < 50 && threadOfTheUpdate == -1; i++)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }

        Assert.Equal(Environment.CurrentManagedThreadId, threadOfTheUpdate);
    }
}

using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// What the standard list box does and the library's one keeps doing: the text an item shows (DisplayMember,
/// FormatString, ToString), ForeColor as the text color, the look of a disabled list, and a few representatives of the
/// members the native control brings along. [Collection]: the colors read the live theme.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ListBoxStandardBehaviorTests : IDisposable
{
    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private sealed class Person
    {
        public string Name { get; set; } = "";

        public int Age { get; set; }

        public override string ToString() => "Person-object";
    }

    // A list bound to a data source needs a window to bind in.
    private readonly List<Form> _forms = new List<Form>();

    private WfuiListBox Bound<T>(List<T> data, string displayMember, string? valueMember = null)
    {
        WfuiListBox listBox = new WfuiListBox();
        Form form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-3000, 0) };
        form.Controls.Add(listBox);
        form.Show();
        _forms.Add(form);

        listBox.DisplayMember = displayMember;

        if (valueMember != null)
            listBox.ValueMember = valueMember;

        listBox.DataSource = data;

        return listBox;
    }

    private static bool CanReorder(WfuiListBox listBox) =>
        (bool)typeof(WfuiListBox).GetProperty("CanReorder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(listBox)!;

    private static string TextOf(WfuiListBox listBox, object item) => listBox.InvokePrivate<string>("GetDisplayText", item)!;

    // ---- the text of an item ----

    [Fact]
    public void AnItem_ShowsItsToString_WithoutAnyMember()
    {
        using WfuiListBox listBox = new WfuiListBox();
        Person person = new Person { Name = "Anna" };

        Assert.Equal("Person-object", TextOf(listBox, person));
        Assert.Equal("plain", TextOf(listBox, "plain"));
        Assert.Equal("", TextOf(listBox, null!));
    }

    [Fact]
    public void DisplayMember_IsFollowed_WithItemsAndWithADataSource()
    {
        using WfuiListBox items = new WfuiListBox { DisplayMember = "Name" };
        Person anna = new Person { Name = "Anna" };
        items.Items.Add(anna);

        WfuiListBox bound = Bound(new List<Person> { new Person { Name = "Ben" } }, "Name");

        Assert.Equal("Anna", TextOf(items, anna));
        Assert.Equal("Ben", TextOf(bound, bound.Items[0]));
    }

    [Fact]
    public void FormatString_IsFollowed()
    {
        using WfuiListBox listBox = new WfuiListBox
        {
            FormattingEnabled = true,
            FormatString = "000"
        };
        listBox.Items.Add(7);

        Assert.Equal("007", TextOf(listBox, listBox.Items[0]));
    }

    [Fact]
    public void TheProvider_GoesBeforeTheStandardMembers()
    {
        using WfuiListBox listBox = new WfuiListBox { DisplayMember = "Name" };
        Person person = new Person { Name = "Anna", Age = 31 };
        listBox.Items.Add(person);

        Assert.Equal("Anna", TextOf(listBox, person));

        listBox.DisplayTextProvider = item => "from the provider";
        Assert.Equal("from the provider", TextOf(listBox, person));

        listBox.DisplayTextProvider = null;
        Assert.Equal("Anna", TextOf(listBox, person));
    }

    // ---- ForeColor ----

    [Fact]
    public void ForeColor_IsTheTextColorOfTheItems_AndTheOtherWayRound()
    {
        using WfuiListBox listBox = new WfuiListBox();

        listBox.ForeColor = Color.Red;
        Assert.Equal(Color.Red, listBox.ItemForeColor);
        Assert.Equal(Color.Red, listBox.InvokePrivate<Color>("GetTextColor", false, false));

        listBox.ItemForeColor = Color.Lime;
        Assert.Equal(Color.Lime, listBox.ForeColor);
    }

    // ---- a disabled list ----

    [Fact]
    public void ADisabledList_ShowsEveryItemDisabled_WhatEverTheSelectionAndTheMouse()
    {
        using WfuiListBox listBox = new WfuiListBox();
        listBox.Items.Add("one");
        listBox.Items.Add("two");
        listBox.Enabled = false;

        Color selectedBack = listBox.InvokePrivate<Color>("GetBackColor", 0, true, false, false);

        Assert.Equal(listBox.DisabledBackColor, selectedBack);
        Assert.NotEqual(listBox.SelectedBackColor, selectedBack);

        // The text of a disabled list is the one worked out for its background - or the one that was set.
        Assert.Equal(UIColors.GetDisabledText(listBox.ItemBackColor), listBox.InvokePrivate<Color>("GetTextColorOn", false, false, listBox.ItemBackColor));
        Assert.Equal(UIColors.GetDisabledText(selectedBack), listBox.InvokePrivate<Color>("GetTextColorOn", true, false, selectedBack));

        listBox.DisabledForeColor = Color.Gray;
        Assert.Equal(Color.Gray, listBox.InvokePrivate<Color>("GetTextColorOn", true, false, selectedBack));
        Assert.Equal(Color.Gray, listBox.InvokePrivate<Color>("GetTextColorOn", false, false, listBox.ItemBackColor));

        listBox.DisabledForeColor = Color.Empty;
        Assert.Equal(UIColors.TextDisabled, listBox.DisabledForeColor);
    }

    [Fact]
    public void AnEnabledList_AndADisabledItem_AreAsTheyWere()
    {
        using WfuiListBox listBox = new WfuiListBox();

        Assert.Equal(listBox.SelectedForeColor, listBox.InvokePrivate<Color>("GetTextColor", true, true));
        Assert.Equal(UIColors.GetDisabledText(listBox.ItemBackColor), listBox.InvokePrivate<Color>("GetTextColor", false, true));
        Assert.Equal(listBox.DisabledBackColor, listBox.InvokePrivate<Color>("GetBackColor", 0, false, false, true));
        Assert.Equal(listBox.SelectedBackColor, listBox.InvokePrivate<Color>("GetBackColor", 0, true, false, true));
    }

    [Fact]
    public void ADisabledList_ReadsInBothThemes()
    {
        try
        {
            foreach (bool light in new[] { false, true })
            {
                UIColors.ApplyTheme(light ? UIThemes.Light : UIThemes.Dark);

                using WfuiListBox listBox = new WfuiListBox { Enabled = false };
                Color back = listBox.InvokePrivate<Color>("GetBackColor", 0, false, false, false);
                Color text = listBox.InvokePrivate<Color>("GetTextColorOn", false, false, back);

                Assert.True(ColorContrastHelper.GetContrastRatio(text, back) >= 3.0, "the disabled text is not seen, light=" + light);
            }
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }

    // ---- what the native list box brings along (representatives) ----

    [Fact]
    public void TheNativeMembers_StillWork()
    {
        using WfuiListBox listBox = new WfuiListBox { Sorted = true };
        _ = listBox.Handle;
        listBox.Items.AddRange(new object[] { "pear", "apple", "fig" });

        Assert.Equal("apple", listBox.Items[0]);
        Assert.Equal(1, listBox.FindString("fi"));
        Assert.Equal(2, listBox.FindStringExact("pear"));

        int changes = 0;
        listBox.SelectedIndexChanged += (s, e) => changes++;
        listBox.SelectedIndex = 1;

        Assert.Equal("fig", listBox.SelectedItem);
        Assert.Equal(1, changes);

        WfuiListBox bound = Bound(new List<Person> { new Person { Name = "Anna", Age = 31 }, new Person { Name = "Ben", Age = 25 } }, "Name", "Age");
        bound.SelectedIndex = 1;

        Assert.Equal(25, bound.SelectedValue);
    }

    // ---- the selection modes and the columns ----

    [Fact]
    public void AMultipleSelection_IsKept_ClickingASelectedItemDoesNotClearIt()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(30, "A", "B", "C");
        listBox.SelectionMode = SelectionMode.MultiExtended;
        listBox.SetSelected(0, true);
        listBox.SetSelected(2, true);

        listBox.InvokePrivate("CapturePendingToggleDeselect", (IntPtr)(5 | (5 << 16)));

        Assert.Equal(2, listBox.SelectedIndices.Count);
        Assert.Equal(-1, listBox.GetPrivateField<int>("_pendingToggleDeselectIndex"));
    }

    [Fact]
    public void TheToggle_StaysForTheSingleSelection()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(30, "A", "B");
        listBox.SelectedIndex = 0;

        listBox.InvokePrivate("CapturePendingToggleDeselect", (IntPtr)(5 | (5 << 16)));

        Assert.Equal(0, listBox.GetPrivateField<int>("_pendingToggleDeselectIndex"));
    }

    [Fact]
    public void WithSeveralColumns_NoReorderingIsOffered()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(30, "A", "B", "C");
        listBox.AllowReorder = true;

        Assert.True(CanReorder(listBox));

        listBox.MultiColumn = true;

        Assert.False(CanReorder(listBox));
        Assert.True(listBox.AllowReorder, "the setting itself is kept for when the columns are gone");
    }

    [Fact]
    public void TheListBox_IsOwnerDrawnWithAFixedHeight_FromTheStart()
    {
        using WfuiListBox listBox = new WfuiListBox();

        Assert.Equal(DrawMode.OwnerDrawFixed, listBox.DrawMode);
        Assert.False(listBox.IntegralHeight);
        Assert.Equal(35, listBox.ItemHeight);
        Assert.Equal(SelectionMode.One, listBox.SelectionMode);
    }
}

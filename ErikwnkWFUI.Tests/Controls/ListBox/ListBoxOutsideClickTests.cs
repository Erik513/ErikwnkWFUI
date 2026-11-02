using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;
using Panel = System.Windows.Forms.Panel;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>A click somewhere else deselects - in the list box as in the list view and the grid - and the switch DeselectOnOutsideClick turns it off.</summary>
public class ListBoxOutsideClickTests : IDisposable
{
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;

    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private Form Host(params Control[] controls)
    {
        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            ClientSize = new Size(400, 400)
        };
        form.Controls.AddRange(controls);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return form;
    }

    private static WfuiListBox NewList()
    {
        WfuiListBox list = new WfuiListBox { Dock = DockStyle.Top, Height = 120 };
        list.Items.AddRange(new object[] { "one", "two", "three" });

        return list;
    }

    private static bool Watching(object owner)
    {
        object? filter = owner.GetPrivateField<object>("_outsideClickDeselectFilter");

        return filter != null && filter.GetPrivateField<bool>("_started");
    }

    private static void Press(object owner, int message, Control where)
    {
        OutsideClickFilter filter = owner.GetPrivateField<OutsideClickFilter>("_outsideClickDeselectFilter")!;
        Message m = Message.Create(where.Handle, message, IntPtr.Zero, IntPtr.Zero);
        filter.PreFilterMessage(ref m);
    }

    [Fact]
    public void TheWatch_RunsWhileTheListHasAWindow_AndEndsWithIt()
    {
        using WfuiListBox never = new WfuiListBox();
        Assert.False(Watching(never));

        WfuiListBox list = NewList();
        Form form = Host(list);
        Assert.True(Watching(list));

        list.InvokePrivate("DestroyHandle");
        Assert.False(Watching(list));

        _ = list.Handle;
        Assert.True(Watching(list));

        form.Controls.Remove(list);
        list.Dispose();
        Assert.False(Watching(list));
    }

    [Fact]
    public void AClickOnABoxOrALabel_ClearsTheSelection_AClickOnACommand_OrOnTheListItself_DoesNot()
    {
        WfuiListBox list = NewList();
        Button button = new Button { Dock = DockStyle.Bottom };
        Panel panel = new Panel { Dock = DockStyle.Bottom };
        ToolStrip toolBar = new ToolStrip { Dock = DockStyle.Bottom };
        Form form = Host(list, button, panel, toolBar);
        list.SelectedIndex = 1;

        bool Deselects(Control control) => list.InvokePrivate<bool>("IsOutsideClickThatDeselects", control.Handle);

        Assert.True(Deselects(panel));
        Assert.True(Deselects(form));
        Assert.False(Deselects(button));
        Assert.False(Deselects(toolBar));
        Assert.False(Deselects(list));
    }

    [Fact]
    public void ARealLeftPress_ClearsTheSelection_ARightPress_OrAPressOnTheList_DoesNot()
    {
        WfuiListBox list = NewList();
        Panel panel = new Panel { Dock = DockStyle.Bottom };
        Host(list, panel);
        list.SelectedIndex = 2;

        Press(list, WM_RBUTTONDOWN, panel);
        Press(list, WM_LBUTTONDOWN, list);
        Assert.Equal(2, list.SelectedIndex);

        Press(list, WM_LBUTTONDOWN, panel);
        Assert.Equal(-1, list.SelectedIndex);
    }

    [Fact]
    public void ThePartsOfATitledListBox_AreInsideTheList_ButAnythingElseIsNot()
    {
        TitledListBox titled = new TitledListBox { Dock = DockStyle.Top, Height = 160 };
        titled.Items.AddRange(new object[] { "one", "two" });
        Panel panel = new Panel { Dock = DockStyle.Bottom };
        Host(titled, panel);
        WfuiListBox inner = titled.GetPrivateField<WfuiListBox>("_listBox")!;
        Control title = titled.GetPrivateField<Control>("_headerPanel")!;
        inner.SelectedIndex = 0;

        bool Deselects(Control control) => inner.InvokePrivate<bool>("IsOutsideClickThatDeselects", control.Handle);

        Assert.False(Deselects(title));
        Assert.False(Deselects(titled));
        Assert.True(Deselects(panel));
    }

    [Fact]
    public void TheSwitch_IsOnByDefault_AndTurnsItOffInTheListBox_TheTitledListBox_TheListView_AndTheGrid()
    {
        WfuiListBox list = NewList();
        TitledListBox titled = new TitledListBox { Dock = DockStyle.Top, Height = 100 };
        titled.Items.AddRange(new object[] { "one" });
        WfuiListView listView = new WfuiListView { Dock = DockStyle.Top, Height = 100 };
        listView.Columns.Add("A", 100);
        listView.Items.Add(new ListViewItem("one"));
        ErikwnkWFUI.Controls.DataGridView grid = new ErikwnkWFUI.Controls.DataGridView { Dock = DockStyle.Top, Height = 100, DataSource = ErikwnkWFUI.Tests.Controls.DataGridView.GridTestHelpers.CreateItems(("Apple", 1)) };
        Panel panel = new Panel { Dock = DockStyle.Bottom };
        Host(list, titled, listView, grid, panel);

        Assert.True(list.DeselectOnOutsideClick);
        Assert.True(titled.DeselectOnOutsideClick);
        Assert.True(listView.DeselectOnOutsideClick);
        Assert.True(grid.DeselectOnOutsideClick);

        WfuiListBox inner = titled.GetPrivateField<WfuiListBox>("_listBox")!;
        list.SelectedIndex = 0;
        inner.SelectedIndex = 0;
        listView.Items[0].Selected = true;
        grid.CurrentCell = grid.Rows[0].Cells[0];
        grid.Rows[0].Cells[0].Selected = true;

        list.DeselectOnOutsideClick = false;
        titled.DeselectOnOutsideClick = false;
        listView.DeselectOnOutsideClick = false;
        grid.DeselectOnOutsideClick = false;

        Assert.False(list.InvokePrivate<bool>("IsOutsideClickThatDeselects", panel.Handle));
        Assert.False(inner.InvokePrivate<bool>("IsOutsideClickThatDeselects", panel.Handle));
        Assert.False(listView.InvokePrivate<bool>("IsOutsideClickThatDeselects", panel.Handle));
        Assert.False(grid.InvokePrivate<bool>("IsOutsideClickThatDeselects", panel.Handle));

        list.DeselectOnOutsideClick = true;
        Assert.True(list.InvokePrivate<bool>("IsOutsideClickThatDeselects", panel.Handle));
    }
}
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiTitledListBox = ErikwnkWFUI.Controls.TitledListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// TitledListBox is the ListBox with a title above it (an optional header bar
/// and every member of the ListBox passed on to the inner list box - see its
/// own class remarks) - these tests only cover that the passing on is
/// complete and correct, the title, and that CreatePrimary reaches the same
/// colors the factory tests lock in on the plain ListBox; the underlying
/// selection/reorder logic itself is ListBox's own concern, covered there.
/// [Collection] (see AccentColorTestCollection) - these read UIColors.Primary,
/// which UIColorsTests changes via SetAccent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class TitledListBoxTests : IDisposable
{
    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    // The one rule of this control: it is the ListBox with a title, so whatever the library's ListBox offers
    // (property, method, event) it offers too, of the same type.
    [Fact]
    public void EveryMemberOfTheLibrarysListBox_IsOnTheTitledListBoxToo()
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly;
        List<string> missing = new List<string>();

        foreach (System.Reflection.PropertyInfo property in typeof(ErikwnkWFUI.Controls.ListBox).GetProperties(flags))
        {
            System.Reflection.PropertyInfo mirrored = typeof(WfuiTitledListBox).GetProperty(property.Name);

            if (mirrored == null || mirrored.PropertyType != property.PropertyType)
                missing.Add("property " + property.Name);
        }

        foreach (System.Reflection.EventInfo item in typeof(ErikwnkWFUI.Controls.ListBox).GetEvents(flags))
        {
            if (typeof(WfuiTitledListBox).GetEvent(item.Name) == null)
                missing.Add("event " + item.Name);
        }

        foreach (System.Reflection.MethodInfo method in typeof(ErikwnkWFUI.Controls.ListBox).GetMethods(flags))
        {
            if (method.IsSpecialName)
                continue;

            Type[] parameters = method.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

            if (typeof(WfuiTitledListBox).GetMethod(method.Name, parameters) == null)
                missing.Add("method " + method.Name);
        }

        Assert.Empty(missing);
    }

    [Fact]
    public void TheMirroredMembers_ReachTheInnerListBox()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox();

        control.AllowReorder = true;
        control.ShowEnumeration = true;
        control.DisplayMember = "Name";
        control.SelectionMode = SelectionMode.MultiExtended;
        control.ItemBackColor = Color.Red;
        control.HoverBackColor = Color.Blue;
        control.Items.Add("a");
        control.Items.Add("b");
        control.SetSelected(1, true);

        Assert.True(control.InnerListBox.AllowReorder);
        Assert.True(control.InnerListBox.ShowEnumeration);
        Assert.Equal("Name", control.InnerListBox.DisplayMember);
        Assert.Equal(SelectionMode.MultiExtended, control.InnerListBox.SelectionMode);
        Assert.Equal(Color.Red, control.InnerListBox.ItemBackColor);
        Assert.Equal(Color.Blue, control.InnerListBox.HoverBackColor);
        Assert.True(control.GetSelected(1));
        Assert.Single(control.SelectedItems);
        Assert.Equal(1, control.FindString("b"));
    }

    [Fact]
    public void Items_ProxiesToInnerListBox()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox();

        control.Items.Add("A");
        control.Items.Add("B");

        Assert.Equal(2, control.InnerListBox.Items.Count);
        Assert.Equal("A", control.InnerListBox.Items[0]);
    }

    [Fact]
    public void SelectedIndex_ProxiesToInnerListBox()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox();
        control.Items.Add("A");
        control.Items.Add("B");

        control.SelectedIndex = 1;

        Assert.Equal(1, control.InnerListBox.SelectedIndex);
        Assert.Equal("B", control.SelectedItem);
    }

    [Fact]
    public void ClearSelected_ProxiesToInnerListBox()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox();
        control.Items.Add("A");
        control.SelectedIndex = 0;

        control.ClearSelected();

        Assert.Equal(-1, control.SelectedIndex);
    }

    [Fact]
    public void MoveItem_ProxiesToInnerListBox()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox();
        control.Items.Add("A");
        control.Items.Add("B");
        control.Items.Add("C");

        control.MoveItem(0, 2);

        Assert.Equal("B", control.Items[0]);
        Assert.Equal("C", control.Items[1]);
        Assert.Equal("A", control.Items[2]);
    }

    [Fact]
    public void SelectedIndexChanged_FiresWhenInnerListBoxSelectionChanges()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox();
        control.Items.Add("A");
        bool raised = false;
        control.SelectedIndexChanged += (sender, e) => raised = true;

        control.SelectedIndex = 0;

        Assert.True(raised);
    }

    [Fact]
    public void ItemsReordered_FiresWhenInnerListBoxReorders()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox();
        control.Items.Add("A");
        control.Items.Add("B");
        control.Items.Add("C");
        bool raised = false;
        control.ItemsReordered += (sender, e) => raised = true;

        control.MoveItem(0, 2);

        Assert.True(raised);
    }

    [Fact]
    public void SelectedBackColor_ProxiesToInnerListBox()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox
        {
            SelectedBackColor = UIColors.Primary
        };

        Assert.Equal(UIColors.Primary, control.InnerListBox.SelectedBackColor);
    }

    [Fact]
    public void DragIndicatorColor_ProxiesToInnerListBox()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox
        {
            DragIndicatorColor = UIColors.Primary
        };

        Assert.Equal(UIColors.Primary, control.InnerListBox.DragIndicatorColor);
    }

    [Fact]
    public void Title_RoundTrips()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox
        {
            Title = "Sample list"
        };

        Assert.Equal("Sample list", control.Title);
    }

    [Fact]
    public void CreatePrimary_SetsSelectedBackColorToAccent()
    {
        using WfuiTitledListBox control = UIStyles.TitledListBoxes.CreatePrimary();

        Assert.Equal(UIColors.Primary, control.SelectedBackColor);
    }

    [Fact]
    public void CreatePrimary_SetsDragIndicatorColorToAccent()
    {
        using WfuiTitledListBox control = UIStyles.TitledListBoxes.CreatePrimary();

        Assert.Equal(UIColors.Primary, control.DragIndicatorColor);
    }

    [Fact]
    public void CreateStandard_KeepsColorsGray()
    {
        using WfuiTitledListBox control = UIStyles.TitledListBoxes.CreateStandard();

        Assert.Equal(UIColors.BorderLight, control.SelectedBackColor);
        Assert.Equal(UIColors.BorderLight, control.DragIndicatorColor);
    }

    // ---- the events and the properties of the control that every list box has ----

    private readonly List<Form> _forms = new List<Form>();

    private WfuiTitledListBox ShownTitled()
    {
        WfuiTitledListBox control = new WfuiTitledListBox(null, false, false, "Title");
        control.Items.Add("A");
        control.Items.Add("B");
        control.Bounds = new System.Drawing.Rectangle(5, 5, 200, 200);

        Form form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-3000, 0), FormBorderStyle = FormBorderStyle.None, ClientSize = new System.Drawing.Size(220, 220) };
        form.Controls.Add(control);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return control;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "PostMessageW")]
    private static extern bool PostMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    [Fact]
    public void TheMouseAndKeyEventsOfTheList_ReachTheOnesOnTheControl_WithTheControlAsSender()
    {
        WfuiTitledListBox control = ShownTitled();
        List<string> seen = new List<string>();
        object? sender = null;
        System.Drawing.Point down = System.Drawing.Point.Empty;

        control.MouseDown += (s, e) => { seen.Add("MouseDown"); sender = s; down = e.Location; };
        control.MouseUp += (s, e) => seen.Add("MouseUp");
        control.Click += (s, e) => seen.Add("Click");
        control.MouseClick += (s, e) => seen.Add("MouseClick");
        control.DoubleClick += (s, e) => seen.Add("DoubleClick");
        control.KeyDown += (s, e) => seen.Add("KeyDown");
        control.KeyUp += (s, e) => seen.Add("KeyUp");

        IntPtr at = (IntPtr)(40 | (45 << 16));
        IntPtr list = control.InnerListBox.Handle;
        SendMessage(list, 0x0201, (IntPtr)1, at); // WM_LBUTTONDOWN
        SendMessage(list, 0x0202, IntPtr.Zero, at); // WM_LBUTTONUP
        SendMessage(list, 0x0203, (IntPtr)1, at); // WM_LBUTTONDBLCLK
        SendMessage(list, 0x0202, IntPtr.Zero, at); // the double click is raised when the button is up
        // (A key reaches the events through the message loop, not through a message sent to the window.)
        PostMessage(list, 0x0100, (IntPtr)0x28, IntPtr.Zero); // WM_KEYDOWN, Down
        PostMessage(list, 0x0101, (IntPtr)0x28, IntPtr.Zero); // WM_KEYUP
        Application.DoEvents();

        Assert.Contains("MouseDown", seen);
        Assert.Contains("MouseUp", seen);
        Assert.Contains("Click", seen);
        Assert.Contains("MouseClick", seen);
        Assert.Contains("DoubleClick", seen);
        Assert.Contains("KeyDown", seen);
        Assert.Contains("KeyUp", seen);
        Assert.Same(control, sender);

        // The place is in the coordinates of the control: the list is below the title bar.
        Assert.True(control.InnerListBox.Top > 0);
        Assert.Equal(45 + control.InnerListBox.Top, down.Y);
    }

    [Fact]
    public void TheFont_TheTextColor_AndTheContextMenu_AreThoseOfTheList()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox();
        using System.Drawing.Font font = new System.Drawing.Font("Consolas", 14f);
        using System.Windows.Forms.ContextMenuStrip menu = new System.Windows.Forms.ContextMenuStrip();

        control.Font = font;
        control.ForeColor = System.Drawing.Color.Red;
        control.ContextMenuStrip = menu;

        Assert.Equal("Consolas", control.InnerListBox.Font.Name);
        Assert.Equal(System.Drawing.Color.Red, control.InnerListBox.ForeColor);
        Assert.Equal(System.Drawing.Color.Red, control.InnerListBox.ItemForeColor);
        Assert.Same(menu, control.InnerListBox.ContextMenuStrip);
        Assert.Equal("Consolas", control.Font.Name);
    }

    [Fact]
    public void Focus_GoesToTheList()
    {
        WfuiTitledListBox control = ShownTitled();

        Assert.True(control.Focus());
        Assert.True(control.InnerListBox.Focused);
    }

    [Fact]
    public void TheEventsOfTheTitledList_SayTheTitledListIsTheSender()
    {
        WfuiTitledListBox control = ShownTitled();
        object? changed = null;
        object? reordered = null;
        control.SelectedIndexChanged += (s, e) => changed = s;
        control.ItemsReordered += (s, e) => reordered = s;

        control.SelectedIndex = 1;
        control.MoveItem(0, 1);

        Assert.Same(control, changed);
        Assert.Same(control, reordered);
    }
}

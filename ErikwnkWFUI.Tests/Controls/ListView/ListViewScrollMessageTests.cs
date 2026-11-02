using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>A scroll message is handled with the window locked and the list painted again, and a column can be made sortable again.</summary>
public class ListViewScrollMessageTests : IDisposable
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    private const int WM_VSCROLL = 0x0115;
    private const int SB_LINEDOWN = 1;

    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    [Fact]
    public void AScrollMessage_MovesTheRows_AndLeavesTheListPaintedAgain()
    {
        WfuiListView list = new WfuiListView { View = View.Details };
        list.Columns.Add("A", 100);

        for (int i = 0; i < 60; i++)
            list.Items.Add(new ListViewItem("row " + i));

        list.Dock = DockStyle.Fill;
        Form form = new Form { FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, Location = new Point(-3000, 0), ShowInTaskbar = false, ClientSize = new Size(200, 150) };
        form.Controls.Add(list);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);
        int top = list.TopItem!.Index;

        SendMessage(list.Handle, WM_VSCROLL, (IntPtr)SB_LINEDOWN, IntPtr.Zero);
        Application.DoEvents();

        Assert.True(list.TopItem!.Index > top);
    }

    [Fact]
    public void AColumnMadeNonSortableAndSortableAgain_IsSortable()
    {
        using WfuiListView list = new WfuiListView();
        list.Columns.Add("A", 100);

        list.SetColumnSortable(0, false);
        Assert.False(list.IsColumnSortable(0));

        list.SetColumnSortable(0, true);
        Assert.True(list.IsColumnSortable(0));
    }

    [Fact]
    public void TheDesigner_WritesTheColorsAndTheFontOnlyWhereTheyWereSet_AndKnowsTheDefaults()
    {
        using WfuiListView list = new WfuiListView();
        System.ComponentModel.PropertyDescriptorCollection properties = System.ComponentModel.TypeDescriptor.GetProperties(list);

        foreach (string name in new[]
        {
            "RowBackColor", "RowForeColor", "SelectionOverlayColor", "ColumnReorderIndicatorColor", "BorderColor", "ContextMenuSelectionColor", "HeaderBackColor",
            "HeaderForeColor", "HeaderFont", "BackColor", "ForeColor"
        })
        {
            System.ComponentModel.PropertyDescriptor property = properties[name]!;
            object other = name == "HeaderFont" ? new Font("Arial", 20) : Color.Magenta;

            Assert.False(property.ShouldSerializeValue(list), name + " writes the value of the theme");
            property.SetValue(list, other);
            Assert.True(property.ShouldSerializeValue(list), name);
            property.ResetValue(list);
            Assert.False(property.ShouldSerializeValue(list), name);
        }

        Assert.False(properties["ToastColors"]!.IsBrowsable);

        foreach (System.ComponentModel.PropertyDescriptor property in properties)
        {
            if (property.Attributes[typeof(System.ComponentModel.DefaultValueAttribute)] is System.ComponentModel.DefaultValueAttribute attribute
                && property.ComponentType == typeof(WfuiListView))
                Assert.True(Equals(attribute.Value, property.GetValue(list)), property.Name + " is not its default");
        }
    }
}

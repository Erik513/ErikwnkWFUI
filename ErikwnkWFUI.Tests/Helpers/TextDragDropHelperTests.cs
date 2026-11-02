using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;
using TextBox = System.Windows.Forms.TextBox;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>
/// Dropping text onto a control: the effect and the cursor while the drag is over it, what happens on the drop. The events are raised the way the
/// system raises them (the protected OnDrag... methods of the control). A drop target needs an STA thread, so the tests run on one.
/// </summary>
public class TextDragDropHelperTests
{
    private static void Raise(Control control, string method, EventArgs e)
    {
        typeof(Control).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(control, new object[] { e });
    }

    private static DragEventArgs Drag(IDataObject data)
    {
        return new DragEventArgs(data, 0, 0, 0, DragDropEffects.Copy, DragDropEffects.None);
    }

    private static IDataObject Text(string text) => new DataObject(DataFormats.Text, text);

    private static IDataObject Picture() => new DataObject(DataFormats.Bitmap, new Bitmap(2, 2));

    [Fact]
    public void WithoutAControl_NothingHappens()
    {
        StaThread.Run(() => Assert.Null(Record.Exception(() => TextDragDropHelper.EnableTextDragDrop(null!, text => { }))));
    }

    [Fact]
    public void TheControlAcceptsDrops_AfterwardsAndOverText_TheEffectIsCopy_AndTheCursorAHand()
    {
        StaThread.Run(() =>
        {
            using TextBox box = new TextBox();
            TextDragDropHelper.EnableTextDragDrop(box, text => { });
            DragEventArgs enter = Drag(Text("hello"));
            DragEventArgs over = Drag(Text("hello"));

            Raise(box, "OnDragEnter", enter);
            Raise(box, "OnDragOver", over);

            Assert.True(box.AllowDrop);
            Assert.Equal(DragDropEffects.Copy, enter.Effect);
            Assert.Equal(DragDropEffects.Copy, over.Effect);
            Assert.Equal(Cursors.Hand, box.Cursor);
        });
    }

    [Fact]
    public void OverSomethingThatIsNotText_TheEffectIsNone_AndTheCursorIsTheNoSign()
    {
        StaThread.Run(() =>
        {
            using TextBox box = new TextBox();
            TextDragDropHelper.EnableTextDragDrop(box, text => { });
            DragEventArgs enter = Drag(Picture());
            DragEventArgs over = Drag(Picture());

            Raise(box, "OnDragEnter", enter);
            Raise(box, "OnDragOver", over);

            Assert.Equal(DragDropEffects.None, enter.Effect);
            Assert.Equal(DragDropEffects.None, over.Effect);
            Assert.Equal(Cursors.No, box.Cursor);
        });
    }

    [Fact]
    public void LeavingTheControl_GivesTheDefaultCursorBack()
    {
        StaThread.Run(() =>
        {
            using TextBox box = new TextBox();
            TextDragDropHelper.EnableTextDragDrop(box, text => { });
            Raise(box, "OnDragEnter", Drag(Text("x")));

            Raise(box, "OnDragLeave", EventArgs.Empty);

            Assert.Equal(Cursors.Default, box.Cursor);
        });
    }

    [Fact]
    public void DroppingText_HandsItOver_AndGivesTheDefaultCursorBack()
    {
        StaThread.Run(() =>
        {
            using TextBox box = new TextBox();
            List<string> dropped = new List<string>();
            TextDragDropHelper.EnableTextDragDrop(box, dropped.Add);
            Raise(box, "OnDragEnter", Drag(Text("dropped text")));

            Raise(box, "OnDragDrop", Drag(Text("dropped text")));

            Assert.Equal(new[] { "dropped text" }, dropped);
            Assert.Equal(Cursors.Default, box.Cursor);
        });
    }

    [Fact]
    public void DroppingWhiteSpaceOrSomethingThatIsNotText_HandsNothingOver()
    {
        StaThread.Run(() =>
        {
            using TextBox box = new TextBox();
            List<string> dropped = new List<string>();
            TextDragDropHelper.EnableTextDragDrop(box, dropped.Add);

            Raise(box, "OnDragDrop", Drag(Text("   ")));
            Raise(box, "OnDragDrop", Drag(Picture()));

            Assert.Empty(dropped);
        });
    }

    [Fact]
    public void WithoutACallback_ADropDoesNotThrow()
    {
        StaThread.Run(() =>
        {
            using TextBox box = new TextBox();
            TextDragDropHelper.EnableTextDragDrop(box, null!);

            Assert.Null(Record.Exception(() => Raise(box, "OnDragDrop", Drag(Text("x")))));
        });
    }

    [Fact]
    public void ACallbackThatThrows_StillLeavesTheDefaultCursor_AndTheExceptionIsNotHidden()
    {
        StaThread.Run(() =>
        {
            using TextBox box = new TextBox();
            TextDragDropHelper.EnableTextDragDrop(box, text => throw new InvalidOperationException("mine"));
            Raise(box, "OnDragEnter", Drag(Text("x")));

            TargetInvocationException error = Assert.Throws<TargetInvocationException>(() => Raise(box, "OnDragDrop", Drag(Text("x"))));

            Assert.IsType<InvalidOperationException>(error.InnerException);
            Assert.Equal(Cursors.Default, box.Cursor);
        });
    }
}
